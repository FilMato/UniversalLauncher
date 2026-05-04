
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
            // Il comando universale che dice a Windows di chiamare Epic e avviare il gioco
            return $"com.epicgames.launcher://apps/{AppName}?action=launch&silent=true";
        }
    }
}