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
            // Il percorso standard dove Epic Games salva la lista dei giochi installati
            string manifestsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Epic\EpicGamesLauncher\Data\Manifests");
            // Se la cartella non esiste, Epic non è installato sul PC
            if (!Directory.Exists(manifestsPath)) 
            {
                return installedGames;
            }
            // Cerca tutti i file .item in quella cartella
            foreach (var file in Directory.GetFiles(manifestsPath, "*.item"))
            {
                try
                {
                    string jsonString = File.ReadAllText(file);
                    // Leggiamo il file JSON
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        //Dati base
                        string? title = root.TryGetProperty("DisplayName", out var nameProp) ? nameProp.GetString() : "";
                        string? appName = root.TryGetProperty("AppName", out var appNameProp) ? appNameProp.GetString() : "";
                        // Se 'MainGameAppName' ha un valore ed è diverso dal proprio 'AppName', significa che è un DLC o un Add-on!
                        string? mainGameAppName = root.TryGetProperty("MainGameAppName", out var mainAppProp) ? mainAppProp.GetString() : "";
                        bool isDLC = !string.IsNullOrEmpty(mainGameAppName) && mainGameAppName != appName;

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