using System.IO;
using System.Xml.Linq;
using UniversalLauncher.Models.GamesModels;
using Windows.ApplicationModel; // Necessario per PackageSignatureKind
using Windows.Management.Deployment;

namespace UniversalLauncher.Services.Scanners
{
    public class MicrosoftStoreScanner : IGameScanner
    {
        // usiamo i namespace ufficiali del manifest
        private static readonly XNamespace NsUap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        private static readonly XNamespace NsFoundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser("");// "" = utente corrente
            foreach (var package in packages)
            {
                // Filtri di sicurezza
                if (package.IsFramework || package.IsResourcePackage || package.IsDevelopmentMode)
                    continue;
                // prende solo pacchetti provenienti dallo Store ufficiale o di Sistema
                if (package.SignatureKind != PackageSignatureKind.Store && package.SignatureKind != PackageSignatureKind.System)
                    continue;
                try
                {
                    string manifestPath = Path.Combine(package.InstalledLocation.Path, "AppxManifest.xml");
                    if (!File.Exists(manifestPath)) continue;
                    XDocument manifest = XDocument.Load(manifestPath);
                    // 1 Troviamo aumid e appId (che sono la chiave per lanciare il gioco)
                    var appElement = manifest.Descendants(NsFoundation + "Application").FirstOrDefault()
                                  ?? manifest.Descendants("Application").FirstOrDefault();
                    string? appId = appElement?.Attribute("Id")?.Value;
                    if (string.IsNullOrEmpty(appId)) continue;
                    string aumid = $"{package.Id.FamilyName}!{appId}";

                    // 2 Estraiamo il vero nome del gioco
                    string realTitle = package.DisplayName;
                    try
                    {
                        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041, 0))
                        {
                            var appListEntries = package.GetAppListEntries();
                            if (appListEntries != null && appListEntries.Any())
                            {
                                realTitle = appListEntries.First().DisplayInfo.DisplayName;
                            }
                        }
                    }
                    catch { }
                    if (string.IsNullOrWhiteSpace(realTitle) || realTitle.StartsWith("ms-resource"))
                    {
                        realTitle = package.Id.Name;
                    }
                    // 3 Verifichiamo se è un gioco vedendo se il manifesto dice chiaramente che è un gioco
                    bool isGame = false;
                    var categoryElement = manifest.Descendants(NsUap + "Category").FirstOrDefault()
                                       ?? manifest.Descendants("Category").FirstOrDefault();
                    if (categoryElement != null && categoryElement.Value.ToLower().Contains("game"))
                    {
                        isGame = true;
                    }
                    //Includiamo anche una lista di titoli che non sono catalogati come giochi ma lo sono (es. Minecraft, Roblox)
                    string titleLower = realTitle.ToLower();
                    if (titleLower.Contains("minecraft") || titleLower.Contains("roblox"))
                    {
                        isGame = true;
                    }
                    if (isGame)
                    {
                        installedGames.Add(new MicrosoftStoreGame{Title = realTitle, AUMID = aumid});
                    }
                }
                catch { }
            }
            return installedGames;
        }
    }
}