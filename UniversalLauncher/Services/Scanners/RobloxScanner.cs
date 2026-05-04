using System.IO;
using UniversalLauncher.Models.GamesModels; // Assicurati che punti ai tuoi modelli

namespace UniversalLauncher.Services.Scanners
{
    public class RobloxScanner : IGameScanner
    {
        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();

            //puntiamo direttamente alla cartella nascosta AppData/Local dell'utente
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string robloxPath = Path.Combine(localAppData, "Roblox", "Versions");
            if (Directory.Exists(robloxPath))
            {
                try
                {
                    // Cerchiamo RobloxPlayerBeta.exe in tutte le sottocartelle
                    var exeFiles = Directory.GetFiles(robloxPath, "RobloxPlayerBeta.exe", SearchOption.AllDirectories);
                    if (exeFiles.Length > 0)
                    {
                        // Roblox conserva le vecchie versioni. Noi vogliamo l'exe modificato più di recente!
                        string latestExe = exeFiles[0];
                        DateTime newestDate = File.GetLastWriteTime(latestExe);
                        foreach (var exe in exeFiles)
                        {
                            DateTime currentDate = File.GetLastWriteTime(exe);
                            if (currentDate > newestDate)
                            {
                                latestExe = exe;
                                newestDate = currentDate;
                            }
                        }

                        installedGames.Add(new StandaloneGame{ Title = "Roblox", ExecutablePath = latestExe, LaunchArguments = "--app" }); // LaunchArguments== "--app": Questo apre l'app desktop!

                    }
                }
                catch{}
            }
            return installedGames;
        }
    }
}