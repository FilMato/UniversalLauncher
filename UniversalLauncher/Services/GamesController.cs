using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;
using UniversalLauncher.Services.Scanners;

namespace UniversalLauncher.Services
{
    public class GamesController
    {
        // La lista dei giochi installati trovati dagli scanner
        public List<Game> InstalledGames { get; private set; } = new List<Game>();
        private List<IGameScanner> _scanners;
        public Dictionary<string, Game> InstalledGamesDict { get; private set; } = new Dictionary<string, Game>(StringComparer.OrdinalIgnoreCase);

        public GamesController()
        {
            _scanners = new List<IGameScanner>
            {
                // Basta aggiungere una riga qui per implementare nuove piattaforme.
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

        // Questa funzione esegue la scansione dal vivo e aggiorna la lista dei giochi installati
        public async Task ScanAndLoadGamesAsync()
        {
            // Creiamo una lista di "lavori" in background (Task.Run), uno per ogni scanner
            var tasks = _scanners.Select(scanner => Task.Run(() => scanner.GetInstalledGames())).ToList();
            //Aspettiamo che tutti gli scanner finiscano contemporaneamente
            var results = await Task.WhenAll(tasks);
            // Raccogliamo i risultati di tutti gli scanner in un'unica lista
            var allRawGames = new List<Game>();
            foreach (var resultList in results)
            {
                allRawGames.AddRange(resultList);
            }
            //Pulizia: rimuoviamo i nomi vuoti, uniamo i doppioni e le piattaforme, e ordiniamo (Codice identico al tuo!)[cite: 8]
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

        //Funzione per capire di che piattaforma va trattato un gioco che ne ha di più
        private int GetPlatformPriority(string platform)
        {
            if (string.IsNullOrEmpty(platform)) return 99; // Gli sconosciuti in fondo alla coda

            string p = platform.ToLower();
            if (p.Contains("steam")) return 1; // Steam vince su tutti (per i suoi link steam://)
            if (p.Contains("epic")) return 2;  // Epic ha i link com.epicgames.launcher://
            if (p.Contains("microsoft") || p.Contains("xbox")) return 3;
            if (p.Contains("gog")) return 4;
            return 10; // Tutti gli altri (es. EA, Ubisoft dal RegistryScanner) prendono priorità bassa
        }
    }
}