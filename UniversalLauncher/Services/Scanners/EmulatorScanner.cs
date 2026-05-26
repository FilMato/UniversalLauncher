using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services
{
    public class EmulatorScanner
    {
        // SMART SEARCH: A predefined list of valid ROM extensions. 
        //-------You can add any format you need here in the future!-------
        private readonly string[] _validExtensions = {
            "wua", "rpx", "wud", "wux", // Wii U (Cemu)
            "iso", "cso", "chd",        // PS1/PS2/PSP
            "nsp", "xci",               // Switch
            "gcz", "rvz",               // GameCube/Wii
            "nes", "sfc", "gba", "nds"  // Retro Nintendo
        };

        // Notice we removed the 'string fileExtension' parameter
        public List<ManualGame> ScanEmulatorRoms(string emulatorExePath, string romsFolderPath, string argumentTemplate)
        {
            var foundGames = new List<ManualGame>();
            if (!File.Exists(emulatorExePath) || !Directory.Exists(romsFolderPath))
            {
                return foundGames;
            }
            // We enumerate all files, but immediately filter out the "garbage" files (like .txt, .jpg, .xml), keeping only those whose extension is in our _validExtensions list.
            var romFiles = Directory.EnumerateFiles(romsFolderPath, "*.*", SearchOption.AllDirectories)
                                    .Where(file => _validExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLower()));

            foreach (string romPath in romFiles)
            {
                // Skip Update and DLC folders to avoid duplicate game cards!
                if (romPath.Contains("Update", System.StringComparison.OrdinalIgnoreCase) ||
                    romPath.Contains("DLC", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Smart Title Extraction (Extracts the folder name if buried, or the file name if standalone)
                string gameTitle = Path.GetFileNameWithoutExtension(romPath);
                string relativePath = Path.GetRelativePath(romsFolderPath, romPath);
                string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
                if (pathParts.Length > 1)
                {
                    gameTitle = pathParts[0];
                }
                // Build launch arguments
                string finalArguments = argumentTemplate.Replace("{ROM_PATH}", $"\"{romPath}\"");

                var emulatedGame = new ManualGame
                {
                    Title = gameTitle,
                    Platform = "Emulator",
                    ExePath = $"\"{emulatorExePath}\"",
                    Arguments = finalArguments,
                    RomPath = romPath
                };
                foundGames.Add(emulatedGame);
            }
            return foundGames;
        }
    }
}