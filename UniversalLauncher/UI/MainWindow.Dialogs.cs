using System.Windows;
using System.Windows.Controls;
using UniversalLauncher.Models;

namespace UniversalLauncher
{
    public partial class MainWindow
    {
        // Le seguenti 3 funzioni servono per mostrare i dialog per creare, rinominare ed eliminare le cartelle. 
        // Il controller si occupa di tutte le logiche, qui ci limitiamo a mostrare i dialog e a chiamare le funzioni del controller in base alla risposta dell'utente. 
        // Il funzionamento è abbastanza simile in tutti e 3 i casi

        private async void BtnNuovaCartella_Click(object sender, RoutedEventArgs e)
        {
            // Popup per chiedere il nome della nuova cartella
            var input = new Wpf.Ui.Controls.TextBox { PlaceholderText = "E.g. RPG Games" };
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost) { Title = "Create new folder", Content = input, PrimaryButtonText = "Create", CloseButtonText = "Cancel" };

            // Se l'utente preme "Create", controlliamo che il nome sia valido e, in caso positivo, creiamo la cartella. Altrimenti mostriamo un messaggio di errore.
            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                string newName = input.Text.Trim();
                string? error = _folderController.ControllaNuovoNome(newName);
                if (error != null)
                {
                    MessageBox.Show(error, "Error");
                    return;
                }
                _folderController.CreaCartella(newName);
                SalvaTutto();
                RefreshFoldersUI();
            }
        }

        private async void RenameFolderDialog(GameFolder folderToRename)
        {
            var input = new Wpf.Ui.Controls.TextBox { Text = folderToRename.Name };
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost) { Title = "Rename folder", Content = input, PrimaryButtonText = "Save", CloseButtonText = "Cancel" };

            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                string newName = input.Text.Trim();
                string? error = _folderController.ControllaNuovoNome(newName, folderToRename.Name);
                if (error != null)
                {
                    MessageBox.Show(error, "Error");
                    return;
                }
                folderToRename.Name = newName;
                SalvaTutto();
                RefreshFoldersUI();
            }
        }

        private async void DeleteFolderDialog(GameFolder folderToDelete)
        {
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost)
            {
                Title = "Delete Folder?",
                Content = $"Delete '{folderToDelete.Name}'?\nGames inside will remain in Uncategorized.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
            };

            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                _folderController.EliminaCartellaESalvaGiochi(folderToDelete);
                SalvaTutto();
                RefreshFoldersUI();
            }
        }

        private async void PersonalizeFolderDialog(GameFolder folderToPersonalize)
        {
            // 1. Creiamo il contenitore a schede STANDARD 
            var tabControl = new System.Windows.Controls.TabControl();

            var selectedBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(118, 185, 237));
            var unselectedBrush = System.Windows.Media.Brushes.LightGray;

            // --- TAB 1: COLORE ---
            // Creiamo gli elementi separatamente così possiamo modificarne il colore in seguito!
            var colorIcon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Color24, Margin = new Thickness(0, 0, 8, 0), Foreground = selectedBrush };
            var colorText = new TextBlock { Text = "Color", VerticalAlignment = VerticalAlignment.Center, Foreground = selectedBrush };
            var colorHeader = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            colorHeader.Children.Add(colorIcon);
            colorHeader.Children.Add(colorText);
            var colorTab = new System.Windows.Controls.TabItem { Header = colorHeader };
            var colorPanel = new StackPanel { Margin = new Thickness(10) };

            // Titolo della tavolozza
            colorPanel.Children.Add(new TextBlock { Text = "Recommended Colors", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            var paletteWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };

            // I nostri colori predefiniti, qui possiamo modificarli ed anggiungerne dei nuovi
            string[] presetColors = {
                "#28282D", // Grigio Base (Default)
                "#204E5F", // Blu Petrolio 
                "#8f2201", // Rosso Mattone
                "#2E7D32", // Verde Foresta
                "#872e87", // Magenta scuro
                "#B08D57"  // Oro Antico
            };

            // La casella di testo per il colore personalizzato
            var hexInput = new Wpf.Ui.Controls.TextBox { Text = folderToPersonalize.BackgroundColor ?? "#28282D", PlaceholderText = "#HEXCODE" };

            // Generiamo i bottoni colorati
            foreach (var hex in presetColors)
            {
                var btnColor = new System.Windows.Controls.Button
                {
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 10),
                    Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex)),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderThickness = new Thickness(0)
                };

                // Se l'utente clicca il quadratino, compila in automatico la casella di testo con quel codice!
                btnColor.Click += (s, e) => hexInput.Text = hex;
                paletteWrap.Children.Add(btnColor);
            }

            colorPanel.Children.Add(paletteWrap);
            colorPanel.Children.Add(new TextBlock { Text = "Custom HEX Color", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            colorPanel.Children.Add(hexInput);
            colorTab.Content = colorPanel;

            // --- TAB 2: ICONA ---
            // Stessa cosa, creiamo gli elementi separatamente così possiamo modificare il colore in seguito
            var iconIcon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Image24, Margin = new Thickness(0, 0, 8, 0), Foreground = unselectedBrush };
            var iconText = new TextBlock { Text = "Icon", VerticalAlignment = VerticalAlignment.Center, Foreground = unselectedBrush };
            var iconHeader = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            iconHeader.Children.Add(iconIcon);
            iconHeader.Children.Add(iconText);
            var iconTab = new System.Windows.Controls.TabItem { Header = iconHeader };
            var iconPanel = new StackPanel { Margin = new Thickness(10) };
            // Titolo della sezione icone
            iconPanel.Children.Add(new TextBlock { Text = "Choose an Icon", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            var iconWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };

            // Qui mettiamo una serie di nomi di simboli predefiniti, se volete potete aggiungerne altri semplicemente mettendo il nome del simbolo
            string[] presetIcons = { 
                "Folder24",          // Cartella classica
                "FolderOpen24",      // Cartella aperta
                "Library24",         // Libreria (ottima per "Tutti i giochi")
                "Archive24",         // Archivio (per i giochi vecchi)
                "Box24",             // Scatola/Pacchetto
                "XboxController24",  // Giochi con controller
                "Target24",          // Sparatutto / FPS
                "VehicleCar24",      // Giochi di corse
                "PuzzlePiece24",     // Puzzle / Rompicapo
                "Shield24",          // RPG / Avventura / Fantasy
                "Flash24",           // Azione / Giochi frenetici
                "Star24",            // Preferiti
                "Heart24",           // Molto amati
                "Trophy24",          // Completati al 100% / Competitivi
                "Flame24",           // Nuove uscite / "In voga"
                "Globe24",           // Giochi Online / MMO
                "Cloud24",           // Cloud Gaming
                "People24",          // Multiplayer / Co-op
                "Play24",            // Da giocare / In corso
                "Rocket24",          // Avvio rapido / Giochi leggeri
                "Wrench24",          // Giochi moddati / Strumenti
                "Beaker24"           // Beta / Accesso Anticipato
            };
            // Variabile per ricordare quale icona abbiamo cliccato
            string selectedIconName = folderToPersonalize.IconSymbolName ?? "Folder24";

            foreach (var iconName in presetIcons)
            {
                // Convertiamo il testo nel simbolo vero e proprio
                if (System.Enum.TryParse(iconName, out Wpf.Ui.Controls.SymbolRegular symbolEnum))
                {
                    var btnIcon = new Wpf.Ui.Controls.Button
                    {
                        Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbolEnum },
                        Width = 46,
                        Height = 46,
                        Margin = new Thickness(0, 0, 10, 10),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        // Se è l'icona attualmente selezionata, la coloriamo per farla spiccare
                        Appearance = iconName == selectedIconName ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary
                    };

                    btnIcon.Click += (s, e) => {
                        selectedIconName = iconName; // Aggiorniamo la scelta

                        // Resettiamo visivamente tutti i bottoni al colore base (Grigio/Secondary)
                        foreach (Wpf.Ui.Controls.Button btn in iconWrap.Children)
                        {
                            btn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                        }
                        // E coloriamo solo quello appena cliccato (Primary)
                        btnIcon.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
                    };
                    iconWrap.Children.Add(btnIcon);
                }
            }
            iconPanel.Children.Add(iconWrap);
            iconTab.Content = iconPanel;
            //--Se vogliamo aggiongere dei tab vanno qui---------
            //---------------------------------------------------
            // Aggiungiamo le schede
            tabControl.Items.Add(colorTab);
            tabControl.Items.Add(iconTab);

            // 2. Creiamo il Dialog vero e proprio
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost)
            {
                Title = $"Personalize '{folderToPersonalize.Name}'",
                Content = tabControl,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel"
            };

            // 3. Salviamo il risultato
            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                folderToPersonalize.BackgroundColor = hexInput.Text.Trim();
                folderToPersonalize.IconSymbolName = selectedIconName;
                SalvaTutto();
                RefreshFoldersUI();
            }
        }

        //Funzione per Scegliere lordinamento delle cartelle
        private void MoveFolder(GameFolder folderToMove, int direction)
        {
            // Troviamo la posizione attuale della cartella nella lista e calcoliamo la nuova posizione
            int currentIndex = _folderController.Folders.IndexOf(folderToMove);
            int newIndex = currentIndex + direction;
            // Controlliamo che la nuova posizione sia valida (non può andare prima dello zero o oltre il massimo)
            if (newIndex >= 0 && newIndex < _folderController.Folders.Count)
            {
                // Rimuoviamo la cartella e la reinseriamo nella nuova posizione
                _folderController.Folders.RemoveAt(currentIndex);
                _folderController.Folders.Insert(newIndex, folderToMove);

                // Salviamo il nuovo ordine nel file JSON e ricarichiamo l'interfaccia
                SalvaTutto();
                RefreshFoldersUI(); // Assicurati che questo sia il nome del tuo metodo per ridisegnare le cartelle (potrebbe chiamarsi LoadFolders o simile)
            }
        }
    }
}