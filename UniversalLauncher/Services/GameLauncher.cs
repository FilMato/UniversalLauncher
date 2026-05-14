using System.Diagnostics;
using System.Windows;

namespace UniversalLauncher.Services
{
    //Stateless utility that parses a launch command string and starts the process.
    public static class GameLauncher
    {
        public static void Launch(string launchCommand)
        {
            if (string.IsNullOrEmpty(launchCommand))
            {
                MessageBox.Show("Missing launch command for this game.", "Error");
                return;
            }
            try
            {
                ParseCommand(launchCommand, out string fileName, out string arguments);
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible to start the game.\nError: {ex.Message}", "Start Error");
            }
        }

        // Splits a raw launch command into executable path and arguments.
        private static void ParseCommand(string launchCommand, out string fileName, out string arguments)
        {
            fileName = launchCommand;
            arguments = "";

            // Case 1 — UWP/Store (e.g. "explorer.exe shell:AppsFolder\Minecraft...")
            if (launchCommand.StartsWith("explorer.exe ", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "explorer.exe";
                arguments = launchCommand.Substring("explorer.exe ".Length);
                return;
            }

            // Case 2 — Quoted path, optional trailing arguments (e.g. Roblox)
            if (launchCommand.StartsWith("\""))
            {
                int closingQuote = launchCommand.IndexOf('"', 1);
                if (closingQuote > 0)
                {
                    fileName = launchCommand.Substring(1, closingQuote - 1);
                    if (launchCommand.Length > closingQuote + 1)
                        arguments = launchCommand.Substring(closingQuote + 1).Trim();
                }
                return;
            }

            // Case 3 — Unquoted path with arguments (e.g. "C:\game.exe -run")
            int exeEnd = launchCommand.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase);
            if (exeEnd >= 0)
            {
                exeEnd += 4; // include ".exe"
                fileName = launchCommand.Substring(0, exeEnd);
                arguments = launchCommand.Substring(exeEnd).Trim();
            }
        }
    }
}
