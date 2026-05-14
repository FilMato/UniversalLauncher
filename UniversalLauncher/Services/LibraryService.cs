using UniversalLauncher.Models;

namespace UniversalLauncher.Services
{
    // This class handles all persistence operations for the game library:saving/loading the JSON config file and reconciling scan results with the cached image data.
    public class LibraryService
    {
        private readonly FolderController _folderController;
        private readonly GamesController _gamesController;
        private readonly string _configPath;

        // In-memory image cache. MainWindow reads and writes it via this property.
        public Dictionary<string, ImageCache> ImageCache { get; private set; } = new();

        public LibraryService(
            FolderController folderController,
            GamesController gamesController,
            string configPath)
        {
            _folderController = folderController;
            _gamesController = gamesController;
            _configPath = configPath;
        }

        // Saving the library
        public void SaveAll()
        {
            try
            {
                var dataToSave = new LibrarySaveData
                {
                    Folders = _folderController.Folders.ToList(),
                    CachedImages = ImageCache
                };
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(dataToSave, options);
                System.IO.File.WriteAllText(_configPath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Error: {ex.Message}");
            }
        }

        // Loading the library
        public void LoadAll()
        {
            if (!System.IO.File.Exists(_configPath)) return;
            try
            {
                string json = System.IO.File.ReadAllText(_configPath);
                var loadedData = System.Text.Json.JsonSerializer.Deserialize<LibrarySaveData>(json);
                if (loadedData == null) return;

                if (loadedData.Folders != null)
                    MergeFolders(loadedData.Folders);

                // Replace the whole dictionary (LoadAll is the only place this happens)
                ImageCache = loadedData.CachedImages ?? new Dictionary<string, ImageCache>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LibraryService] Error: {ex.Message}");
            }
        }

        // Smart merging of folders
        private void MergeFolders(List<GameFolder> savedFolders)
        {
            // 1. Keep a snapshot of the current system folders so we don't lose their game lists and clear the list on screen to prepare for the merge
            var backupSystemFolders = _folderController.Folders
                .Where(f => f.IsSystemFolder)
                .ToList();
            _folderController.Folders.Clear();
            // 2.  We rebuild the list by reading the saved data line by line
            foreach (var savedFolder in savedFolders)
            {
                var systemFolder = backupSystemFolders
                    .FirstOrDefault(f => f.Name == savedFolder.Name);
                if (systemFolder != null)
                {
                    // Restore appearance settings on the live reference
                    systemFolder.BackgroundColor = savedFolder.BackgroundColor;
                    systemFolder.IconSymbolName = savedFolder.IconSymbolName;
                    _folderController.Folders.Add(systemFolder);
                }
                else
                {
                    _folderController.Folders.Add(savedFolder);
                }
            }
            // 3. Safety case: if in the future we add a new system folder to the program that wasn't present in the old save, let's make sure we don't lose it and add it at the end
            foreach (var sysFolder in backupSystemFolders)
            {
                if (!_folderController.Folders.Contains(sysFolder))
                    _folderController.Folders.Add(sysFolder);
            }
        }

        // This method is called after every scan and reconciles the scan results with the cached image paths, removing any entries for uninstalled games.
        public void ApplyScanResults()
        {
            foreach (var game in _gamesController.InstalledGames)
            {
                if (game.Title == null || !ImageCache.ContainsKey(game.Title))
                    continue;
                // Cover Check
                ReconcileImagePath(
                    game.Title,
                    ImageCache[game.Title].CoverUrl,
                    path => { game.CoverImageUrl = path; ImageCache[game.Title].CoverUrl = path; });
                // Icon Check
                ReconcileImagePath(
                    game.Title,
                    ImageCache[game.Title].IconUrl,
                    path => { game.IconImageUrl = path; ImageCache[game.Title].IconUrl = path; });
            }

            // We go through the folders and remove uninstalled games
            var installedTitles = _gamesController.InstalledGames
                .Select(g => g.Title)
                .ToList();
            foreach (var staleTitle in ImageCache.Keys
                .Where(t => !installedTitles.Contains(t))
                .ToList())
            {
                ImageCache.Remove(staleTitle);
            }
            foreach (var folder in _folderController.Folders)
                folder.Games.RemoveWhere(title => !installedTitles.Contains(title));

            _folderController.PopulateSystemFolders(_gamesController.InstalledGames);
        }

        // Helper method to check if the cached image file still exists and update the game and cache accordingly. If the file is missing, we clear the path to avoid broken images in the UI.
        private void ReconcileImagePath(string gameTitle, string? cachedUrl, Action<string> assign)
        {
            if (string.IsNullOrEmpty(cachedUrl)) return;

            string fileName = System.IO.Path.GetFileName(cachedUrl);
            string expectedPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache", fileName);

            assign(System.IO.File.Exists(expectedPath) ? expectedPath : "");
        }

        // This function deletes images that are no longer linked to any installed game, to prevent accumulating unnecessary files in the ImageCache folder.
        public void CleanUpImageCache()
        {
            try
            {
                // Find the ImageCache folder and if it doesn't exist, there's nothing to clean
                string cacheFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
                if (!System.IO.Directory.Exists(cacheFolder)) return;

                // 1. We collect in an "intelligent" list (HashSet) all the paths of the images IN USE
                var activeImagePaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var game in _gamesController.InstalledGames)
                {
                    if (!string.IsNullOrWhiteSpace(game.CoverImageUrl))
                        activeImagePaths.Add(System.IO.Path.GetFullPath(game.CoverImageUrl));
                    if (!string.IsNullOrWhiteSpace(game.IconImageUrl))
                        activeImagePaths.Add(System.IO.Path.GetFullPath(game.IconImageUrl));
                }
                // 2. We get all the files physically present in the ImageCache folder
                string[] filesInCache = System.IO.Directory.GetFiles(cacheFolder);
                // 3. For each physical file, if it's not in our list of active files, we delete it
                foreach (string file in filesInCache)
                {
                    string fullPath = System.IO.Path.GetFullPath(file);
                    if (!activeImagePaths.Contains(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore durante la pulizia della cache: {ex.Message}");
            }
        }

    }
}