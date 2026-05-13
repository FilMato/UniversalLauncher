using Microsoft.Win32;
using System.IO;
using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services.Scanners
{
    public class UniversalRegistryScanner : IGameScanner
    {
        // Recive the entire list of platforms to scan, so we can apply our filters and optimizations in a single pass
        private readonly List<RegistryPlatformConfig> _configs;
        public UniversalRegistryScanner(List<RegistryPlatformConfig> configs)
        {
            _configs = configs;
        }
        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();
            // The two paths in the Windows Registry (64-bit and 32-bit)
            string[] registryKeys = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            // For each path, we look for all subfolders that represent installed programs
            foreach (var keyPath in registryKeys)
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) continue;
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                        {
                            if (subKey == null) continue;
                            // Reads basic information: publisher, program name and installation folder, or use empty strings to avoid errors
                            string publisher = subKey.GetValue("Publisher") as string ?? "";
                            string title = subKey.GetValue("DisplayName") as string ?? "";
                            string installLocation = subKey.GetValue("InstallLocation") as string ?? "";
                            // First filter: if any of these information is missing or the installation folder doesn't exist, it's not a valid game
                            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                                continue;
                            string titleLower = title.ToLower();
                            string publisherLower = publisher.ToLower();
                            // Controls if this program belongs to one of our platforms, applying all our filters in a single pass.
                            foreach (var config in _configs)
                            {
                                // Second filter: check if the publisher of the program contains one of our keywords
                                bool isPublisherMatch = config.PublisherKeywords.Any(keyword =>
                                    publisherLower.Contains(keyword.ToLower()));
                                if (isPublisherMatch)
                                {
                                    //Third filter: if the game title contains one of the blacklist keywords, we ignore it
                                    bool isBlacklisted = false;
                                    if (config.IgnoredTitles != null && config.IgnoredTitles.Count > 0)
                                    {
                                        isBlacklisted = config.IgnoredTitles.Any(badWord =>
                                            titleLower.Contains(badWord.ToLower()));
                                    }
                                    if (!isBlacklisted)
                                    {
                                        //Extracting the executable and saving the game
                                        string? executable = FindGameExecutable(installLocation);
                                        if (!string.IsNullOrEmpty(executable))
                                        {
                                            installedGames.Add(new RegistryGame
                                            {
                                                Title = title,
                                                Platform = config.PlatformName,
                                                ExecutablePath = executable
                                            });
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return installedGames;
        }

        private string? FindGameExecutable(string installDir)
        {
            try
            {
                // We set our threshold (10 MB for big games): if we find an executable larger than this, we can be pretty sure it's the main game executable and we can stop searching immediately.
                const long MASSIVE_FILE_THRESHOLD = 10 * 1024 * 1024;
                string? bestExe = null;
                long maxSize = 0;
                // We call our search method
                string? foundHeavyExe = SearchDirectoryIntelligently(installDir, ref bestExe, ref maxSize, MASSIVE_FILE_THRESHOLD);
                return foundHeavyExe ?? bestExe;
            }
            catch
            {
                return null;
            }
        }

        // Smart search engine: it searches recursively, but with optimizations to avoid wasting time in useless folders
        private string? SearchDirectoryIntelligently(string directoryPath, ref string? bestExe, ref long maxSize, long threshold, int currentDepth = 0)
        {
            // we set a maximum depth for the search to avoid getting lost in too deep folders, which are unlikely to contain the main executable and can significantly slow down the search.
            if (currentDepth > 4) return null;

            try
            {
                // Examine first the .exe files in the current folder,witch is more likely to contain the main executable.
                foreach (var exe in Directory.EnumerateFiles(directoryPath, "*.exe"))
                {
                    string fileName = Path.GetFileName(exe).ToLower();
                    // Blacklist of file name keywords: if the file name contains any of these keywords, we skip it directly
                    if (fileName.Contains("cleanup") || fileName.Contains("touchup") ||
                        fileName.Contains("uninstall") || fileName.Contains("unins") ||
                        fileName.Contains("activation") || fileName.Contains("crash") ||
                        fileName.Contains("redist") || fileName.Contains("dxsetup") ||
                        fileName.Contains("vcredist") || fileName.Contains("dotne"))
                    {
                        continue;
                    }
                    long size = new FileInfo(exe).Length;
                    // If we exceed the threshold we can be pretty sure we found the main executable, so we return it immediately without searching further
                    if (size > threshold){return exe;}
                    if (size > maxSize)
                    {
                        maxSize = size;
                        bestExe = exe;
                    }
                }
                // If we haven't found any executable that exceeds the threshold, we continue searching in the subfolders, applying the same optimizations to skip useless folders.
                foreach (var subDir in Directory.EnumerateDirectories(directoryPath))
                {
                    string dirName = new DirectoryInfo(subDir).Name.ToLower();
                    if (dirName == "redist" || dirName == "_redist" || dirName == "support" ||
                        dirName == "movies" || dirName == "video" || dirName == "sound" ||
                        dirName == "audio" || dirName == "music" || dirName == "logs" ||
                        dirName == "save" || dirName == "saves" || dirName == "locales" ||
                        dirName == "dlc" || dirName == "bonus")
                    {
                        continue;
                    }
                    // If the folder is valid, we enter and search recursively
                    string? foundInSub = SearchDirectoryIntelligently(subDir, ref bestExe, ref maxSize, threshold, currentDepth + 1);
                    // If the internal call found a file that exceeds the threshold, we pass it up and close everything
                    if (foundInSub != null) return foundInSub;
                }
                return null;
            }
            catch (Exception) 
            {
                return null;
            }
        }
    }
}