
namespace UniversalLauncher.Models.GamesModels
{
    public class StandaloneGame : Game
    {
        // Proprietà specifiche per i giochi slegati dai launcher
        public string ExecutablePath { get; set; } = "";
        public string LaunchArguments { get; set; } = "";
        public StandaloneGame()
        {
            Platform = "Standalone";
        }

        public override string GetLaunchCommand()
        {
            // Mettiamo il percorso tra virgolette per evitare problemi con gli spazi nelle cartelle di Windows e ci attacchiamo gli argomenti (se ci sono)
            return $"\"{ExecutablePath}\" {LaunchArguments}".Trim();
        }
    }
}