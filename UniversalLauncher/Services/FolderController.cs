using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services
{
    public class FolderController
    {
        public List<GameFolder> Folders { get; private set; } = new List<GameFolder>();
        
        public void InitializeSystemFolders()
        {
            if (Folders.Count > 0) return;
            Folders.Add(new GameFolder("All games", true));
            Folders.Add(new GameFolder("Uncategorized", true));
        }

        //Controls if the folder name is valid (not empty and not duplicate, unless it's the same name as the folder we're renaming)
        public string? ControlNewName(string newName, string oldName = "")
        {
            if (string.IsNullOrWhiteSpace(newName)) return "Name cannot be empty.";
            bool nameExists = Folders.Any(f => f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (nameExists && newName != oldName) return $"A folder named '{newName}' already exists!";
            return null; 
        }

        public GameFolder CreateFolder(string name)
        {
            var newFolder = new GameFolder(name, false);
            Folders.Add(newFolder);
            return newFolder;
        }

        //Deletes the folder and moves its games to "Uncategorized" only if they are not present in any other custom folder
        public void DeleteFolderAndSaveGames(GameFolder folderToDelete) 
        {
            var uncategorized = Folders.FirstOrDefault(f => f.Name == "Uncategorized");
            foreach (string gameTitle in folderToDelete.Games)
            {
                int otherFoldersCount = Folders.Count(f =>!f.IsSystemFolder && f.Name != folderToDelete.Name && f.Games.Contains(gameTitle));
                if (otherFoldersCount == 0 && uncategorized != null && !uncategorized.Games.Contains(gameTitle))
                {
                    uncategorized.Games.Add(gameTitle);
                }
            }
            Folders.Remove(folderToDelete);
        }

        //Update Uncategorized every time a game is added or removed from a custom folder, to keep the "Uncategorized" status always updated
        public void UpdateUncategorizedStatus(string gameTitle, int assignedCustomFoldersCount)
        {
            var uncategorized = Folders.FirstOrDefault(f => f.Name == "Uncategorized");
            if (uncategorized != null)
            {
                if (assignedCustomFoldersCount > 0)
                {
                    if (uncategorized.Games.Contains(gameTitle)) uncategorized.Games.Remove(gameTitle);
                }
                else
                {
                    if (!uncategorized.Games.Contains(gameTitle)) uncategorized.Games.Add(gameTitle);
                }
            }
        }

        //Populate "All games" and "Uncategorized" with all installed games at startup, to have a starting point for folder management
        public void PopulateSystemFolders(List<Game> installedGames) 
        {
            var allGamesFolder = Folders.FirstOrDefault(f => f.Name == "All games");
            var uncategorizedFolder = Folders.FirstOrDefault(f => f.Name == "Uncategorized");

            if (allGamesFolder != null && uncategorizedFolder != null)
            {
                uncategorizedFolder.Games.Clear();// To be sure we start with an empty "Uncategorized" before populating it

                foreach (var game in installedGames)
                {
                    if (game.Title != null)
                    {
                        //We always add the game to "All games"
                        allGamesFolder.Games.Add(game.Title);
                        //We add the game to "Uncategorized" only if it's not already present in any custom folder
                        bool isAlreadyCategorized = Folders.Any(f => !f.IsSystemFolder && f.Games.Contains(game.Title));
                        if (!isAlreadyCategorized)
                        {
                            if (!uncategorizedFolder.Games.Contains(game.Title))
                            {
                                uncategorizedFolder.Games.Add(game.Title);
                            }
                        }
                    }
                }
            }
        }

        //Function to assign or remove a game from a custom folder, which also updates the "Uncategorized" status of the game in real time
        public void AssignGameToFolder(string gameTitle, GameFolder folder, bool aggiungi) 
        {
            if (aggiungi)
            {
                if (!folder.Games.Contains(gameTitle)) folder.Games.Add(gameTitle);
            }
            else
            {
                if (folder.Games.Contains(gameTitle)) folder.Games.Remove(gameTitle);
            }
            //Update Uncategorized automatically
            int assignedCount = Folders.Count(f => !f.IsSystemFolder && f.Games.Contains(gameTitle));
            UpdateUncategorizedStatus(gameTitle, assignedCount);
        }
    }
}