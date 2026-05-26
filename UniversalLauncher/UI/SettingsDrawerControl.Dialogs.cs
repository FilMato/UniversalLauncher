using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UniversalLauncher.Services;

namespace UniversalLauncher
{
    public partial class SettingsDrawerControl
    {
        private async void AddManualGameDialog()
        {
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock { Text = "1. Search Game Title", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var searchPanel = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txtSearch = new Wpf.Ui.Controls.TextBox { PlaceholderText = "E.g. The Witcher 3" };
            Grid.SetColumn(txtSearch, 0);
            var btnSearch = new Wpf.Ui.Controls.Button { Content = "Search", Margin = new Thickness(10, 0, 0, 0), Cursor = System.Windows.Input.Cursors.Hand };
            Grid.SetColumn(btnSearch, 1);
            searchPanel.Children.Add(txtSearch);
            searchPanel.Children.Add(btnSearch);
            panel.Children.Add(searchPanel);

            var comboResults = new System.Windows.Controls.ComboBox { DisplayMemberPath = "Value", SelectedValuePath = "Key", IsEnabled = false, Margin = new Thickness(0, 0, 0, 20) };
            panel.Children.Add(comboResults);

            btnSearch.Click += async (s, e) =>
            {
                btnSearch.IsEnabled = false;
                btnSearch.Content = "Wait...";
                var results = await _steamGridService.SearchGamesListAsync(txtSearch.Text);
                comboResults.ItemsSource = results;
                comboResults.IsEnabled = results.Count > 0;
                if (results.Count > 0) comboResults.SelectedIndex = 0;
                else MessageBox.Show("No games found. Try a different name.");
                btnSearch.IsEnabled = true;
                btnSearch.Content = "Search";
            };

            panel.Children.Add(new TextBlock { Text = "2. Platform", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var comboPlatform = new System.Windows.Controls.ComboBox
            {
                ItemsSource = new[] { "PC (Custom)", "Steam", "Epic Games", "GOG", "EA App", "Ubisoft Connect", "Battle.net", "Microsoft Store/Xbox", "Riot", "Rockstar Games" },
                SelectedIndex = 0,
                Margin = new Thickness(0, 0, 0, 20)
            };
            panel.Children.Add(comboPlatform);

            panel.Children.Add(new TextBlock { Text = "3. Executable File (.exe)", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var exePanel = new Grid();
            exePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            exePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txtExe = new Wpf.Ui.Controls.TextBox { IsReadOnly = true, PlaceholderText = "Select the .exe file..." };
            Grid.SetColumn(txtExe, 0);
            var btnBrowse = new Wpf.Ui.Controls.Button { Content = "Browse...", Margin = new Thickness(10, 0, 0, 0), Cursor = System.Windows.Input.Cursors.Hand };
            Grid.SetColumn(btnBrowse, 1);
            exePanel.Children.Add(txtExe);
            exePanel.Children.Add(btnBrowse);
            panel.Children.Add(exePanel);

            btnBrowse.Click += (s, e) =>
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Executable Files (*.exe)|*.exe|All files (*.*)|*.*",
                    Title = "Select Game Executable"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    txtExe.Text = openFileDialog.FileName;
                }
            };

            panel.Children.Add(new TextBlock { Text = "4. Launch Arguments (Optional)", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 5) });
            var txtArguments = new Wpf.Ui.Controls.TextBox { PlaceholderText = "E.g. -g \"C:\\Games\\Zelda.wua\"" };
            panel.Children.Add(txtArguments);

            // Notice that we use _dialogHost here instead of this.RootDialogHost!
            var dialog = new Wpf.Ui.Controls.ContentDialog(_dialogHost)
            {
                Title = "Add Game Manually",
                Content = panel,
                PrimaryButtonText = "Add Game",
                CloseButtonText = "Cancel"
            };

            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                if (comboResults.SelectedItem is KeyValuePair<string, string> selectedGame && !string.IsNullOrEmpty(txtExe.Text))
                {
                    string officialTitle = selectedGame.Value;
                    string platform = comboPlatform.SelectedItem.ToString() ?? "PC";
                    string exePath = $"\"{txtExe.Text}\"";

                    var newGame = new UniversalLauncher.Models.GamesModels.ManualGame
                    {
                        Title = officialTitle,
                        Platform = platform,
                        ExePath = exePath,
                        Arguments = txtArguments.Text.Trim()
                    };

                    _libraryService.AddManualGameToMemory(newGame);
                    _libraryService.ApplyScanResults();
                    _libraryService.SaveAll();

                    // Trigger the UI refresh in MainWindow
                    LibraryUpdated?.Invoke();
                }
                else
                {
                    MessageBox.Show("Please search and select a valid game, and choose an executable file.", "Missing Info");
                }
            }
        }

        private async void ScanEmulatorDialog()
        {
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock { Text = "1. Emulator Info", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });

            var txtEmuName = new Wpf.Ui.Controls.TextBox { PlaceholderText = "Emulator Name (e.g. Cemu)", Margin = new Thickness(0, 0, 0, 10) };
            panel.Children.Add(txtEmuName);
            var exePanel = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            exePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            exePanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txtExe = new Wpf.Ui.Controls.TextBox { IsReadOnly = true, PlaceholderText = "Select Emulator .exe" };
            var btnBrowseExe = new Wpf.Ui.Controls.Button { Content = "Browse...", Margin = new Thickness(10, 0, 0, 0) };

            Grid.SetColumn(txtExe, 0);
            Grid.SetColumn(btnBrowseExe, 1);
            exePanel.Children.Add(txtExe);
            exePanel.Children.Add(btnBrowseExe);
            panel.Children.Add(exePanel);

            txtEmuName.TextChanged += (s, e) =>
            {
                if (_libraryService.EmulatorPaths.TryGetValue(txtEmuName.Text.Trim(), out string? savedPath))
                {
                    txtExe.Text = savedPath;
                }
            };
            btnBrowseExe.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Executable (*.exe)|*.exe" };
                if (dialog.ShowDialog() == true) txtExe.Text = dialog.FileName;
            };

            panel.Children.Add(new TextBlock { Text = "2. Games Folder", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var romPanel = new Grid { Margin = new Thickness(0, 0, 0, 20) };
            romPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            romPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var txtRomFolder = new Wpf.Ui.Controls.TextBox { IsReadOnly = true, PlaceholderText = "Select folder containing games" };
            var btnBrowseRom = new Wpf.Ui.Controls.Button { Content = "Browse...", Margin = new Thickness(10, 0, 0, 0) };

            Grid.SetColumn(txtRomFolder, 0);
            Grid.SetColumn(btnBrowseRom, 1);
            romPanel.Children.Add(txtRomFolder);
            romPanel.Children.Add(btnBrowseRom);
            panel.Children.Add(romPanel);

            btnBrowseRom.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Select Games Folder" };
                if (dialog.ShowDialog() == true) txtRomFolder.Text = dialog.FolderName;
            };

            panel.Children.Add(new TextBlock { Text = "3. Scan Settings", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
            var txtArgs = new Wpf.Ui.Controls.TextBox { Text = "-g {ROM_PATH}", PlaceholderText = "Argument Template", Margin = new Thickness(0, 0, 0, 10) };
            panel.Children.Add(txtArgs);

            // Notice that we use _dialogHost here instead of this.RootDialogHost!
            var dialog = new Wpf.Ui.Controls.ContentDialog(_dialogHost)
            {
                Title = "Scan Emulator Games",
                Content = panel,
                PrimaryButtonText = "Scan & Add",
                CloseButtonText = "Cancel"
            };

            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                string emuName = txtEmuName.Text.Trim();
                string exePath = txtExe.Text.Trim();
                string romFolder = txtRomFolder.Text.Trim();
                string args = txtArgs.Text.Trim();

                if (string.IsNullOrEmpty(exePath) || string.IsNullOrEmpty(romFolder))
                {
                    MessageBox.Show("Please fill in all the required fields.", "Error");
                    return;
                }
                if (!string.IsNullOrEmpty(emuName))
                {
                    _libraryService.EmulatorPaths[emuName] = exePath;
                }

                var scanner = new EmulatorScanner();
                var newGames = scanner.ScanEmulatorRoms(exePath, romFolder, args);

                if (newGames.Count == 0)
                {
                    MessageBox.Show("No games found in the selected folder.", "Scan Complete");
                    return;
                }
                foreach (var game in newGames)
                {
                    _libraryService.AddManualGameToMemory(game);
                }
                _libraryService.ApplyScanResults();
                _libraryService.SaveAll();

                // Trigger the UI refresh in MainWindow
                LibraryUpdated?.Invoke();

                MessageBox.Show($"Successfully found and added {newGames.Count} games!", "Success");
            }
        }
    }
}