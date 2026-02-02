using System;
using System.Diagnostics;
using System.IO;

namespace Devken.CBC.SchoolManagement.API.Services
{
    public static class AngularLauncher
    {
        private static Process? _angularProcess;

        /// <summary>
        /// Launches the Angular frontend in a new terminal window.
        /// </summary>
        /// <param name="relativePath">Relative path from API project root to Angular project</param>
        public static void Launch(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️ Angular project path is empty. Skipping launch.");
                Console.ResetColor();
                return;
            }

            var absolutePath = Path.GetFullPath(relativePath);

            if (!Directory.Exists(absolutePath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Angular project folder does not exist: {absolutePath}");
                Console.ResetColor();
                return;
            }

            try
            {
                Console.WriteLine("🌐 Launching Angular frontend in a new window...");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k cd /d \"{absolutePath}\" && ng serve --open",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                _angularProcess = Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Failed to launch Angular frontend: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Terminates the Angular frontend process if it is running.
        /// </summary>
        public static void Close()
        {
            try
            {
                if (_angularProcess != null && !_angularProcess.HasExited)
                {
                    Console.WriteLine("🛑 Closing Angular frontend...");
                    _angularProcess.Kill(entireProcessTree: true);
                    _angularProcess.WaitForExit();
                    _angularProcess.Dispose();
                    _angularProcess = null;
                    Console.WriteLine("✅ Angular frontend closed.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Failed to close Angular frontend: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
