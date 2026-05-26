using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = System.Windows.Controls.TextBox;

namespace UniversalLauncher
{
    public partial class MainWindow
    {
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

        private async Task PerformLibrarySync(Wpf.Ui.Controls.Button btnSync)
        {
            // Feedback visivo sul bottone del pannello
            var originalContent = btnSync.Content;
            btnSync.Content = "Syncing...";
            btnSync.IsEnabled = false;

            await _gamesController.ScanAndLoadGamesAsync();
            _libraryService.ApplyScanResults();
            _libraryService.SaveAll();
            RefreshFoldersUI();
            _libraryService.CleanUpImageCache();

            // Ripristina il bottone
            btnSync.Content = originalContent;
            btnSync.IsEnabled = true;
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

        private async void BtnCreateFolder_Click(object sender, RoutedEventArgs e)
        {
            // Popup to ask the name of the new folder
            var input = new Wpf.Ui.Controls.TextBox { PlaceholderText = "E.g. RPG Games" };
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost) { Title = "Create new folder", Content = input, PrimaryButtonText = "Create", CloseButtonText = "Cancel" };
            // If the user clicks "Create", we check if the name is valid and, if it is, we create the folder. Otherwise we show an error message.
            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                string newName = input.Text.Trim();
                string? error = _folderController.ControlNewName(newName);
                if (error != null)
                {
                    MessageBox.Show(error, "Error");
                    return;
                }
                _folderController.CreateFolder(newName);
                _libraryService.SaveAll();
                RefreshFoldersUI();
            }
        }

        // Toggles the visibility of the settings drawer
        private void ToggleSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SettingsDrawer.Visibility == System.Windows.Visibility.Collapsed)
            {
                SettingsDrawer.Visibility = System.Windows.Visibility.Visible;
                BtnSettings.Visibility = Visibility.Collapsed; // NASCONDE l'ingranaggio
            }
            else
            {
                SettingsDrawer.Visibility = System.Windows.Visibility.Collapsed;
                BtnSettings.Visibility = Visibility.Visible; // MOSTRA l'ingranaggio
            }
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
    }
}
