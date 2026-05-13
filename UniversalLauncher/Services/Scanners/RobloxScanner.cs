using System.IO;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services.Scanners
{
    /*  Roblox is a special case, because it doesn't have a standard installation path and it doesn't save the list of installed games in a file.
        However, we can find the executable of the game in the user's AppData folder, where Roblox saves its versions.
        The problem is that Roblox keeps all the old versions of the game, so we need to find the most recent one to be sure to launch the latest version of the game.
    */
    public class RobloxScanner : IGameScanner
    {
        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();
            // Point directly to the hidden AppData/Local folder of the user
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string robloxPath = Path.Combine(localAppData, "Roblox", "Versions");
            if (Directory.Exists(robloxPath))
            {
                try
                {
                    // Find all the RobloxPlayerBeta.exe files in the Versions folder and its subfolders
                    var exeFiles = Directory.GetFiles(robloxPath, "RobloxPlayerBeta.exe", SearchOption.AllDirectories);
                    if (exeFiles.Length > 0)
                    {
                        // Find the most recent executable by comparing the last write time of each file
                        string latestExe = exeFiles[0];
                        DateTime newestDate = File.GetLastWriteTime(latestExe);
                        foreach (var exe in exeFiles)
                        {
                            DateTime currentDate = File.GetLastWriteTime(exe);
                            if (currentDate > newestDate)
                            {
                                latestExe = exe;
                                newestDate = currentDate;
                            }
                        }
                        installedGames.Add(new StandaloneGame{ Title = "Roblox", ExecutablePath = latestExe, LaunchArguments = "--app" }); // LaunchArguments== "--app":`This argument omens the Roblox app directly.
                    }
                }
                catch{}
            }
            return installedGames;
        }
    }
}