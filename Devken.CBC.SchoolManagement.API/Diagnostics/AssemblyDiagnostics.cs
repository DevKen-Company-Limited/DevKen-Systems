using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Devken.CBC.SchoolManagement.API.Diagnostics
{
    /// <summary>
    /// Diagnostic service to identify assembly loading issues
    /// </summary>
    public static class AssemblyDiagnostics
    {
        /// <summary>
        /// Scans all loaded assemblies and reports any loading issues
        /// </summary>
        public static void DiagnoseAssemblyIssues(WebApplicationBuilder builder)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine(" Assembly Diagnostics - Scanning for Issues");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.ResetColor();

            try
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Total Loaded Assemblies: {loadedAssemblies.Length}");
                Console.ResetColor();

                // Check for problematic assemblies
                var problematicAssemblies = new List<string>();

                foreach (var assembly in loadedAssemblies)
                {
                    try
                    {
                        // Try to get types - this will trigger the exception if there's an issue
                        var types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        problematicAssemblies.Add(assembly.FullName ?? "Unknown");

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"\n❌ Problem Assembly: {assembly.FullName}");
                        Console.ResetColor();

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("   Loader Exceptions:");

                        foreach (var loaderException in ex.LoaderExceptions)
                        {
                            if (loaderException != null)
                            {
                                Console.WriteLine($"   - {loaderException.Message}");

                                if (loaderException.InnerException != null)
                                {
                                    Console.WriteLine($"     Inner: {loaderException.InnerException.Message}");
                                }
                            }
                        }
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"⚠️  Assembly {assembly.FullName}: {ex.Message}");
                        Console.ResetColor();
                    }
                }

                if (problematicAssemblies.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✅ No assembly loading issues detected!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n❌ Found {problematicAssemblies.Count} problematic assembly(ies)");
                    Console.ResetColor();
                }

                // Check for missing dependencies
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n📦 Checking Referenced Assemblies...");
                Console.ResetColor();

                var missingDependencies = new List<string>();

                foreach (var assembly in loadedAssemblies)
                {
                    try
                    {
                        var referencedAssemblies = assembly.GetReferencedAssemblies();

                        foreach (var refAssembly in referencedAssemblies)
                        {
                            try
                            {
                                Assembly.Load(refAssembly);
                            }
                            catch (FileNotFoundException)
                            {
                                var missing = $"{refAssembly.Name} (required by {assembly.GetName().Name})";
                                if (!missingDependencies.Contains(missing))
                                {
                                    missingDependencies.Add(missing);
                                }
                            }
                        }
                    }
                    catch { /* Ignore */ }
                }

                if (missingDependencies.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n⚠️  Missing Dependencies: {missingDependencies.Count}");
                    foreach (var dep in missingDependencies)
                    {
                        Console.WriteLine($"   - {dep}");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n✅ All dependencies loaded successfully!");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error during diagnostics: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n═══════════════════════════════════════════════════════════\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Validates controller discovery
        /// </summary>
        public static void ValidateControllers(WebApplication app)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.WriteLine(" Controller Discovery Validation");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.ResetColor();

            try
            {
                var applicationPartManager = app.Services.GetRequiredService<ApplicationPartManager>();
                var controllerFeature = new ControllerFeature();
                applicationPartManager.PopulateFeature(controllerFeature);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Controllers Found: {controllerFeature.Controllers.Count}");
                Console.ResetColor();

                if (controllerFeature.Controllers.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("\nDiscovered Controllers:");
                    foreach (var controller in controllerFeature.Controllers.OrderBy(c => c.Name))
                    {
                        Console.WriteLine($"   - {controller.Name}");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  No controllers discovered! This might indicate an assembly loading issue.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error validating controllers: {ex.Message}");
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n═══════════════════════════════════════════════════════════\n");
            Console.ResetColor();
        }
    }
}