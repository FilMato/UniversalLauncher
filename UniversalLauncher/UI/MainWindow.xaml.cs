using System.Windows;
using UniversalLauncher.Services;
using Wpf.Ui.Controls;

namespace UniversalLauncher
{
    public partial class MainWindow : FluentWindow
    {
        private FolderController _folderController = new FolderController();
        private GamesController _gamesController = new GamesController();
        private string _currentOpenFolderName = "";
        private bool _isListView = false;
        private string _searchQuery = "";
        private System.Windows.Threading.DispatcherTimer? _saveTimer;
        private LibraryService _libraryService;

        public MainWindow()
        {
            InitializeComponent();
            _libraryService = new LibraryService(
                _folderController,
                _gamesController,
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library_v1.json")
            );
            // Inizializzazione Timer Debounce
            // Inizialization of the Debounce Timer for saving the library
            _saveTimer = new System.Windows.Threading.DispatcherTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(2);
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop(); // stops the timer until the next request
                _libraryService.SaveAll();      
            };

            this.Loaded += MainWindow_Loaded;
            this.Closing += (s, e) => _libraryService.SaveAll();
        }

        private void RequestDeferredSave()
        {
            // This way, if the user makes multiple changes in a short time, we won't save the library multiple times unnecessarily, but only once after they've stopped making changes for 2 seconds.
            _saveTimer?.Stop();
            _saveTimer?.Start();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApiKeyControl();
            await LoadGamesAndFoldersAsync();
            _libraryService.CleanUpImageCache();
            // Passiamo gli strumenti di MainWindow al pannello Settings
            SettingsControl.Initialize(_libraryService, _steamGridService, RootDialogHost);
            // Ascoltiamo i comandi dal pannello Settings
            SettingsControl.LibraryUpdated += RefreshFoldersUI;
            SettingsControl.CloseRequested += () =>
            {
                SettingsDrawer.Visibility = Visibility.Collapsed;
                BtnSettings.Visibility = Visibility.Visible;
            };
            SettingsControl.SyncRequested += async (syncButton) => await PerformLibrarySync(syncButton);
        }
    }
}