using Microsoft.Win32;
using System.IO;
using UniversalLauncher.Models;
using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services.Scanners
{
    public class UniversalRegistryScanner : IGameScanner
    {
        // Riceviamo l'intera lista delle piattaforme da scansionare
        private readonly List<RegistryPlatformConfig> _configs;
        public UniversalRegistryScanner(List<RegistryPlatformConfig> configs)
        {
            _configs = configs;
        }
        public List<Game> GetInstalledGames()
        {
            var installedGames = new List<Game>();
            // I due percorsi del Registro di Windows (64-bit e 32-bit)
            string[] registryKeys = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            // Per ogni percorso, cerchiamo tutte le sottocartelle che rappresentano i programmi installati
            foreach (var keyPath in registryKeys)
            {
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key == null) continue;
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                        {
                            if (subKey == null) continue;
                            // Leggiamo le informazioni di base: editore, nome del programma e cartella di installazione, oppure usiamo stringhe vuote per evitare errori
                            string publisher = subKey.GetValue("Publisher") as string ?? "";
                            string title = subKey.GetValue("DisplayName") as string ?? "";
                            string installLocation = subKey.GetValue("InstallLocation") as string ?? "";
                            // Primo foltro: se manca una di queste informazioni o la cartella di installazione non esiste, non è un gioco valido
                            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(installLocation) || !Directory.Exists(installLocation))
                                continue;
                            // Trasformiamo tutto in minuscolo per non farci fregare dalle maiuscole
                            string titleLower = title.ToLower();
                            string publisherLower = publisher.ToLower();
                            // Controlliamo se questo programma appartiene a una delle nostre piattaforme
                            foreach (var config in _configs)
                            {
                                // Secondo foltro: verifica se il publisher del programma contiene una delle nostre parole chiave
                                bool isPublisherMatch = config.PublisherKeywords.Any(keyword =>
                                    publisherLower.Contains(keyword.ToLower()));
                                if (isPublisherMatch)
                                {
                                    // Terzo filtro: se il titolo del gioco contiene una delle parole chiave di blacklist, lo ignoriamo
                                    bool isBlacklisted = false;
                                    if (config.IgnoredTitles != null && config.IgnoredTitles.Count > 0)
                                    {
                                        isBlacklisted = config.IgnoredTitles.Any(badWord =>
                                            titleLower.Contains(badWord.ToLower()));
                                    }
                                    if (!isBlacklisted)
                                    {
                                        //Estrazione dell'Eseguibile e Salvataggio
                                        string? executable = FindGameExecutable(installLocation);
                                        if (!string.IsNullOrEmpty(executable))
                                        {
                                            installedGames.Add(new RegistryGame
                                            {
                                                Title = title,
                                                Platform = config.PlatformName,
                                                ExecutablePath = executable
                                            });
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return installedGames;
        }

        // Funzione per trovare l'eseguibile del gioco
        private string? FindGameExecutable(string installDir)
        {
            try
            {
                // Impostiamo la nostra soglia (10 MB per i giochi grandi)
                const long MASSIVE_FILE_THRESHOLD = 10 * 1024 * 1024;
                string? bestExe = null;
                long maxSize = 0;
                // Chiamiamo il nostro  metodo di ricerca
                string? foundHeavyExe = SearchDirectoryIntelligently(installDir, ref bestExe, ref maxSize, MASSIVE_FILE_THRESHOLD);
                return foundHeavyExe ?? bestExe;
            }
            catch
            {
                return null;
            }
        }

        // motore di ricerca intelligente: cerca in modo ricorsivo, ma con ottimizzazioni per evitare di perdere tempo in cartelle inutili
        private string? SearchDirectoryIntelligently(string directoryPath, ref string? bestExe, ref long maxSize, long threshold, int currentDepth = 0)
        {
            // limitiamo la profondità della ricerca per evitare di perderci in cartelle troppo profonde
            if (currentDepth > 4) return null;

            try
            {
                // Esaminiamo prima i file .exe nella cartella attuale
                foreach (var exe in Directory.EnumerateFiles(directoryPath, "*.exe"))
                {
                    string fileName = Path.GetFileName(exe).ToLower();
                    // Lista nera dei nomi di file: se il nome del file contiene una di queste parole chiave lo saltiamo direttamente
                    if (fileName.Contains("cleanup") || fileName.Contains("touchup") ||
                        fileName.Contains("uninstall") || fileName.Contains("unins") ||
                        fileName.Contains("activation") || fileName.Contains("crash") ||
                        fileName.Contains("redist") || fileName.Contains("dxsetup") ||
                        fileName.Contains("vcredist") || fileName.Contains("dotne"))
                    {
                        continue;
                    }
                    long size = new FileInfo(exe).Length;
                    // Se superiamo la soglia, abbiamo trovato il jackpot! Fermiamo tutta la ricerca.
                    if (size > threshold){return exe;}
                    if (size > maxSize)
                    {
                        maxSize = size;
                        bestExe = exe;
                    }
                }
                //Esaminiamo le sottocartelle
                foreach (var subDir in Directory.EnumerateDirectories(directoryPath))
                {
                    string dirName = new DirectoryInfo(subDir).Name.ToLower();
                    // Lista nera delle sottocartelle: se il nome della sottocartella contiene una di queste parole chiave lo saltiamo direttamente
                    if (dirName == "redist" || dirName == "_redist" || dirName == "support" ||
                        dirName == "movies" || dirName == "video" || dirName == "sound" ||
                        dirName == "audio" || dirName == "music" || dirName == "logs" ||
                        dirName == "save" || dirName == "saves" || dirName == "locales" ||
                        dirName == "dlc" || dirName == "bonus")
                    {
                        continue;
                    }
                    // Se la cartella è valida, entriamo e cerchiamo ricorsivamente
                    string? foundInSub = SearchDirectoryIntelligently(subDir, ref bestExe, ref maxSize, threshold, currentDepth + 1);
                    // Se la chiamata interna ha trovato un file che supera la soglia, lo passiamo in alto e chiudiamo tutto
                    if (foundInSub != null) return foundInSub;
                }
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                // Ignora silenziosamente le cartelle di sistema bloccate
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}