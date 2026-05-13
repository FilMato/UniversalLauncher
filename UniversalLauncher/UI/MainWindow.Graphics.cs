using System.Windows;
using System.Windows.Controls;
using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;
using UniversalLauncher.Services;
using Wpf.Ui.Controls;
using MenuItem = System.Windows.Controls.MenuItem;
using TextBlock = System.Windows.Controls.TextBlock;

namespace UniversalLauncher
{
    public partial class MainWindow
    {
        private SteamGridService _steamGridService = new SteamGridService();

        // This method is "async" (asynchronous), it will never block your GUI!
        private async void LoadGameImageAsync(Game game, Border borderControl, bool isIcon)
        {
            // Check if we already have the image URL (icon or cover) for this game. If not, we download it from SteamGridDB.
            bool needsFetch = isIcon ? string.IsNullOrEmpty(game.IconImageUrl) : string.IsNullOrEmpty(game.CoverImageUrl);
            if (needsFetch)
            {
                await _steamGridService.FetchImagesForGameAsync(game);  // Download the full image package from SteamGridDB (ID + Icon + Grid)
            }

            // Choose which URL to use based on the isIcon parameter
            string finalUrl = isIcon ? game.IconImageUrl : game.CoverImageUrl;
            if (!string.IsNullOrEmpty(finalUrl))
            {
                try
                {
                    // Reconstruct the image from the URL
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(finalUrl, UriKind.Absolute);
                    bitmap.DecodePixelWidth = isIcon ? 32 : 160; // RAM optimization: 32px for icons, 160px for covers
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Apply the "paint" to the border
                    borderControl.Background = new System.Windows.Media.ImageBrush(bitmap)
                    {
                        Stretch = isIcon ? System.Windows.Media.Stretch.Uniform : System.Windows.Media.Stretch.UniformToFill
                    };

                    // Save the URL in the local cache to avoid re-downloading it in the future
                    if (game.Title != null)
                    {
                        if (!_imageCache.ContainsKey(game.Title)) _imageCache[game.Title] = new ImageCache();
                        if (isIcon) _imageCache[game.Title].IconUrl = finalUrl;
                        else _imageCache[game.Title].CoverUrl = finalUrl;
                        RequestDeferredSave();
                    }
                }
                catch { }
            }
        }

        private void DrawFolder(GameFolder folder)
        {
            // Check if the filtered game is present in the folder, if a search query is in progress
            var filteredGames = folder.Games
                .Where(g => string.IsNullOrEmpty(_searchQuery) || g.ToLower().Contains(_searchQuery))
                .ToList();

            if (!string.IsNullOrEmpty(_searchQuery) && filteredGames.Count == 0) return;

            // Try to read the saved color. If there's a typo in the HEX, we don't let the app crash.
            System.Windows.Media.Color folderColor;
            try
            {
                folderColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(folder.BackgroundColor ?? "#28282D");
            }
            catch
            {
                folderColor = System.Windows.Media.Color.FromRgb(40, 40, 45); // Emergency gray
            }
            // Custom Color and Hover Effect
            var defaultBrush = new System.Windows.Media.SolidColorBrush(folderColor);
            var hoverBrush = new System.Windows.Media.SolidColorBrush(folderColor) { Opacity = 0.8 };
            // Creation of the Frame (Card) for the folder
            var cardBorder = new Border
            {
                Margin = new Thickness(0, 0, 20, 20),
                Width = 270,
                Height = 280,
                CornerRadius = new CornerRadius(16),
                Background = defaultBrush,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = folder.Name
            };
            cardBorder.MouseEnter += (s, e) => cardBorder.Background = hoverBrush;
            cardBorder.MouseLeave += (s, e) => cardBorder.Background = defaultBrush;
            // Open folder on click
            cardBorder.MouseLeftButtonDown += (s, ev) => OpenFolder(folder.Name);

            // Add a menu that can be opened with the right click 
            cardBorder.ContextMenu = CreateFolderContextMenu(folder);

            // Card content: Icon + Folder Name + Number of Games (FILTERED)
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            // Custom icon logic
            var iconSymbol = Wpf.Ui.Controls.SymbolRegular.Folder24; // Default safety icon
            // Try to convert the saved text into the graphic symbol
            if (System.Enum.TryParse(folder.IconSymbolName, out Wpf.Ui.Controls.SymbolRegular parsedSymbol))
            {
                iconSymbol = parsedSymbol;
            }
            else if (folder.IsSystemFolder)// If it is a system folder and doesn't have a saved custom icon yet, use the old defaults
            {
                iconSymbol = folder.Name == "All games" ? Wpf.Ui.Controls.SymbolRegular.Library24 : Wpf.Ui.Controls.SymbolRegular.Folder24;
            }

            // Add the icon
            stack.Children.Add(new Wpf.Ui.Controls.SymbolIcon { Symbol = iconSymbol, FontSize = 48, Margin = new Thickness(0, 0, 0, 10) });
            // Larger folder title
            stack.Children.Add(new TextBlock { Text = folder.Name, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, FontSize = 18 });
            // Subtitle
            stack.Children.Add(new TextBlock { Text = $"{filteredGames.Count} games", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 13, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) { Opacity = 0.75 }, Margin = new Thickness(0, 4, 0, 0) });
            // Assembly and Activation
            cardBorder.Child = stack;
            MainContainer.Children.Add(cardBorder);
        }

        private void DrawFullListView()
        {
            // Header preparation
            MainContainer.Children.Clear();
            TxtPageTitle.Text = "Installed Games";
            BtnBack.Visibility = Visibility.Collapsed;
            if (BtnCreateFolder != null) BtnCreateFolder.Visibility = Visibility.Visible;
            if (MainContainer is WrapPanel wp) { wp.ItemWidth = double.NaN; wp.ItemHeight = double.NaN; }

            // Creation of a vertical container for the folders
            var listContainer = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
            System.Windows.Data.BindingOperations.SetBinding(listContainer, FrameworkElement.WidthProperty, new System.Windows.Data.Binding("ActualWidth") { Source = MainContainer });

            // For each folder, we create a dropdown (Expander) containing the filtered games
            foreach (var folder in _folderController.Folders)
            {
                var filteredGames = folder.Games
                    .Where(g => string.IsNullOrEmpty(_searchQuery) || g.ToLower().Contains(_searchQuery))
                    .ToList();

                if (!string.IsNullOrEmpty(_searchQuery) && filteredGames.Count == 0) continue;// Hide the dropdown if we are searching and there are no results

                // If there is a search query, automatically open folders that contain results
                bool autoOpen = !string.IsNullOrEmpty(_searchQuery) || folder.Name == _currentOpenFolderName;
                var expander = new Expander { Header = $"{folder.Name.ToUpper()} ({filteredGames.Count})", IsExpanded = autoOpen, Margin = new Thickness(0, 0, 0, 10), FontWeight = FontWeights.Bold };// if there are games start an internal loop and draw the games
                expander.ContextMenu = CreateFolderContextMenu(folder);
                var gamesStack = new StackPanel { Margin = new Thickness(20, 5, 0, 5) };

                if (filteredGames.Count > 0)
                {
                    foreach (var gameTitle in filteredGames) gamesStack.Children.Add(CreateCompactGameRow(gameTitle));
                }
                else
                {
                    gamesStack.Children.Add(new TextBlock { Text = "This folder is empty.", FontStyle = FontStyles.Italic, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray), Margin = new Thickness(10, 5, 0, 5) });
                }
                expander.Content = gamesStack;

                // If this is the folder we auto-opened because of memory, scroll the screen to show it!
                if (folder.Name == _currentOpenFolderName && string.IsNullOrEmpty(_searchQuery))
                {
                    expander.Loaded += (s, ev) => expander.BringIntoView();
                }
                listContainer.Children.Add(expander);
            }
            MainContainer.Children.Add(listContainer);
        }

        private void DrawGameCard(string gameTitle)
        {
            // Retrieve complete game information to get the platform
            if (!_gamesController.InstalledGamesDict.TryGetValue(gameTitle, out var gameData)) { return; }// If for some absurd reason the game is in the folder but not among the installed ones, we avoid the crash and don't draw the card
            string platforms = gameData != null ? gameData.Platform : "Unknown";

            // Main card container preparation
            var cardBorder = new Border
            {
                Margin = new Thickness(0, 0, 20, 20),
                Width = 160,
                Height = 290,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent),
                BorderThickness = new Thickness(2),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent),
                CornerRadius = new CornerRadius(8),
                Cursor = System.Windows.Input.Cursors.Hand,
                ClipToBounds = true
            };

            // Creation of the tooltip with game name and platform (hover)
            var toolTipPanel = new StackPanel { Margin = new Thickness(5) };
            toolTipPanel.Children.Add(new TextBlock { Text = gameTitle, FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 4) });
            toolTipPanel.Children.Add(new TextBlock { Text = platforms, FontSize = 12, Foreground = System.Windows.Media.Brushes.LightGray });
            var cardToolTip = new ToolTip
            {
                Content = toolTipPanel,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70))
            };
            cardBorder.ToolTip = cardToolTip; // Assign hover to the card

            // Right click to manage folders
            cardBorder.ContextMenu = CreateGameContextMenu(gameTitle);

            // THE INTERNAL GRID (3 rows: Cover, Title, Play Button)
            var grid = new Grid(); ; // Small internal margin to not touch the border
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Cover (takes all possible space)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Play

            // Row 0: Game cover (with placeholder if not available)
            var coverBorder = new Border
            {
                Height = 230,
                VerticalAlignment = VerticalAlignment.Top,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 45)),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                ClipToBounds = true
            };
            var placeholderIcon = new SymbolIcon
            {
                Symbol = SymbolRegular.XboxController24,
                FontSize = 50,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            // Create empty Image object
            var coverImage = new Border { CornerRadius = new CornerRadius(8, 8, 8, 8) };
            // Create a container to overlap them
            var imageContainer = new Grid();
            imageContainer.Children.Add(placeholderIcon); // Placed first, stays underneath
            imageContainer.Children.Add(coverImage);      // Placed second, stays on top and will cover the icon as soon as it loads

            coverBorder.Child = imageContainer;
            Grid.SetRow(coverBorder, 0);

            // Row 1: Game title (with ellipsis if too long)
            var titleLabel = new TextBlock
            {
                Text = gameTitle,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(10, 10, 10, 4),
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            Grid.SetRow(titleLabel, 1);

            // --- Row 2: Play Button ---
            var playButton = new Wpf.Ui.Controls.Button
            {
                Content = "Play",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Play20 },
                HorizontalAlignment = HorizontalAlignment.Stretch, // Makes the button stretch for the full width of the card
                Margin = new Thickness(10, 0, 10, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary // Default gray
            };

            // When the mouse enters, the button colors up
            playButton.MouseEnter += (s, e) => playButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            // When it leaves, it goes back to gray
            playButton.MouseLeave += (s, e) => playButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;

            Grid.SetRow(playButton, 2);


            // Starting the game and "Cooldown"
            playButton.Click += async (s, ev) =>
            {
                if (gameData != null && playButton.IsEnabled)
                {
                    playButton.IsEnabled = false;
                    playButton.Content = "Starting...";
                    playButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Clock24 };

                    LaunchUniversalGame(gameData.GetLaunchCommand());

                    await System.Threading.Tasks.Task.Delay(8000); // 8-second Cooldown

                    playButton.IsEnabled = true;
                    playButton.Content = "Play";
                    playButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play20 };
                }
                ev.Handled = true; // Prevents the click from passing to the underlying card
            };

            // Add everything to the grid
            grid.Children.Add(coverBorder);
            grid.Children.Add(titleLabel);
            grid.Children.Add(playButton);
            cardBorder.Child = grid;

            // Start asynchronous cover download
            if (gameData != null)
            {
                LoadGameImageAsync(gameData, coverImage, false);
            }

            // Animations and Effects (Hover) ONLY FOR THE CARD BORDER
            var defaultBorderColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            var hoverBorderColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));

            cardBorder.MouseEnter += (s, ev) =>
            {
                cardBorder.BorderBrush = hoverBorderColor;
            };
            cardBorder.MouseLeave += (s, ev) =>
            {
                cardBorder.BorderBrush = defaultBorderColor;
            };

            // Add the complete card to the page's main container
            MainContainer.Children.Add(cardBorder);
        }

        private CardAction CreateCompactGameRow(string gameTitle)
        {
            // 1. Retrieve complete game information
            var gameData = _gamesController.InstalledGames.FirstOrDefault(g => g.Title == gameTitle);
            string platforms = gameData != null ? gameData.Platform : "Unknown";

            // 2. Main container preparation (CardAction)
            var card = new CardAction
            {
                Margin = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(5),
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            card.ContextMenu = CreateGameContextMenu(gameTitle);

            // 3. THE INTERNAL GRID (3 columns: Icon, Title, Play Button)
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // Slightly wider for the icon
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Play Button

            // Column 0: Icon
            var iconBorder = new Border
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(5, 0, 5, 0),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 65)),
                ClipToBounds = true // Clips the icon if it goes outside the rounded borders
            };

            // Fetch the online icon
            if (gameData != null)
            {
                LoadGameImageAsync(gameData, iconBorder, true); // true = isIcon
            }

            Grid.SetColumn(iconBorder, 0);

            // Column 1: Game title
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            label.Inlines.Add(new System.Windows.Documents.Run { Text = gameTitle, FontSize = 14, FontWeight = FontWeights.SemiBold });

            if (!string.IsNullOrEmpty(platforms))
            {
                label.Inlines.Add(new System.Windows.Documents.Run { Text = $"   [{platforms}]", FontSize = 12, Foreground = System.Windows.Media.Brushes.DarkGray });
            }
            Grid.SetColumn(label, 1);

            // Column 2: Play Button
            var playButton = new Wpf.Ui.Controls.Button
            {
                Content = "Play",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Play20 },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary
            };

            playButton.MouseEnter += (s, e) => playButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            playButton.MouseLeave += (s, e) => playButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;

            playButton.Click += async (s, ev) =>
            {
                if (gameData != null)
                {
                    playButton.IsEnabled = false;
                    playButton.Content = "Starting...";
                    playButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Clock24 };

                    LaunchUniversalGame(gameData.GetLaunchCommand());

                    await System.Threading.Tasks.Task.Delay(8000);

                    playButton.IsEnabled = true;
                    playButton.Content = "Play";
                    playButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play20 };
                }
                ev.Handled = true;
            };
            Grid.SetColumn(playButton, 2);

            grid.Children.Add(iconBorder);
            grid.Children.Add(label);
            grid.Children.Add(playButton);

            card.Content = grid;

            return card;
        }

        private ContextMenu CreateGameContextMenu(string gameTitle)
        {
            // Main menu preparation
            var contextMenu = new ContextMenu();
            var manageItem = new MenuItem { Header = "Manage folders" };

            // Retrieve custom folders
            var customFolders = _folderController.Folders.Where(f => !f.IsSystemFolder).ToList();
            if (customFolders.Count > 0)
            {
                foreach (var folder in customFolders)
                {
                    var folderItem = new MenuItem { Header = folder.Name, IsCheckable = true, IsChecked = folder.Games.Contains(gameTitle), StaysOpenOnClick = true };
                    folderItem.Click += (s, ev) =>
                    {
                        _folderController.AssignGameToFolder(gameTitle, folder, folderItem.IsChecked);
                        SaveAll();
                    };
                    manageItem.Items.Add(folderItem);
                }
            }
            else
            {
                manageItem.Items.Add(new MenuItem { Header = "No custom folders", IsEnabled = false });
            }
            contextMenu.Items.Add(manageItem);

            // Real-time update
            contextMenu.Closed += (s, ev) =>
            {
                if (_isListView)
                {
                    // If we are in list mode, update the list interface
                    DrawFullListView();
                }
                else
                {
                    // If we are in grid mode, reload the current folder
                    if (!string.IsNullOrEmpty(_currentOpenFolderName))
                    {
                        OpenFolder(_currentOpenFolderName);
                    }
                }
            };
            return contextMenu;
        }

        private ContextMenu CreateFolderContextMenu(GameFolder folder)
        {
            var contextMenu = new ContextMenu();

            // 1. Personalize
            var personalizeItem = new MenuItem { Header = "Personalize", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Color24 } };
            personalizeItem.Click += (s, ev) => PersonalizeFolderDialog(folder);
            contextMenu.Items.Add(personalizeItem);

            contextMenu.Items.Add(new Separator());

            // 2. Movement
            var moveLeftItem = new MenuItem { Header = "Move Left / Up", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.ArrowLeft24 } };
            moveLeftItem.Click += (s, ev) => MoveFolder(folder, -1);
            contextMenu.Items.Add(moveLeftItem);

            var moveRightItem = new MenuItem { Header = "Move Right / Down", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.ArrowRight24 } };
            moveRightItem.Click += (s, ev) => MoveFolder(folder, 1);
            contextMenu.Items.Add(moveRightItem);

            // 3. Rename and Delete (Only for non-system folders)
            if (!folder.IsSystemFolder)
            {
                contextMenu.Items.Add(new Separator());

                var renameItem = new MenuItem { Header = "Rename", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Edit24 } };
                renameItem.Click += (s, ev) => RenameFolderDialog(folder);
                contextMenu.Items.Add(renameItem);

                var deleteItem = new MenuItem { Header = "Delete", Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red), Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Delete24, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red) } };
                deleteItem.Click += (s, ev) => DeleteFolderDialog(folder);
                contextMenu.Items.Add(deleteItem);
            }

            return contextMenu;
        }
    }
}