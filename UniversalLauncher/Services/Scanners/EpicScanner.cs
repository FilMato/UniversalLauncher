using System.IO;
using System.Text.Json;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services.Scanners
{
    public class EpicScanner : IGameScanner
    {
        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();
            //Standard path where Epic Games saves the list of installed games
            string manifestsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Epic\EpicGamesLauncher\Data\Manifests");
            //If the folder doesn't exist, Epic is not installed on the PC
            if (!Directory.Exists(manifestsPath)) 
            {
                return installedGames;
            }
            //Search for all .item files in that folder
            foreach (var file in Directory.GetFiles(manifestsPath, "*.item"))
            {
                try
                {
                    string jsonString = File.ReadAllText(file);
                    // reads the JSON file
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        //basic datas
                        string? title = root.TryGetProperty("DisplayName", out var nameProp) ? nameProp.GetString() : "";
                        string? appName = root.TryGetProperty("AppName", out var appNameProp) ? appNameProp.GetString() : "";
                        // If 'MainGameAppName' has a value and is different from its own 'AppName', it means it's a DLC or an Add-on
                        string? mainGameAppName = root.TryGetProperty("MainGameAppName", out var mainAppProp) ? mainAppProp.GetString() : "";
                        bool isDLC = !string.IsNullOrEmpty(mainGameAppName) && mainGameAppName != appName;
                        // We add the game to the list if it has a valid title and app name, and it's not a DLC or an Unreal Engine sample project
                        if (!string.IsNullOrEmpty(appName) && !string.IsNullOrEmpty(title) && !title.Contains("Unreal Engine") && !isDLC)
                        {
                            installedGames.Add(new EpicGame{ Title = title, AppName = appName });
                        }
                    }
                }
                catch{ }
            }
            return installedGames;
        }
    }
}