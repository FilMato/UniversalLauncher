using System.Windows;
using System.Windows.Controls;
using UniversalLauncher.Models;
using UniversalLauncher.Services;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace UniversalLauncher
{
    public partial class MainWindow : FluentWindow
    {
        private FolderController _folderController = new FolderController();
        private GamesController _gamesController = new GamesController();
        private string _currentOpenFolderName = "";
        private bool _isListView = false;
        private string _searchQuery = "";
        private Dictionary<string, ImageCache> _imageCache = new Dictionary<string, ImageCache>();
        private readonly string _configPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "library_v1.json");
        private System.Windows.Threading.DispatcherTimer? _saveTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Inizializzazione Timer Debounce
            // Inizialization of the Debounce Timer for saving the library
            _saveTimer = new System.Windows.Threading.DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(2);
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop(); // stops the timer until the next request
                SaveAll();      
            };

            this.Loaded += MainWindow_Loaded;
            this.Closing += (s, e) => SaveAll();
        }
        private void RequestDeferredSave()
        {
            // This way, if the user makes multiple changes in a short time, we won't save the library multiple times unnecessarily, but only once after they've stopped making changes for 2 seconds.
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApiKeyControl();
            await LoadGamesAndFoldersAsync();
            CleanUpImageCache();
        }
        private void ApiKeyControl()
        {
            string secretPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "secrets.json");
            bool keyMancante = false;

            // If the file doesn't exist, we create it as a "template"
            if (!System.IO.File.Exists(secretPath))
            {
                string template = "{\n  \"SteamGridApiKey\": \"YOUR_API_KEY_HERE\"\n}";
                System.IO.File.WriteAllText(secretPath, template, System.Text.Encoding.UTF8);
                keyMancante = true;
            }
            else
            {
                // If it exists, we check its content
                try
                {
                    string json = System.IO.File.ReadAllText(secretPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    string apiKey = doc.RootElement.GetProperty("SteamGridApiKey").GetString() ?? "";
                    // If it's empty or still has the default text, we show the warning
                    if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY_HERE")
                    {
                        keyMancante = true;
                    }
                }
                catch
                {
                    // If the file is corrupted or badly formatted
                    keyMancante = true;
                }
            }

            if (keyMancante)
            {
                MessageBox.Show("Welcome to UniversalLauncher! 🚀\n\n" +
                                "If you want to visualize game covers and icons, you need to enter a free API Key.\n\n" +
                                "1. Go to: steamgriddb.com and create an account for free\n" +
                                "2. Go to: steamgriddb.com/profile/api\n" +
                                "3. Generate a key and copy it.\n" +
                                "4. Open the 'secrets.json' file (next to the executable) and paste the key in place of 'YOUR_API_KEY_HERE'.\n\n" +
                                "The program will still work, but you will only see the default gray icons until you enter the key.");
            }
        }

        // This function deletes images that are no longer linked to any installed game, to prevent accumulating unnecessary files in the ImageCache folder.
        private void CleanUpImageCache()
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

        // Loads the installed games and populates the system folders (All games and Uncategorized) in one go, then draws the interface
        private async Task LoadGamesAndFoldersAsync()
        {
            _folderController.InitializeSystemFolders();
            LoadAll(); 
            RefreshFoldersUI();
            // Start the parallel scan in the background without blocking the UI
            await _gamesController.ScanAndLoadGamesAsync();
            ApplyScanResults();
            RefreshFoldersUI();
        }

        // Search bar management
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the search query every time the user types something, trimming it and converting to lowercase to make the search more flexible
            var searchBox = sender as TextBox ?? sender as Wpf.Ui.Controls.TextBox;
            _searchQuery = searchBox?.Text?.Trim().ToLower() ?? "";
            // While typing, instantly reload the interface to show only the games that match the search query
            if (!string.IsNullOrEmpty(_currentOpenFolderName)) OpenFolder(_currentOpenFolderName);
            else RefreshFoldersUI();
        }

        private void OpenFolder(string name)
        {
            _currentOpenFolderName = name;
            TxtPageTitle.Text = name.ToUpper();
            BtnBack.Visibility = Visibility.Visible;
            if (BtnCreateFolder != null) BtnCreateFolder.Visibility = Visibility.Collapsed;
            // Clears the interface and draws the games of the selected folder, resetting the dimensions so that the new elements adapt dynamically to the space
            MainContainer.Children.Clear();
            if (MainContainer is WrapPanel wp)
            {
                wp.ItemWidth = double.NaN;
                wp.ItemHeight = double.NaN;
            }
            // Adds the games in the folder and filters them based on the search query, if present
            var currentFolder = _folderController.Folders.FirstOrDefault(f => f.Name == name);
            if (currentFolder != null && currentFolder.Games.Count > 0)
            {
                var filteredGames = currentFolder.Games
                    .Where(g => string.IsNullOrEmpty(_searchQuery) || g.ToLower().Contains(_searchQuery))
                    .ToList();

                // If there are games that match the search, it draws them, otherwise it shows a message
                if (filteredGames.Count > 0)
                {
                    foreach (string gameTitle in filteredGames)
                    {
                        if (_isListView) MainContainer.Children.Add(CreateCompactGameRow(gameTitle));
                        else DrawGameCard(gameTitle);
                    }
                }
                else
                {
                    MainContainer.Children.Add(new TextBlock { Text = "No games match your search.", Margin = new Thickness(20) });
                }
            }
            else
            {
                MainContainer.Children.Add(new TextBlock { Text = "This folder is empty.", Margin = new Thickness(20) });
            }
        }

        // Function to return to the main view of the folders
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            TxtPageTitle.Text = "Installed Games";
            BtnBack.Visibility = Visibility.Collapsed;
            if (BtnCreateFolder != null) BtnCreateFolder.Visibility = Visibility.Visible;
            RefreshFoldersUI();
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            // Visual feedback
            var originalContent = BtnSync.Content;
            BtnSync.Content = "Syncing...";
            BtnSync.IsEnabled = false;
            // Asynchronous parallel scanning of installed games, which can take a few seconds, especially if the user has a large library.
            await _gamesController.ScanAndLoadGamesAsync();
            // Update the image URLs of the newly scanned games with those already present in the cache, if available
            ApplyScanResults();
            SaveAll();
            RefreshFoldersUI();
            CleanUpImageCache();
            BtnSync.Content = originalContent;
            BtnSync.IsEnabled = true;
        }

        private void RefreshFoldersUI()
        {
            if (_isListView) DrawFullListView();
            else
            {
                MainContainer.Children.Clear();
                foreach (var folder in _folderController.Folders) DrawFolder(folder);
            }
        }

        // Function to switch between grid view and list view, and vice versa
        private void SwitchView_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Wpf.Ui.Controls.Button;
            string mode = btn?.Tag?.ToString() ?? "";
            if (mode == "list" && !_isListView)
            {
                _isListView = true;
                RefreshFoldersUI();
            }
            else if (mode == "grid" && _isListView)
            {
                _isListView = false;
                _currentOpenFolderName = ""; // In this way we ensure that when switching back to grid view, we return to the main view with all the folders
                RefreshFoldersUI();
            }
        }

        // Function to launch games
        private void LaunchUniversalGame(string launchCommand)
        {
            if (string.IsNullOrEmpty(launchCommand))
            {
                System.Windows.MessageBox.Show("Missing launch command for this game.", "Error");
                return;
            }
            try
            {
                string fileName = launchCommand;
                string arguments = "";

                // 1 Case: Microsoft Store / UWP games (e.g. Minecraft Launcher)
                if (launchCommand.StartsWith("explorer.exe ", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = "explorer.exe"; // the program to launch is always explorer.exe
                    arguments = launchCommand.Substring("explorer.exe ".Length); // the argument is everything that comes after "explorer.exe "
                }
                // 2 Case: Paths enclosed in quotes with final arguments (e.g. Roblox)
                else if (launchCommand.StartsWith("\""))
                {
                    int secondQuoteIndex = launchCommand.IndexOf("\"", 1);
                    if (secondQuoteIndex > 0)
                    {
                        // Extract only what is inside the quotes, which is the actual path of the executable to launch
                        fileName = launchCommand.Substring(1, secondQuoteIndex - 1);
                        // Extract what is after the quotes (the arguments)
                        if (launchCommand.Length > secondQuoteIndex + 1)
                        {
                            arguments = launchCommand.Substring(secondQuoteIndex + 1).Trim();
                        }
                    }
                }
                // 3 Case: Paths without quotes but with arguments (e.g. C:\game.exe -run)
                else if (launchCommand.Contains(".exe ", StringComparison.OrdinalIgnoreCase))
                {
                    int exeIndex = launchCommand.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase) + 4;
                    fileName = launchCommand.Substring(0, exeIndex);
                    arguments = launchCommand.Substring(exeIndex).Trim();
                }

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Impossible to start the game.\nError: {ex.Message}", "Start Error");
            }
        }

        // Saving the library
        public void SaveAll()
        {
            try
            {
                var dataToSave = new LibrarySaveData();
                dataToSave.Folders = _folderController.Folders.ToList();
                dataToSave.CachedImages = _imageCache;

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(dataToSave, options);
                System.IO.File.WriteAllText(_configPath, json, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        // Loading the library 
        private void LoadAll()
        {
            if (!System.IO.File.Exists(_configPath)) return;
            try
            {
                string json = System.IO.File.ReadAllText(_configPath);
                var loadedData = System.Text.Json.JsonSerializer.Deserialize<LibrarySaveData>(json);
                if (loadedData != null)
                {
                    // Smart merging of folders
                    if (loadedData.Folders != null)
                    {
                        // 1. We set aside the system folders created by default by the program (to avoid losing the games inside them)
                        var backupSystemFolders = _folderController.Folders.Where(f => f.IsSystemFolder).ToList();
                        // 2. We completely clear the list on screen
                        _folderController.Folders.Clear(); 
                        // 3. We rebuild the list by reading the saved data line by line
                        foreach (var savedFolder in loadedData.Folders)
                        {
                            // We look for a match between the saved folder and one of our system folders that we set aside
                            var systemFolder = backupSystemFolders.FirstOrDefault(f => f.Name == savedFolder.Name);

                            if (systemFolder != null)
                            {
                                systemFolder.BackgroundColor = savedFolder.BackgroundColor;
                                systemFolder.IconSymbolName = savedFolder.IconSymbolName;
                                _folderController.Folders.Add(systemFolder);
                            }
                            else
                            {
                                _folderController.Folders.Add(savedFolder);
                            }
                        }
                        // 4. Safety case: if in the future we add a new system folder to the program that wasn't present in the old save, let's make sure we don't lose it and add it at the end
                        foreach (var sysFolder in backupSystemFolders)
                        {
                            if (!_folderController.Folders.Contains(sysFolder))
                            {
                                _folderController.Folders.Add(sysFolder);
                            }
                        }
                    }
                    _imageCache = loadedData.CachedImages ?? new Dictionary<string, ImageCache>();
                }
            }
            catch { }
        }

        private void ApplyScanResults()
        {
            foreach (var game in _gamesController.InstalledGames)
            {
                if (game.Title != null && _imageCache.ContainsKey(game.Title))
                {
                    // --- Image Check ---
                    string? oldCover = _imageCache[game.Title].CoverUrl;
                    if (!string.IsNullOrEmpty(oldCover))
                    {
                        string fileName = System.IO.Path.GetFileName(oldCover);
                        string expectedPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache", fileName);

                        if (System.IO.File.Exists(expectedPath))
                        {
                            game.CoverImageUrl = expectedPath;
                        }
                        else
                        {
                            // THE FILE DOES NOT EXIST: Total reset to force the download of the image again
                            game.CoverImageUrl = "";
                            _imageCache[game.Title].CoverUrl = "";
                        }
                    }
                    // --- ICON CHECK ---
                    string? oldIcon = _imageCache[game.Title].IconUrl;
                    if (!string.IsNullOrEmpty(oldIcon))
                    {
                        string fileName = System.IO.Path.GetFileName(oldIcon);
                        string expectedPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache", fileName);

                        if (System.IO.File.Exists(expectedPath))
                        {
                            game.IconImageUrl = expectedPath;
                        }
                        else
                        {
                            game.IconImageUrl = "";
                            _imageCache[game.Title].IconUrl = "";
                        }
                    }
                }
            }
            // We go through the folders and remove uninstalled games
            var installedTitles = _gamesController.InstalledGames.Select(g => g.Title).ToList();
            var gamesToForget = _imageCache.Keys.Where(titolo => !installedTitles.Contains(titolo)).ToList();
            foreach (var title in gamesToForget)
            {
                _imageCache.Remove(title);
            }
            foreach (var folder in _folderController.Folders)
            {
                folder.Games.RemoveWhere(gameTitle => !installedTitles.Contains(gameTitle));
            }
            _folderController.PopulateSystemFolders(_gamesController.InstalledGames);
        }
    }
}