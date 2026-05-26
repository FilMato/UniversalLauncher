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
        //This is an invisible browser, that will talk to the SteamGridDB website in the background
        private readonly HttpClient _client;
        private string _apiKey = ""; // the API key is needed to authenticate our requests, but it's free and you can get it in 2 minutes by registering on https://www.steamgriddb.com/profile/api
        public SteamGridService()
        {
            _client = new HttpClient();
            // We try to read the API key from a "secrets.json" file located in the same folder as the executable.
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

        // This method is "async" so your launcher keeps scrolling smoothly while it downloads in the background
        public async Task FetchImagesForGameAsync(Game game)
        {
            try
            {
                if (string.IsNullOrEmpty(game.Title)) return; // If the game doesn't even have a title, we can't do anything, so we exit immediately
                // 1 Step (sequential): We search for the game ID starting from the formatted title
                string cleanTitle = game.Title.Replace("®", "").Replace("™", "").Replace("©", "").Trim(); // We clean the title from special characters that might mess up the search, and we also trim it to remove extra spaces at the beginning or end
                string searchUrl = $"https://www.steamgriddb.com/api/v2/search/autocomplete/{Uri.EscapeDataString(cleanTitle)}";
                var searchResponse = await _client.GetStringAsync(searchUrl);
                // Obtain the JSON response with the possible games that match the title
                using var searchDoc = JsonDocument.Parse(searchResponse);
                var dataArray = searchDoc.RootElement.GetProperty("data");
                if (dataArray.GetArrayLength() == 0) return; // If there are no results, we exit immediately
                string gameId = dataArray[0].GetProperty("id").GetInt32().ToString(); //We take the ID of the first result, which is usually the most relevant one

                // 2 Step (parallel): We prepare the URLs for the Cover and the Icon, and we start the download immediately without waiting for each other, to save time
                string gridUrl = $"https://www.steamgriddb.com/api/v2/grids/game/{gameId}?dimensions=600x900,342x482";
                string iconUrl = $"https://www.steamgriddb.com/api/v2/icons/game/{gameId}";
                Task<string> gridTask = _client.GetStringAsync(gridUrl);
                Task<string> iconTask = _client.GetStringAsync(iconUrl);

                // Awaiting both calls to finish, so we can process the results together.
                await Task.WhenAll(gridTask, iconTask);

                //3 Step (sequential): We read the results of the Cover and SAVE TO DISK
                using var gridDoc = JsonDocument.Parse(gridTask.Result);
                var gridData = gridDoc.RootElement.GetProperty("data");
                if (gridData.GetArrayLength() > 0)
                {
                    string webUrl = gridData[0].GetProperty("url").GetString() ?? "";
                    game.CoverImageUrl = await DownloadAndSaveImageAsync(webUrl, gameId, "cover");
                }

                // 4 Step (sequential): We read the results of the Icon and SAVE TO DISK
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
                // print the error in the Visual Studio console
                System.Diagnostics.Debug.WriteLine($"Errore API SteamGrid per {game.Title}: {ex.Message}");
            }
        }

        //Download the image physically and return the path on our PC
        private async Task<string> DownloadAndSaveImageAsync(string imageUrl, string gameId, string suffix)
        {
            if (string.IsNullOrEmpty(imageUrl)) return "";

            try
            {
                // Create a folder "ImageCache" near the executable of your launcher, to store the downloaded images permanently on the disk, so we don't have to download them again every time
                string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
                Directory.CreateDirectory(cacheFolder); // Se esiste già, non fa nulla

                // Create the file name by extracting the original extension (.png or .jpg)
                string extension = Path.GetExtension(imageUrl.Split('?')[0]);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg"; // Fallback in case we can't extract the extension for some reason
                string localFilePath = Path.Combine(cacheFolder, $"{gameId}_{suffix}{extension}");

                // If the file doesn't exist yet, we download it and save it to disk. If it already exists, we skip the download.
                if (!File.Exists(localFilePath))
                {
                    byte[] imageBytes = await _client.GetByteArrayAsync(imageUrl);
                    await File.WriteAllBytesAsync(localFilePath, imageBytes);
                }
                return localFilePath;
            }
            catch
            {
                // If something goes wrong with the disk, we return the original URL from the internet to avoid crashing anything
                return imageUrl;
            }
        }

        // This function is used to search for games based on a query string,it is used to make sure that a game exists and have the actual name.
        // it returns a dictionary where the key is the game ID (as a string) and the value is the official name of the game.
        public async Task<Dictionary<string, string>> SearchGamesListAsync(string query)
        {
            var results = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(query)) return results;

            try
            {
                string searchUrl = $"https://www.steamgriddb.com/api/v2/search/autocomplete/{Uri.EscapeDataString(query)}";
                var response = await _client.GetStringAsync(searchUrl);

                using var doc = JsonDocument.Parse(response);
                var data = doc.RootElement.GetProperty("data");
                // We take the first 10 results 
                int count = 0;
                foreach (var item in data.EnumerateArray())
                {
                    if (count >= 10) break;
                    string id = item.GetProperty("id").GetInt32().ToString();
                    string name = item.GetProperty("name").GetString() ?? "Unknown";

                    if (!results.ContainsKey(id))
                    {
                        results.Add(id, name);
                        count++;
                    }
                }
            }
            catch { }
            return results;
        }
    }
}