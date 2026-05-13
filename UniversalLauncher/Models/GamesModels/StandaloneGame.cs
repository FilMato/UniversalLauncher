
namespace UniversalLauncher.Models.GamesModels
{
    public class StandaloneGame : Game
    {
        // This class is meant to be used for games that are not tied to any specific launcher.
        public string ExecutablePath { get; set; } = "";
        public string LaunchArguments { get; set; } = "";
        public StandaloneGame()
        {
            Platform = "Standalone";
        }

        public override string GetLaunchCommand()
        {
            // We put the path in quotes to avoid issues with spaces in Windows folder names and we attach the arguments (if any)
            return $"\"{ExecutablePath}\" {LaunchArguments}".Trim();
        }
    }
}