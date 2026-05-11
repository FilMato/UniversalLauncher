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
            _saveTimer = new System.Windows.Threading.DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(2);
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop(); // Ferma il timer per non farlo scattare a ripetizione
                SalvaTutto();      // Esegue il salvataggio vero e proprio
            };

            this.Loaded += MainWindow_Loaded;
            this.Closing += (s, e) => SalvaTutto();
        }
        private void RequestDeferredSave()
        {
            // Spegnendo e riaccendendo il timer, il conteggio dei 2 secondi riparte da zero!
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ControllaApiKey();
            await LoadGamesAndFoldersAsync();
            CleanUpImageCache();
        }
        private void ControllaApiKey()
        {
            string secretPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "secrets.json");
            bool keyMancante = false;

            // Se il file non esiste, lo creiamo noi come "template"
            if (!System.IO.File.Exists(secretPath))
            {
                string template = "{\n  \"SteamGridApiKey\": \"YOUR_API_KEY_HERE\"\n}";
                System.IO.File.WriteAllText(secretPath, template, System.Text.Encoding.UTF8);
                keyMancante = true;
            }
            else
            {
                // Se esiste, controlliamo cosa c'è scritto dentro
                try
                {
                    string json = System.IO.File.ReadAllText(secretPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    string apiKey = doc.RootElement.GetProperty("SteamGridApiKey").GetString() ?? "";

                    // Se è vuota o ha ancora la scritta di default, mostriamo l'avviso
                    if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY_HERE")
                    {
                        keyMancante = true;
                    }
                }
                catch
                {
                    // Se il file è corrotto o formattato male
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

        //Questa funzione serve ad eliminare le immagini che non sono più collegate a nessun gioco installato, per evitare di accumulare file inutili nella cartella ImageCache.
        private void CleanUpImageCache()
        {
            try
            {
                // Trova la cartella ImageCache
                string cacheFolder = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
                // Se la cartella non esiste ancora, non c'è nulla da pulire
                if (!System.IO.Directory.Exists(cacheFolder)) return;

                // 1. Raccogliamo in una lista "intelligente" (HashSet) tutti i percorsi delle immagini IN USO
                var activeImagePaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var game in _gamesController.InstalledGames)
                {
                    if (!string.IsNullOrWhiteSpace(game.CoverImageUrl))
                        activeImagePaths.Add(System.IO.Path.GetFullPath(game.CoverImageUrl));

                    if (!string.IsNullOrWhiteSpace(game.IconImageUrl))
                        activeImagePaths.Add(System.IO.Path.GetFullPath(game.IconImageUrl));
                }
                // 2. Prendiamo tutti i file fisicamente presenti nella cartella
                string[] filesInCache = System.IO.Directory.GetFiles(cacheFolder);
                // 3. Per ogni file fisico, se non è nella nostra lista di file in uso, lo eliminiamo!
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

        // Carica i giochi installati e popola le cartelle di sistema (All games e Uncategorized) in un solo colpo, poi disegna l'interfaccia
        private async Task LoadGamesAndFoldersAsync()
        {
            _folderController.InizializzaCartelleDiSistema();
            CaricaTutto(); // Carica le tue cartelle e gli URL salvati

            // Intanto visualizziamo subito l'interfaccia con i giochi salvati l'ultima volta
            RefreshFoldersUI();
            // Ora avvia la scansione parallela in background senza bloccare la UI
            await _gamesController.ScanAndLoadGamesAsync();
            // Diamo ai giochi appena trovati gli URL che già conosciamo
            ApplyScanResults();
            // Ridisegna l'interfaccia un'ultima volta con i dati freschi appena scansionati
            RefreshFoldersUI();
        }

        // Gestione della barra di ricerca
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Aggiorna la query di ricerca ogni volta che l'utente digita qualcosa, pulendo cio che siscrive per rendere la ricerca più flessibile
            var searchBox = sender as TextBox ?? sender as Wpf.Ui.Controls.TextBox;
            _searchQuery = searchBox?.Text?.Trim().ToLower() ?? "";

            // Quando digiti, ricarica all'istante l'interfaccia
            if (!string.IsNullOrEmpty(_currentOpenFolderName)) OpenFolder(_currentOpenFolderName);
            else RefreshFoldersUI();
        }

        private void OpenFolder(string name)
        {
            // Aggiornamento intestazione
            _currentOpenFolderName = name;
            TxtTitoloPagina.Text = name.ToUpper();
            BtnIndietro.Visibility = Visibility.Visible;
            if (BtnCreaCartella != null) BtnCreaCartella.Visibility = Visibility.Collapsed;

            // pulisce l'interfaccia e disegna i giochi della cartella selezionata,resettando le dimensioni in modo che i nuovi elementi si adattino dinamicamente allo spazio 
            MainContainer.Children.Clear();
            if (MainContainer is WrapPanel wp)
            {
                wp.ItemWidth = double.NaN;
                wp.ItemHeight = double.NaN;
            }

            // Inserisce i giochi nella cartella e li filtra in base alla query di ricerca, se presente
            var currentFolder = _folderController.Folders.FirstOrDefault(f => f.Name == name);
            if (currentFolder != null && currentFolder.Games.Count > 0)
            {
                var filteredGames = currentFolder.Games
                    .Where(g => string.IsNullOrEmpty(_searchQuery) || g.ToLower().Contains(_searchQuery))
                    .ToList();

                // Se ci sono giochi che corrispondono alla ricerca, li disegna, altrimenti mostra un messaggio
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

        // Funzione per tornare alla visualizzazione principale delle cartelle
        private void BtnIndietro_Click(object sender, RoutedEventArgs e)
        {
            TxtTitoloPagina.Text = "Installed Games";
            BtnIndietro.Visibility = Visibility.Collapsed;
            if (BtnCreaCartella != null) BtnCreaCartella.Visibility = Visibility.Visible;
            RefreshFoldersUI();
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            // Feedback visivo
            var originalContent = BtnSync.Content;
            BtnSync.Content = "Syncing...";
            BtnSync.IsEnabled = false;
            // Scansione parallela asincrona
            await _gamesController.ScanAndLoadGamesAsync();
            // Aggiorna gli URL delle immagini dei giochi appena scansionati con quelli già presenti nella cache, se disponibili
            ApplyScanResults();
            SalvaTutto();
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

        // Funzione per passare dalla visualizzazione a griglia a quella a lista, e viceversa
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
                _currentOpenFolderName = ""; // in questo modo quando torni alla visualizzazione a griglia, mostri tutte le cartelle 
                RefreshFoldersUI();
            }
        }

        // Funzione per far parture i giochi
        private void LaunchUniversalGame(string launchCommand)
        {
            if (string.IsNullOrEmpty(launchCommand))
            {
                System.Windows.MessageBox.Show("Comando di avvio mancante per questo gioco.", "Errore");
                return;
            }
            try
            {
                string fileName = launchCommand;
                string arguments = "";

                // CASO 1: Giochi del Microsoft Store / UWP (es. Minecraft Launcher)
                if (launchCommand.StartsWith("explorer.exe ", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = "explorer.exe"; // Il programma da avviare è l'esplora risorse
                    arguments = launchCommand.Substring("explorer.exe ".Length); // Il resto è l'argomento
                }
                // CASO 2: Percorsi racchiusi tra virgolette con argomenti finali (es. Roblox)
                else if (launchCommand.StartsWith("\""))
                {
                    int secondQuoteIndex = launchCommand.IndexOf("\"", 1);
                    if (secondQuoteIndex > 0)
                    {
                        // Estraiamo solo ciò che è dentro le virgolette
                        fileName = launchCommand.Substring(1, secondQuoteIndex - 1);
                        // Estraiamo ciò che c'è DOPO le virgolette (gli argomenti)
                        if (launchCommand.Length > secondQuoteIndex + 1)
                        {
                            arguments = launchCommand.Substring(secondQuoteIndex + 1).Trim();
                        }
                    }
                }
                // CASO 3: Percorsi senza virgolette ma con argomenti (es. C:\gioco.exe -run)
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
                System.Windows.MessageBox.Show($"Impossibile avviare il gioco.\nErrore: {ex.Message}", "Errore di avvio");
            }
        }

        // Salvataggio della libreria
        public void SalvaTutto()
        {
            try
            {
                var dataToSave = new LibrarySaveData();

                // Salviamo tutte le cartelle
                dataToSave.Folders = _folderController.Folders.ToList();
                dataToSave.CachedImages = _imageCache;

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(dataToSave, options);
                System.IO.File.WriteAllText(_configPath, json, System.Text.Encoding.UTF8);
            }
            catch { }
        }

        // Caricamento della libreria
        private void CaricaTutto()
        {
            if (!System.IO.File.Exists(_configPath)) return;
            try
            {
                string json = System.IO.File.ReadAllText(_configPath);
                var loadedData = System.Text.Json.JsonSerializer.Deserialize<LibrarySaveData>(json);
                if (loadedData != null)
                {
                    // Fusione intelligente delle cartelle
                    if (loadedData.Folders != null)
                    {
                        // 1. Mettiamo da parte le cartelle di sistema create di default dal programma (per non perdere i giochi al loro interno)
                        var backupSystemFolders = _folderController.Folders.Where(f => f.IsSystemFolder).ToList();
                        // 2. Svuotiamo completamente la lista a schermo
                        _folderController.Folders.Clear();
                        // 3. Ricostruiamo la lista leggendo il salvataggio riga per riga 
                        foreach (var savedFolder in loadedData.Folders)
                        {
                            // Cerchiamo se questa cartella salvata corrisponde a una delle nostre cartelle di sistema messe da parte
                            var systemFolder = backupSystemFolders.FirstOrDefault(f => f.Name == savedFolder.Name);

                            if (systemFolder != null)
                            {
                                // È una cartella di sistema: aggiorniamo grafica e la aggiungiamo alla lista
                                systemFolder.BackgroundColor = savedFolder.BackgroundColor;
                                systemFolder.IconSymbolName = savedFolder.IconSymbolName;
                                _folderController.Folders.Add(systemFolder);
                            }
                            else
                            {
                                // È una cartella personalizzata: la aggiungiamo direttamente
                                _folderController.Folders.Add(savedFolder);
                            }
                        }
                        // 4. Caso di sicurezza: se in futuro aggiungiamo una nuova cartella di sistema al programma 
                        // che non era presente nel vecchio salvataggio, assicuriamoci di non perderla e di metterla in fondo
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
                    //Estraiamo solo il nome del file e lo ricongiungiamo alla cartella attuale
                    string oldCover = _imageCache[game.Title].CoverUrl;
                    if (!string.IsNullOrEmpty(oldCover))
                    {
                        string fileName = System.IO.Path.GetFileName(oldCover);
                        game.CoverImageUrl = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache", fileName);
                        _imageCache[game.Title].CoverUrl = game.CoverImageUrl; // Aggiorna la memoria corretta
                    }

                    string oldIcon = _imageCache[game.Title].IconUrl;
                    if (!string.IsNullOrEmpty(oldIcon))
                    {
                        string fileName = System.IO.Path.GetFileName(oldIcon);
                        game.IconImageUrl = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ImageCache", fileName);
                        _imageCache[game.Title].IconUrl = game.IconImageUrl; // Aggiorna la memoria corretta
                    }
                }
            }
            // Passiamo al setaccio le cartelle e rimuoviamo i giochi disinstallati
            var installedTitles = _gamesController.InstalledGames.Select(g => g.Title).ToList();
            foreach (var folder in _folderController.Folders)
            {
                folder.Games.RemoveWhere(gameTitle => !installedTitles.Contains(gameTitle));
            }

            _folderController.PopolaCartelleDiSistema(_gamesController.InstalledGames);
        }
    }
}