
namespace UniversalLauncher.Models.GamesModels
{
    public class EpicGame : Game
    {
        public string? AppName { get; set; }
        public EpicGame()
        {
            Platform = "Epic Games";
        }

        public override string GetLaunchCommand()
        {
            // The universal command that tells Windows to call Epic and launch the game
            return $"com.epicgames.launcher://apps/{AppName}?action=launch&silent=true";
        }
    }
}