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
            // otteniamo il percorso di installazione di Steam
            string? mainSteamPath = GetSteamInstallationPath();
            if (string.IsNullOrEmpty(mainSteamPath))
            {
                return installedGames;
            }
            // otteniamo il percorsi delle librerie in cui steam installa i giochi, possono essere più di una
            List<string> libraryPaths = GetLibraryPaths(mainSteamPath);
            foreach (string libPath in libraryPaths)
            {
                string steamAppsPath = Path.Combine(libPath, "steamapps");
                if (Directory.Exists(steamAppsPath))
                {
                    // cerchiamo tutti i file manifest dei giochi installati, ogni gioco ha un suo .acf
                    string[] manifestFiles = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");
                    foreach (string file in manifestFiles)
                    {
                        // per ogni manifest, estraiamo le informazioni del gioco, se il titolo contiene "Steamworks" lo ignoriamo
                        Game? parsedGame = ParseManifest(file);
                        // We also check if Title is not null to keep the compiler happy
                        if (parsedGame != null && parsedGame.Title != null && !parsedGame.Title.Contains("Steamworks"))
                        {
                            installedGames.Add(parsedGame);
                        }
                    }
                }
            }
            return installedGames;
        }

        // Funzione per ottenere il percorso di installazione di Steam
        private string? GetSteamInstallationPath()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    if (key != null)
                    {
                        // Changed type to string?
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

        // Funzione per ottenere il percorso delle librerie di Steam
        private List<string> GetLibraryPaths(string mainSteamPath)
        {
            List<string> paths = new List<string> { mainSteamPath };
            string vdfPath = Path.Combine(mainSteamPath, "steamapps", "libraryfolders.vdf"); // Questo file contiene i percorsi delle librerie aggiuntive di Steam
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

        // Funzione che estrapola da un manifest le informazioni del gioco, se il titolo contiene "Steamworks" lo ignora
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