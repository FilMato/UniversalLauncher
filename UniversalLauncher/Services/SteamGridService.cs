using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services
{
    public class SteamGridService
    {
        // Questo è il nostro browser invisibile
        private readonly HttpClient _client;
        private string _apiKey = ""; // chiave di accesso al sito, che non facciamo visualizzare per ragioni di sicurezza
        public SteamGridService()
        {
            _client = new HttpClient();
            // Cerchiamo di leggere la chiave API da un file "secrets.json"
            string secretPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "secrets.json");
            if (File.Exists(secretPath))
            {
                try
                {
                    string json = File.ReadAllText(secretPath);
                    using var doc = JsonDocument.Parse(json);
                    _apiKey = doc.RootElement.GetProperty("SteamGridApiKey").GetString() ?? "";
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("Errore nella lettura di secrets.json");
                }
            }
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _client.DefaultRequestHeaders.Add("User-Agent", "UniversalLauncher/1.0");
        }

        // Questo metodo usa "async" così il tuo launcher continua a scorrere fluido mentre lui scarica in background
        public async Task FetchImagesForGameAsync(Game game)
        {
            try
            {
                if (string.IsNullOrEmpty(game.Title)) return; // Se il gioco non ha un titolo, non possiamo cercarlo!
                // PASSO 1 (SEQUENZIALE): Cerchiamo l'ID del gioco partendo dal titolo formattato
                string cleanTitle = game.Title.Replace("®", "").Replace("™", "").Replace("©", "").Trim();
                string searchUrl = $"https://www.steamgriddb.com/api/v2/search/autocomplete/{Uri.EscapeDataString(cleanTitle)}";
                var searchResponse = await _client.GetStringAsync(searchUrl);

                // Otteniamo la risposta JSON con i possibili giochi che corrispondono al titolo
                using var searchDoc = JsonDocument.Parse(searchResponse);
                var dataArray = searchDoc.RootElement.GetProperty("data");
                if (dataArray.GetArrayLength() == 0) return; // Se non lo trova, ci fermiamo qui

                string gameId = dataArray[0].GetProperty("id").GetInt32().ToString(); // Prendiamo l'ID del primo risultato

                // PASSO 2 (PARALLELO): Prepariamo gli indirizzi per la Copertina e per l'Icona
                string gridUrl = $"https://www.steamgriddb.com/api/v2/grids/game/{gameId}?dimensions=600x900,342x482";
                string iconUrl = $"https://www.steamgriddb.com/api/v2/icons/game/{gameId}";

                // INIZIAMO I DOWNLOAD CONTEMPORANEAMENTE! (Senza l'await, le richieste partono subito in background)
                Task<string> gridTask = _client.GetStringAsync(gridUrl);
                Task<string> iconTask = _client.GetStringAsync(iconUrl);

                // Aspettiamo che ENTRAMBE le chiamate abbiano finito
                await Task.WhenAll(gridTask, iconTask);

                // PASSO 3: Leggiamo i risultati della Copertina e SALVIAMO SU DISCO
                using var gridDoc = JsonDocument.Parse(gridTask.Result);
                var gridData = gridDoc.RootElement.GetProperty("data");
                if (gridData.GetArrayLength() > 0)
                {
                    string webUrl = gridData[0].GetProperty("url").GetString() ?? "";
                    game.CoverImageUrl = await DownloadAndSaveImageAsync(webUrl, gameId, "cover");
                }

                // PASSO 4: Leggiamo i risultati dell'Icona e SALVIAMO SU DISCO
                using var iconDoc = JsonDocument.Parse(iconTask.Result);
                var iconData = iconDoc.RootElement.GetProperty("data");
                if (iconData.GetArrayLength() > 0)
                {
                    string webUrl = iconData[0].GetProperty("url").GetString() ?? "";
                    game.IconImageUrl = await DownloadAndSaveImageAsync(webUrl, gameId, "icon");
                }
            }
            catch (Exception ex)
            {
                // Stampiamo l'errore nella console di Visual Studio
                System.Diagnostics.Debug.WriteLine($"Errore API SteamGrid per {game.Title}: {ex.Message}");
            }
        }

        // Scarica fisicamente l'immagine e ci restituisce il percorso sul nostro PC
        private async Task<string> DownloadAndSaveImageAsync(string imageUrl, string gameId, string suffix)
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";

            try
            {
                // Creiamo una cartella "ImageCache" vicino all'eseguibile del tuo launcher
                string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
                Directory.CreateDirectory(cacheFolder); // Se esiste già, non fa nulla

                // Creiamo il nome del file estraendo l'estensione originale (.png o .jpg)
                string extension = Path.GetExtension(imageUrl.Split('?')[0]);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg"; // Fallback di sicurezza

                string localFilePath = Path.Combine(cacheFolder, $"{gameId}_{suffix}{extension}");

                // Se non l'abbiamo ancora scaricata, la scarichiamo fisicamente!
                if (!File.Exists(localFilePath))
                {
                    byte[] imageBytes = await _client.GetByteArrayAsync(imageUrl);
                    await File.WriteAllBytesAsync(localFilePath, imageBytes);
                }
                return localFilePath;
            }
            catch
            {
                // Se qualcosa va storto col disco, restituiamo l'URL originale di internet per non far crashare nulla
                return imageUrl;
            }
        }

        // Questa funzione pulisce i nomi dei file per evitare problemi con i caratteri proibiti o strani
        private string GetSafeFilename(string filename)
        {
            // 1. Rimuove i caratteri non accettati nei nomi dei file (come \ / : * ? " < > |)
            string safe = string.Join("_", filename.Split(System.IO.Path.GetInvalidFileNameChars()));
            // 2. Rimuove i simboli di copyright e marchi 
            safe = safe.Replace("®", "").Replace("™", "").Replace("©", "");
            // 3. Toglie eventuali spazi vuoti doppi o all'inizio/fine
            return safe.Trim();
        }
    }
}