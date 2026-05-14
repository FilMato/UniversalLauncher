using System.Windows;
using System.Windows.Controls;
using UniversalLauncher.Models;

namespace UniversalLauncher
{
    public partial class MainWindow
    {

        /* This 3 functions are used to show the dialogs for renaming, deleting and personalizing folders.
         * The controller takes care of all the logic, here we just show the dialogs and call the controller functions based on the user's response.
         * The functionality is quite similar in all 3 cases.
         * Note: at the end there also is the MoveFolder function that is used to move the folders up and down, it's not directly called by a dialog but I put it here because it's related to the management of the folders and the UI.
         */

        private async void RenameFolderDialog(GameFolder folderToRename)
        {
            var input = new Wpf.Ui.Controls.TextBox { Text = folderToRename.Name };
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost) { Title = "Rename folder", Content = input, PrimaryButtonText = "Save", CloseButtonText = "Cancel" };

            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                string newName = input.Text.Trim();
                string? error = _folderController.ControlNewName(newName, folderToRename.Name);
                if (error != null)
                {
                    MessageBox.Show(error, "Error");
                    return;
                }
                folderToRename.Name = newName;
                _libraryService.SaveAll();
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
                _folderController.DeleteFolderAndSaveGames(folderToDelete);
                _libraryService.SaveAll();
                RefreshFoldersUI();
            }
        }

        private async void PersonalizeFolderDialog(GameFolder folderToPersonalize)
        {
            // 1. We create the standard tab control container, nothing fancy here
            var tabControl = new System.Windows.Controls.TabControl();
            var selectedBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(118, 185, 237));
            var unselectedBrush = System.Windows.Media.Brushes.LightGray;

            // --- TAB 1: Color ---
            // Create the header with the icon and the text, we will change their color when the tab is selected
            var colorIcon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Color24, Margin = new Thickness(0, 0, 8, 0), Foreground = selectedBrush };
            var colorText = new TextBlock { Text = "Color", VerticalAlignment = VerticalAlignment.Center, Foreground = selectedBrush };
            var colorHeader = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            colorHeader.Children.Add(colorIcon);
            colorHeader.Children.Add(colorText);
            var colorTab = new System.Windows.Controls.TabItem { Header = colorHeader };
            var colorPanel = new StackPanel { Margin = new Thickness(10) };
            // Title of the palette section
            colorPanel.Children.Add(new TextBlock { Text = "Recommended Colors", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            var paletteWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };
            // Our predefined colors, we can modify and add new ones here
            string[] presetColors = {
                "#28282D", // Base Gray (Default)
                "#204E5F", // Petrol Blue
                "#8f2201", // Brick Red
                "#2E7D32", // Forest Green
                "#872e87", // Dark magenta
                "#B08D57"  // Ancient Gold
            };

            // The text box for the custom color
            var hexInput = new Wpf.Ui.Controls.TextBox { Text = folderToPersonalize.BackgroundColor ?? "#28282D", PlaceholderText = "#HEXCODE" };

            // Generate the color buttons, if the user clicks on one of them, it fills the text box with the corresponding hex code
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
                // If the user clicks the square, it automatically fills the text box with that code
                btnColor.Click += (s, e) => hexInput.Text = hex;
                paletteWrap.Children.Add(btnColor);
            }
            colorPanel.Children.Add(paletteWrap);
            colorPanel.Children.Add(new TextBlock { Text = "Custom HEX Color", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            colorPanel.Children.Add(hexInput);
            colorTab.Content = colorPanel;

            // --- TAB 2: ICONS ---
            // Same thing, we create the elements separately so we can change their color later when the tab is selected
            var iconIcon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Image24, Margin = new Thickness(0, 0, 8, 0), Foreground = unselectedBrush };
            var iconText = new TextBlock { Text = "Icon", VerticalAlignment = VerticalAlignment.Center, Foreground = unselectedBrush };
            var iconHeader = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            iconHeader.Children.Add(iconIcon);
            iconHeader.Children.Add(iconText);
            var iconTab = new System.Windows.Controls.TabItem { Header = iconHeader };
            var iconPanel = new StackPanel { Margin = new Thickness(10) };
            // Title of the icons section
            iconPanel.Children.Add(new TextBlock { Text = "Choose an Icon", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            var iconWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 20) };

            // Here we have a list of predefined symbol names, you can modify and add new ones here by simply putting the name of the symbol
            string[] presetIcons = { 
                "Folder24",          // Classic Folder (Default)
                "FolderOpen24",      
                "Library24",         
                "Archive24",        
                "Box24",            
                "XboxController24",  
                "Target24",       
                "VehicleCar24",    
                "PuzzlePiece24",   
                "Shield24",         
                "Flash24",           
                "Star24",            
                "Heart24",           
                "Trophy24",          
                "Flame24",           
                "Globe24",           
                "Cloud24",          
                "People24",         
                "Play24",           
                "Rocket24",         
                "Wrench24",         
                "Beaker24"          
            };
            // Variable to remember which icon we clicked, if it's null we will use the default one (Folder24)
            string selectedIconName = folderToPersonalize.IconSymbolName ?? "Folder24";

            foreach (var iconName in presetIcons)
            {
                // We use TryParse to avoid crashes in case of invalid names, but since we are hardcoding them it shouldn't be a problem
                if (System.Enum.TryParse(iconName, out Wpf.Ui.Controls.SymbolRegular symbolEnum))
                {
                    var btnIcon = new Wpf.Ui.Controls.Button
                    {
                        Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = symbolEnum },
                        Width = 46,
                        Height = 46,
                        Margin = new Thickness(0, 0, 10, 10),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        // If it's the currently selected icon, we color it to make it stand out
                        Appearance = iconName == selectedIconName ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary
                    };

                    btnIcon.Click += (s, e) => {
                        selectedIconName = iconName; // Update the selected icon variable with the name of the clicked icon
                        // Reset all buttons to the base color (Gray/Secondary)
                        foreach (Wpf.Ui.Controls.Button btn in iconWrap.Children)
                        {
                            btn.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
                        }
                        // And color only the one just clicked (Primary)
                        btnIcon.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
                    };
                    iconWrap.Children.Add(btnIcon);
                }
            }
            iconPanel.Children.Add(iconWrap);
            iconTab.Content = iconPanel;
            //--Add additional tabs here---------
            //-----------------------------------
            // Update
            tabControl.Items.Add(colorTab);
            tabControl.Items.Add(iconTab);
            // Creating the dialog
            var dialog = new Wpf.Ui.Controls.ContentDialog(this.RootDialogHost)
            {
                Title = $"Personalize '{folderToPersonalize.Name}'",
                Content = tabControl,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel"
            };
            //Save the results
            if (await dialog.ShowAsync() == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                folderToPersonalize.BackgroundColor = hexInput.Text.Trim();
                folderToPersonalize.IconSymbolName = selectedIconName;
                _libraryService.SaveAll();
                RefreshFoldersUI();
            }
        }

        // This function is not directly called by a dialog, but I put it here beacause it's related to the management of the folders and the UI
        private void MoveFolder(GameFolder folderToMove, int direction)
        {
            // Find the current position of the folder in the list and calculate the new position
            int currentIndex = _folderController.Folders.IndexOf(folderToMove);
            int newIndex = currentIndex + direction;
            // Controls that the new position is valid (it can't go before zero or beyond the maximum)
            if (newIndex >= 0 && newIndex < _folderController.Folders.Count)
            {
                //Remove the folder and reinsert it in the new position
                _folderController.Folders.RemoveAt(currentIndex);
                _folderController.Folders.Insert(newIndex, folderToMove);
                // We save the new order in the JSON file and reload the interface
                _libraryService.SaveAll();
                RefreshFoldersUI(); 
            }
        }
    }
}