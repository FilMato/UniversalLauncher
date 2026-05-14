
using MessageBox = System.Windows.MessageBox;

namespace UniversalLauncher
{
    public partial class MainWindow
    {
        private void ApiKeyControl()
        {
            string secretPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "secrets.json");
            bool keyMancante = false;

            // If the file doesn't exist, we create it as a "template"
            if (!System.IO.File.Exists(secretPath))
            {
                string template = "{\n  \"SteamGridApiKey\": \"YOUR_API_KEY_HERE\"\n}";
                System.IO.File.WriteAllText(secretPath, template, System.Text.Encoding.UTF8);
                keyMancante = true;
            }
            else
            {
                // If it exists, we check its content
                try
                {
                    string json = System.IO.File.ReadAllText(secretPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    string apiKey = doc.RootElement.GetProperty("SteamGridApiKey").GetString() ?? "";
                    // If it's empty or still has the default text, we show the warning
                    if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY_HERE")
                    {
                        keyMancante = true;
                    }
                }
                catch
                {
                    // If the file is corrupted or badly formatted
                    keyMancante = true;
                }
            }

            if (keyMancante)
            {
                MessageBox.Show("Welcome to UniversalLauncher! 🚀\n\n" +
                                "If you want to visualize game covers and icons, you need to enter a free API Key.\n\n" +
                                "1. Go to: steamgriddb.com and create an account for free\n" +
                                "2. Go to: steamgriddb.com/profile/api\n" +
                                "3. Generate a key and copy it.\n" +
                                "4. Open the 'secrets.json' file (next to the executable) and paste the key in place of 'YOUR_API_KEY_HERE'.\n\n" +
                                "The program will still work, but you will only see the default gray icons until you enter the key.");
            }
        }
        
        // Loads the installed games and populates the system folders (All games and Uncategorized) in one go, then draws the interface
        private async Task LoadGamesAndFoldersAsync()
        {
            _folderController.InitializeSystemFolders();
            _libraryService.LoadAll();
            RefreshFoldersUI();
            // Start the parallel scan in the background without blocking the UI
            await _gamesController.ScanAndLoadGamesAsync();
            _libraryService.ApplyScanResults();
            RefreshFoldersUI();
        }


    }
}
