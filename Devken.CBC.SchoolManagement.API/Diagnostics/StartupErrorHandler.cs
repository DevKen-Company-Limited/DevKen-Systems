using System.Reflection;

namespace Devken.CBC.SchoolManagement.API.Diagnostics
{
    /// <summary>
    /// Early exception handler to catch ReflectionTypeLoadException before debugger attachment
    /// </summary>
    public static class StartupErrorHandler
    {
        /// <summary>
        /// Must be called FIRST in Program.cs Main method
        /// </summary>
        public static void Initialize()
        {
            // Catch first-chance exceptions
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (e.Exception is ReflectionTypeLoadException rtle)
                {
                    LogReflectionError(rtle);
                }
            };

            // Catch unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is ReflectionTypeLoadException rtle)
                {
                    LogReflectionError(rtle);
                }
            };

            // Catch assembly resolve failures
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static void LogReflectionError(ReflectionTypeLoadException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n" + new string('═', 70));
            Console.WriteLine("🔥 REFLECTION TYPE LOAD EXCEPTION DETECTED");
            Console.WriteLine(new string('═', 70) + "\n");
            Console.ResetColor();

            // Show successfully loaded types
            var successfulTypes = ex.Types.Where(t => t != null).ToList();
            if (successfulTypes.Any())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Successfully loaded {successfulTypes.Count} types:");
                foreach (var type in successfulTypes.Take(10))
                {
                    Console.WriteLine($"   • {type.FullName}");
                }
                if (successfulTypes.Count > 10)
                {
                    Console.WriteLine($"   ... and {successfulTypes.Count - 10} more");
                }
                Console.ResetColor();
            }

            // Show loader exceptions (THE IMPORTANT PART)
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n❌ Loader Exceptions ({ex.LoaderExceptions.Length}):\n");

            var uniqueExceptions = ex.LoaderExceptions
                .Where(e => e != null)
                .GroupBy(e => e.Message)
                .Select(g => g.First())
                .ToList();

            foreach (var loaderEx in uniqueExceptions)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"├─ {loaderEx.GetType().Name}");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"│  Message: {loaderEx.Message}");

                if (loaderEx is FileNotFoundException fnf)
                {
                    Console.WriteLine($"│  Missing File: {fnf.FileName}");
                }

                if (loaderEx is FileLoadException fle)
                {
                    Console.WriteLine($"│  File: {fle.FileName}");
                }

                if (loaderEx.InnerException != null)
                {
                    Console.WriteLine($"│  Inner: {loaderEx.InnerException.Message}");
                }
                Console.WriteLine("│");
            }
            Console.ResetColor();

            // Provide solutions
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', 70));
            Console.WriteLine("💡 POSSIBLE SOLUTIONS:");
            Console.WriteLine(new string('═', 70));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("1. Missing NuGet Package:");
            Console.WriteLine("   dotnet add package [PackageName]");
            Console.WriteLine("\n2. Clean and Rebuild:");
            Console.WriteLine("   dotnet clean && dotnet restore && dotnet build");
            Console.WriteLine("\n3. Delete bin/obj folders:");
            Console.WriteLine("   Get-ChildItem -Recurse -Include bin,obj | Remove-Item -Recurse -Force");
            Console.WriteLine("\n4. Check project references and versions");
            Console.WriteLine("\n5. Ensure all projects target same framework version");
            Console.ResetColor();

            Console.WriteLine("\n" + new string('═', 70) + "\n");
        }

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  Assembly Resolve Request: {args.Name}");
            Console.ResetColor();

            try
            {
                // Try to find the assembly in loaded assemblies
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName == args.Name);

                if (loadedAssembly != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"   ✅ Found in loaded assemblies");
                    Console.ResetColor();
                    return loadedAssembly;
                }

                // Try to load by name
                var assemblyName = new AssemblyName(args.Name);
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);

                if (assembly != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"   ✅ Found by name: {assembly.GetName().Name}");
                    Console.ResetColor();
                    return assembly;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   ❌ Could not resolve assembly");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   ❌ Error resolving: {ex.Message}");
                Console.ResetColor();
            }

            return null;
        }
    }
}