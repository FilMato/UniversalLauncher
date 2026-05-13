using System.IO;
using System.Xml.Linq;
using UniversalLauncher.Models.GamesModels;
using Windows.ApplicationModel; // Necessary for PackageSignatureKind
using Windows.Management.Deployment;

namespace UniversalLauncher.Services.Scanners
{
    public class MicrosoftStoreScanner : IGameScanner
    {
        // use the official namespaces of the manifest to avoid issues with different manifest versions and structures
        private static readonly XNamespace NsUap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
        private static readonly XNamespace NsFoundation = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";

        //---If there are eny more games that are not categorized as such in the manifest, we can add them to this list to make sure they are included in the library---
        private readonly string[] _ExeptionGames = { "minecraft", "roblox" };

        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();
            var packageManager = new PackageManager();
            var packages = packageManager.FindPackagesForUser("");// "" = current user
            foreach (var package in packages)
            {
                // Security check: skip framework, resource and development mode packages as they are not user-installed apps
                if (package.IsFramework || package.IsResourcePackage || package.IsDevelopmentMode)
                    continue;
                //Only take packages from the official Store or System
                if (package.SignatureKind != PackageSignatureKind.Store && package.SignatureKind != PackageSignatureKind.System)
                    continue;
                try
                {
                    // 1 Load the manifest to get the necessary info to launch the game and to verify if it's really a game
                    string manifestPath = Path.Combine(package.InstalledLocation.Path, "AppxManifest.xml");
                    if (!File.Exists(manifestPath)) continue;
                    XDocument manifest = XDocument.Load(manifestPath);
                    // 2 find aumid and appId (which are the key to launch the game)
                    var appElement = manifest.Descendants(NsFoundation + "Application").FirstOrDefault()
                                  ?? manifest.Descendants("Application").FirstOrDefault();
                    string? appId = appElement?.Attribute("Id")?.Value;
                    if (string.IsNullOrEmpty(appId)) continue;
                    string aumid = $"{package.Id.FamilyName}!{appId}";
                    //3 Extract the real name of the game
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
                    // In some cases, the title can be missing or be a resource reference, in that case we fallback to the package name which is better than nothing
                    if (string.IsNullOrWhiteSpace(realTitle) || realTitle.StartsWith("ms-resource"))
                    {
                        realTitle = package.Id.Name;
                    }
                    // 4 Verify if it's a game by checking if the manifest explicitly says it's a game (some games might not be categorized as such but it's better than nothing)
                    bool isGame = false;
                    var categoryElement = manifest.Descendants(NsUap + "Category").FirstOrDefault()
                                       ?? manifest.Descendants("Category").FirstOrDefault();
                    if (categoryElement != null && categoryElement.Value.ToLower().Contains("game"))
                    {
                        isGame = true;
                    }
                    // 5 Include also a list of titles that are not categorized as games but are (e.g. Minecraft, Roblox)
                    string titleLower = realTitle.ToLower();
                    if (_ExeptionGames.Any(exeption => titleLower.Contains(exeption)))
                    {
                        isGame = true;
                    }
                    // 6 If it's a game, add it to the list with all the necessary info to launch it and display it correctly in the UI
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