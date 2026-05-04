using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services
{
    public class FolderController
    {
        public List<GameFolder> Folders { get; private set; } = new List<GameFolder>();

        public void InizializzaCartelleDiSistema()
        {
            if (Folders.Count > 0) return;
            Folders.Add(new GameFolder("All games", true));
            Folders.Add(new GameFolder("Uncategorized", true));
        }

        //Controllo se il nome della cartella è valido (non vuoto e non duplicato, a meno che non sia lo stesso nome della cartella che stiamo rinominando)
        public string? ControllaNuovoNome(string newName, string oldName = "")
        {
            if (string.IsNullOrWhiteSpace(newName)) return "Name cannot be empty.";
            bool nameExists = Folders.Any(f => f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));
            if (nameExists && newName != oldName) return $"A folder named '{newName}' already exists!";
            return null; 
        }

        public GameFolder CreaCartella(string name)
        {
            var newFolder = new GameFolder(name, false);
            Folders.Add(newFolder);
            return newFolder;
        }

        //Eliminazione cartella, con salvataggio dei giochi in "Uncategorized" se non sono presenti in altre cartelle personalizzate
        public void EliminaCartellaESalvaGiochi(GameFolder folderToDelete)
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

        //Aggiora Uncategorised ogni volta che un gioco viene aggiunto o rimosso da una cartella personalizzata, in modo da mantenere sempre aggiornato lo stato di "Uncategorized"
        public void AggiornaStatoUncategorized(string gameTitle, int assignedCustomFoldersCount)
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

        // Popola "All games" e "Uncategorized" con tutti i giochi installati all'avvio, in modo da avere una base di partenza per la gestione delle cartelle
        public void PopolaCartelleDiSistema(List<Game> installedGames)
        {
            var allGamesFolder = Folders.FirstOrDefault(f => f.Name == "All games");
            var uncategorizedFolder = Folders.FirstOrDefault(f => f.Name == "Uncategorized");

            if (allGamesFolder != null && uncategorizedFolder != null)
            {
                uncategorizedFolder.Games.Clear();// Per sicurezza, partiamo da una Uncategorized pulita ad ogni riavvio

                foreach (var game in installedGames)
                {
                    if (game.Title != null)
                    {
                        // 1. Aggiungiamo sempre il gioco a "All games"
                        allGamesFolder.Games.Add(game.Title);
                        // Inseriamo il gioco in "Uncategorized" solo se non è già presente in nessuna cartella personalizzata
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

        //metodo per assegnare o rimuovere un gioco da una cartella personalizzata
        public void AssegnaGiocoACartella(string gameTitle, GameFolder folder, bool aggiungi)
        {
            if (aggiungi)
            {
                if (!folder.Games.Contains(gameTitle)) folder.Games.Add(gameTitle);
            }
            else
            {
                if (folder.Games.Contains(gameTitle)) folder.Games.Remove(gameTitle);
            }
            // Aggiorna Uncategorized in automatico
            int assignedCount = Folders.Count(f => !f.IsSystemFolder && f.Games.Contains(gameTitle));
            AggiornaStatoUncategorized(gameTitle, assignedCount);
        }
    }
}