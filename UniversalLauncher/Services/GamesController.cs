using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;
using UniversalLauncher.Services.Scanners;

namespace UniversalLauncher.Services
{
    public class GamesController
    {
        //List of installed games found by the scanners.
        public List<Game> InstalledGames { get; private set; } = new List<Game>();
        private List<IGameScanner> _scanners;
        public Dictionary<string, Game> InstalledGamesDict { get; private set; } = new Dictionary<string, Game>(StringComparer.OrdinalIgnoreCase);

        public GamesController()
        {
            _scanners = new List<IGameScanner>
            {
                // ---Add a new line here to implement new platforms---
                new UniversalRegistryScanner(new List<RegistryPlatformConfig>
                {
                    new RegistryPlatformConfig{ PlatformName = "EA App", PublisherKeywords = new List<string> { "Electronic Arts" }, IgnoredTitles = new List<string> { "ea app"}},
                    new RegistryPlatformConfig{ PlatformName = "GOG Galaxy", PublisherKeywords = new List<string> { "GOG.com", "GOG sp. z o.o." },  IgnoredTitles = new List<string> { "gog galaxy" }},
                    new RegistryPlatformConfig{ PlatformName = "Ubisoft", PublisherKeywords = new List<string> { "Ubisoft" }, IgnoredTitles = new List<string> { "ubisoft connect" }},
                    new RegistryPlatformConfig{ PlatformName = "Battle.net", PublisherKeywords = new List<string> { "Blizzard Entertainment" }, IgnoredTitles = new List<string> { "battle.net" }},
                    new RegistryPlatformConfig{ PlatformName = "Rockstar Games", PublisherKeywords = new List<string> { "Rockstar Games" }, IgnoredTitles = new List<string> { "rockstar games launcher", "social club" }},
                    new RegistryPlatformConfig { PlatformName = "Square Enix", PublisherKeywords = new List<string> { "SQUARE ENIX" }, IgnoredTitles = new List<string> {""} }
                }),
                new SteamScanner(),
                new EpicScanner(),
                new MicrosoftStoreScanner(),
                new RobloxScanner()
            };
        }

        // This function performs a live scan and updates the list of installed games
        public async Task ScanAndLoadGamesAsync()
        {
            // Creates a list of background "jobs" (Task.Run), one for each scanner
            var tasks = _scanners.Select(scanner => Task.Run(() => scanner.GetInstalledGames())).ToList();
            //Awaits the completion of all tasks simultaneously, without blocking the UI thread
            var results = await Task.WhenAll(tasks);
            // Regroups the results of all scanners into a single list of games, which may contain duplicates and empty titles at this stage
            var allRawGames = new List<Game>();
            foreach (var resultList in results)
            {
                allRawGames.AddRange(resultList);
            }
            // Cleanup: we remove empty titles, merge duplicates and platforms, and sort.
            InstalledGames = allRawGames
                .Where(g => !string.IsNullOrEmpty(g.Title))
                .GroupBy(g => g.Title)
                .Select(group =>
                {
                    var bestGame = group.OrderBy(g => GetPlatformPriority(g.Platform)).First();
                    var uniquePlatforms = group.Select(g => g.Platform).Where(p => !string.IsNullOrEmpty(p)).Distinct();
                    bestGame.Platform = string.Join(" • ", uniquePlatforms);

                    return bestGame;
                })
                .OrderBy(g => g.Title)
                .ToList();
            InstalledGamesDict = InstalledGames.ToDictionary(g => g.Title!, g => g, StringComparer.OrdinalIgnoreCase);
        }

        //Function to determine which platform to prioritize for a game that has multiple platforms
        private int GetPlatformPriority(string platform)
        {
            if (string.IsNullOrEmpty(platform)) return 99; // The unknown platform gets the lowest priority

            string p = platform.ToLower();
            if (p.Contains("steam")) return 1; // Steam alaws wins over all (because of its steam:// links).
            if (p.Contains("epic")) return 2;  // Then Epic wins over all the others (because of its com.epicgames.launcher:// links). And so on
            if (p.Contains("microsoft") || p.Contains("xbox")) return 3;
            if (p.Contains("gog")) return 4;
            return 10; //All of the others (like EA, Ubisoft from the RegistryScanner) get low priority.
        }
    }
}