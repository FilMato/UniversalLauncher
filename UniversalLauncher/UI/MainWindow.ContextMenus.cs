using System.Windows.Controls;
using UniversalLauncher.Models;
using MenuItem = System.Windows.Controls.MenuItem;

namespace UniversalLauncher
{
    public partial class MainWindow
    {
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
                        _libraryService.SaveAll();
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
    }
}
