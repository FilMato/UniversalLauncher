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

        // Questo metodo è "async" (asincrono), non bloccherà mai la tua interfaccia grafica!
        private async void LoadGameImageAsync(Game game, Border borderControl, bool isIcon)
        {
            // Controlliamo se abbiamo già l'URL dell'immagine (icona o copertina) per questo gioco. Se no, lo scarichiamo da SteamGridDB.
            bool needsFetch = isIcon ? string.IsNullOrEmpty(game.IconImageUrl) : string.IsNullOrEmpty(game.CoverImageUrl);
            if (needsFetch)
            {
                await _steamGridService.FetchImagesForGameAsync(game);  // Scarica tutto il pacchetto immagini da SteamGridDB (ID + Icona + Grid)
            }

            // Scegliamo quale URL usare in base al parametro isIcon
            string finalUrl = isIcon ? game.IconImageUrl : game.CoverImageUrl;
            if (!string.IsNullOrEmpty(finalUrl))
            {
                try
                {
                    // Ricostruiamo l'immagine a partire dall'URL
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(finalUrl, UriKind.Absolute);
                    bitmap.DecodePixelWidth = isIcon ? 32 : 160; // Ottimizzazione RAM: 32px per icone, 160px per copertine
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    // Applichiamo la "vernice" al bordo
                    borderControl.Background = new System.Windows.Media.ImageBrush(bitmap)
                    {
                        Stretch = isIcon ? System.Windows.Media.Stretch.Uniform : System.Windows.Media.Stretch.UniformToFill
                    };

                    // Salviamo l'URL nel cache locale per evitare di doverlo riscaricare in futuro
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
            // controllo se il gioco filtrato è presente nella cartella, se c'è una query di ricerca in corso
            var filteredGames = folder.Games
                .Where(g => string.IsNullOrEmpty(_searchQuery) || g.ToLower().Contains(_searchQuery))
                .ToList();

            if (!string.IsNullOrEmpty(_searchQuery) && filteredGames.Count == 0) return;

            // Tentiamo di leggere il colore salvato. Se c'è un errore di battitura nell'HEX, non facciamo crashare l'app.
            System.Windows.Media.Color folderColor;
            try
            {
                folderColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(folder.BackgroundColor ?? "#28282D");
            }
            catch
            {
                folderColor = System.Windows.Media.Color.FromRgb(40, 40, 45); // Grigio di emergenza
            }
            // Colore ed Effetto Hover personalizzato
            var defaultBrush = new System.Windows.Media.SolidColorBrush(folderColor);
            var hoverBrush = new System.Windows.Media.SolidColorBrush(folderColor) { Opacity = 0.8 };
            // Creazione del Riquadro (Card) per la cartella
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
            // Apertura cartella al click
            cardBorder.MouseLeftButtonDown += (s, ev) => OpenFolder(folder.Name);

            //Aggiungiamo un menu apribile con il tasto destro 
            cardBorder.ContextMenu = CreateFolderContextMenu(folder);

            // Contenuto della card: Icona + Nome Cartella + Numero Giochi (FILTRATI)
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            // Logica dell'icona personalizzata
            var iconSymbol = Wpf.Ui.Controls.SymbolRegular.Folder24; // Icona di sicurezza predefinita
            // Proviamo a convertire il testo salvato nel simbolo grafico
            if (System.Enum.TryParse(folder.IconSymbolName, out Wpf.Ui.Controls.SymbolRegular parsedSymbol))
            {
                iconSymbol = parsedSymbol;
            }
            else if (folder.IsSystemFolder)// Se è una cartella di sistema e non ha ancora un'icona personalizzata salvata, usiamo i vecchi default
            {
                iconSymbol = folder.Name == "All games" ? Wpf.Ui.Controls.SymbolRegular.Library24 : Wpf.Ui.Controls.SymbolRegular.Folder24;
            }

            // Aggiungiamo l'icona
            stack.Children.Add(new Wpf.Ui.Controls.SymbolIcon { Symbol = iconSymbol, FontSize = 48, Margin = new Thickness(0, 0, 0, 10) });
            // Titolo della cartella più grande
            stack.Children.Add(new TextBlock { Text = folder.Name, HorizontalAlignment = HorizontalAlignment.Center, FontWeight = FontWeights.Bold, FontSize = 18 });
            // Sottotitolo
            stack.Children.Add(new TextBlock{ Text = $"{filteredGames.Count} games", HorizontalAlignment = HorizontalAlignment.Center, FontSize = 13, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) { Opacity = 0.75 }, Margin = new Thickness(0, 4, 0, 0)});
            // Assemblaggio e Attivazione
            cardBorder.Child = stack;
            MainContainer.Children.Add(cardBorder);
        }

        private void DrawFullListView()
        {
            // Preparazione intestazione
            MainContainer.Children.Clear();
            TxtTitoloPagina.Text = "Installed Games";
            BtnIndietro.Visibility = Visibility.Collapsed;
            if (BtnCreaCartella != null) BtnCreaCartella.Visibility = Visibility.Visible;
            if (MainContainer is WrapPanel wp) { wp.ItemWidth = double.NaN; wp.ItemHeight = double.NaN; }

            // Creazione di un contenitore verticale per le cartelle
            var listContainer = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };
            System.Windows.Data.BindingOperations.SetBinding(listContainer, FrameworkElement.WidthProperty, new System.Windows.Data.Binding("ActualWidth") { Source = MainContainer });

            // Per ogni cartella, creiamo una tendina (Expander) che contiene i giochi filtrati
            foreach (var folder in _folderController.Folders)
            {
                var filteredGames = folder.Games
                    .Where(g => string.IsNullOrEmpty(_searchQuery) || g.ToLower().Contains(_searchQuery))
                    .ToList();

                if (!string.IsNullOrEmpty(_searchQuery) && filteredGames.Count == 0) continue;// Nascondi la tendina se stiamo cercando e non ci sono risultati

                // Se c'è una query di ricerca, apri automaticamente le cartelle che contengono risultati
                bool autoOpen = !string.IsNullOrEmpty(_searchQuery) || folder.Name == _currentOpenFolderName;
                var expander = new Expander { Header = $"{folder.Name.ToUpper()} ({filteredGames.Count})", IsExpanded = autoOpen, Margin = new Thickness(0, 0, 0, 10), FontWeight = FontWeights.Bold };// se ci sono giochi avvia un ciclo interno e disegna i giochi
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

                // Se questa è la cartella che abbiamo auto-aperto per via della memoria, scorriamo lo schermo per farla vedere!
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
            // Recupero le informazioni complete del gioco per avere la piattaforma
            if (!_gamesController.InstalledGamesDict.TryGetValue(gameTitle, out var gameData)){return; }// Se per qualche assurdo motivo il gioco è nella cartella ma non tra gli installati evitiamo il crash e non disegniamo la card
            string piattaforme = gameData != null ? gameData.Platform : "Sconosciuta";

            // Preparazione contenitore principale della card
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

            // Creazione della tooltip con il nome del gioco e la piattaforma (hover)
            var toolTipPanel = new StackPanel { Margin = new Thickness(5) };
            toolTipPanel.Children.Add(new TextBlock { Text = gameTitle, FontWeight = FontWeights.Bold, FontSize = 14, Margin = new Thickness(0, 0, 0, 4) });
            toolTipPanel.Children.Add(new TextBlock { Text = piattaforme, FontSize = 12, Foreground = System.Windows.Media.Brushes.LightGray });
            var cardToolTip = new ToolTip
            {
                Content = toolTipPanel,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70))
            };
            cardBorder.ToolTip = cardToolTip; // Assegno l'hover alla card

            // Tasto destro per gestire le cartelle
            cardBorder.ContextMenu = CreateGameContextMenu(gameTitle);

            // LA GRIGLIA INTERNA (3 righe: Copertina, Titolo, Pulsante Play)
            var grid = new Grid(); ; // Piccolo margine interno per non toccare il bordo
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Copertina (prende tutto lo spazio possibile)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Titolo
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Play

            // Riga 0: Copertina del gioco (con placeholder se non disponibile)
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
            // Creiamo l'oggetto Immagine vuoto
            var coverImage = new Border { CornerRadius = new CornerRadius(8, 8, 8, 8) };
            // Creiamo un contenitore per sovrapporli
            var imageContainer = new Grid();
            imageContainer.Children.Add(placeholderIcon); // Messo per primo, sta sotto
            imageContainer.Children.Add(coverImage);      // Messo per secondo, sta sopra e coprirà l'icona appena si carica

            coverBorder.Child = imageContainer;
            Grid.SetRow(coverBorder, 0);

            // Riga 1: Titolo del gioco (con ellissi se troppo lungo)
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

            // --- Riga 2: Pulsante Play ---
            var playButton = new Wpf.Ui.Controls.Button
            {
                Content = "Play",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Play20 },
                HorizontalAlignment = HorizontalAlignment.Stretch, // Fa allargare il pulsante per tutta la larghezza della card
                Margin = new Thickness(10, 0, 10, 10),
                Cursor = System.Windows.Input.Cursors.Hand,
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary // Grigio di default
            };

            // Quando il mouse entra, il pulsante si colora
            playButton.MouseEnter += (s, e) => playButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            // Quando esce, torna grigio
            playButton.MouseLeave += (s, e) => playButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;

            Grid.SetRow(playButton, 2);


            // Avviare il gioco e il "Cooldown"
            playButton.Click += async (s, ev) =>
            {
                if (gameData != null && playButton.IsEnabled)
                {
                    playButton.IsEnabled = false;
                    playButton.Content = "Starting...";
                    playButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Clock24 };

                    LaunchUniversalGame(gameData.GetLaunchCommand());

                    await System.Threading.Tasks.Task.Delay(8000); // Cooldown di 8 secondi

                    playButton.IsEnabled = true;
                    playButton.Content = "Play";
                    playButton.Icon = new SymbolIcon { Symbol = SymbolRegular.Play20 };
                }
                ev.Handled = true; // Impedisce che il click passi alla card sottostante
            };

            // Aggiungiamo tutto alla griglia
            grid.Children.Add(coverBorder);
            grid.Children.Add(titleLabel);
            grid.Children.Add(playButton);
            cardBorder.Child = grid;

            // Avviamo il download asincrono della copertina
            if (gameData != null)
            {
                LoadGameImageAsync(gameData, coverImage, false);
            }

            // Animazioni ed Effetti (Hover) SOLO PER IL BORDO DELLA CARD
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

            // Aggiungiamo la card completa al contenitore principale della pagina
            MainContainer.Children.Add(cardBorder);
        }

        private CardAction CreateCompactGameRow(string gameTitle)
        {
            // 1. Recupero le informazioni complete del gioco
            var gameData = _gamesController.InstalledGames.FirstOrDefault(g => g.Title == gameTitle);
            string piattaforme = gameData != null ? gameData.Platform : "Sconosciuta";

            // 2. Preparazione contenitore principale (CardAction)
            var card = new CardAction
            {
                Margin = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(5),
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            card.ContextMenu = CreateGameContextMenu(gameTitle);

            // 3. LA GRIGLIA INTERNA (3 colonne: Icona, Titolo, Pulsante Play)
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // Leggermente più larga per l'icona
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Titolo
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Pulsante Play

            // Colonna 0: Icona
            var iconBorder = new Border
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(5, 0, 5, 0),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 65)),
                ClipToBounds = true // Taglia l'icona se esce dai bordi arrotondati
            };

            // prendiamo l'icona online
            if (gameData != null)
            {
                LoadGameImageAsync(gameData, iconBorder, true); // true = isIcon
            }

            Grid.SetColumn(iconBorder, 0);

            // Colonna 1: Titolo del gioco
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            label.Inlines.Add(new System.Windows.Documents.Run { Text = gameTitle, FontSize = 14, FontWeight = FontWeights.SemiBold });

            if (!string.IsNullOrEmpty(piattaforme))
            {
                label.Inlines.Add(new System.Windows.Documents.Run { Text = $"   [{piattaforme}]", FontSize = 12, Foreground = System.Windows.Media.Brushes.DarkGray });
            }
            Grid.SetColumn(label, 1);

            // Colonna 2: Pulsante Play
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
            // Preparazione del menu principale
            var contextMenu = new ContextMenu();
            var manageItem = new MenuItem { Header = "Manage folders" };

            // Recupero delle cartelle personalizzate
            var customFolders = _folderController.Folders.Where(f => !f.IsSystemFolder).ToList();
            if (customFolders.Count > 0)
            {
                foreach (var folder in customFolders)
                {
                    var folderItem = new MenuItem { Header = folder.Name, IsCheckable = true, IsChecked = folder.Games.Contains(gameTitle), StaysOpenOnClick = true };
                    folderItem.Click += (s, ev) =>
                    {
                        _folderController.AssegnaGiocoACartella(gameTitle, folder, folderItem.IsChecked);
                        SalvaTutto();
                    };
                    manageItem.Items.Add(folderItem);
                }
            }
            else
            {
                manageItem.Items.Add(new MenuItem { Header = "No custom folders", IsEnabled = false });
            }
            contextMenu.Items.Add(manageItem);

            // Aggiornamento in tempo reale
            contextMenu.Closed += (s, ev) =>
            {
                if (_isListView)
                {
                    // Se siamo in modalità lista, aggiorna l'interfaccia a lista
                    DrawFullListView();
                }
                else
                {
                    // Se siamo in modalità griglia, ricarica la cartella in cui ci troviamo
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

            // 2. Spostamento
            var moveLeftItem = new MenuItem { Header = "Move Left / Up", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.ArrowLeft24 } };
            moveLeftItem.Click += (s, ev) => MoveFolder(folder, -1);
            contextMenu.Items.Add(moveLeftItem);

            var moveRightItem = new MenuItem { Header = "Move Right / Down", Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.ArrowRight24 } };
            moveRightItem.Click += (s, ev) => MoveFolder(folder, 1);
            contextMenu.Items.Add(moveRightItem);

            // 3. Rename e Delete (Solo per cartelle non di sistema)
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