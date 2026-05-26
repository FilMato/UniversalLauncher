using System;
using System.Windows;
using System.Windows.Controls;
using UniversalLauncher.Services;

namespace UniversalLauncher
{
    public partial class SettingsDrawerControl : UserControl
    {
        private LibraryService _libraryService = null!;
        private SteamGridService _steamGridService = null!;
        private Wpf.Ui.Controls.ContentDialogHost _dialogHost = null!;

        public event Action? LibraryUpdated;
        public event Action? CloseRequested;

        public event Action<Wpf.Ui.Controls.Button>? SyncRequested;

        public SettingsDrawerControl()
        {
            InitializeComponent();
        }

        // Called by MainWindow to pass the necessary services securely
        public void Initialize(LibraryService libraryService, SteamGridService steamGridService, Wpf.Ui.Controls.ContentDialogHost dialogHost)
        {
            _libraryService = libraryService;
            _steamGridService = steamGridService;
            _dialogHost = dialogHost;
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke();
        }

        private void AddManualGameButton_Click(object sender, RoutedEventArgs e)
        {
            AddManualGameDialog();
        }

        private void ScanEmulatorButton_Click(object sender, RoutedEventArgs e)
        {
            ScanEmulatorDialog();
        }

        private void DrawerSyncButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn)
            {
                SyncRequested?.Invoke(btn);
            }
        }
    }
}