using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services.Scanners
{
    public class SteamScanner : IGameScanner
    {
        private static readonly Regex NameRegex = new Regex("\"name\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex AppIdRegex = new Regex("\"appid\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);
        private static readonly Regex LibraryPathRegex = new Regex("\"path\"\\s+\"([^\"]+)\"", RegexOptions.Compiled);
        public List<Game> GetInstalledGames()
        {
            List<Game> installedGames = new List<Game>();
            // Obtains the installation path of Steam, if it fails we return an empty list
            string? mainSteamPath = GetSteamInstallationPath();
            if (string.IsNullOrEmpty(mainSteamPath))
            {
                return installedGames;
            }
            // Selects all library paths where Steam games are installed, there can be more than one
            List<string> libraryPaths = GetLibraryPaths(mainSteamPath);
            foreach (string libPath in libraryPaths)
            {
                string steamAppsPath = Path.Combine(libPath, "steamapps");
                if (Directory.Exists(steamAppsPath))
                {
                    // Each manifest file contains the information of a single game, we parse it and if the title contains "Steamworks" we ignore it (we also check if Title is not null to keep the compiler happy).
                    string[] manifestFiles = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");
                    foreach (string file in manifestFiles)
                    {
                        Game? parsedGame = ParseManifest(file);
                        if (parsedGame != null && parsedGame.Title != null && !parsedGame.Title.Contains("Steamworks"))
                        {
                            installedGames.Add(parsedGame);
                        }
                    }
                }
            }
            return installedGames;
        }

        // Function to get the installation path of Steam
        private string? GetSteamInstallationPath()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        string? steamPath = key.GetValue("SteamPath")?.ToString();
                        if (!string.IsNullOrEmpty(steamPath))
                        {
                            return steamPath.Replace("/", "\\");
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        // Function to get the paths of Steam libraries
        private List<string> GetLibraryPaths(string mainSteamPath)
        {
            List<string> paths = new List<string> { mainSteamPath };
            string vdfPath = Path.Combine(mainSteamPath, "steamapps", "libraryfolders.vdf"); // This file contains the paths of all the libraries where Steam games are installed.
            if (File.Exists(vdfPath))
            {
                string[] lines = File.ReadAllLines(vdfPath);
                foreach (string line in lines)
                {
                    if (line.Contains("\"path\""))
                    {
                        Match match = LibraryPathRegex.Match(line);
                        if (match.Success)
                        {
                            string extractedPath = match.Groups[1].Value.Replace("\\\\", "\\");
                            if (!paths.Contains(extractedPath))
                            {
                                paths.Add(extractedPath);
                            }
                        }
                    }
                }
            }
            return paths;
        }

        //Function that extracts the game information from a manifest file, if the title contains "Steamworks" it ignores it
        private Game? ParseManifest(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                Match nameMatch = NameRegex.Match(content);
                Match idMatch = AppIdRegex.Match(content);
                if (nameMatch.Success && idMatch.Success)
                {
                    return  new SteamGame
                    {
                        Title = nameMatch.Groups[1].Value,
                        AppId = idMatch.Groups[1].Value,
                        Platform = "Steam"
                    };
                }
            }
            catch { }
            return null;
        }
    }
}