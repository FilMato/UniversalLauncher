
namespace UniversalLauncher.Models.GamesModels
{
    public class SteamGame : Game
    {
        public string? AppId { get; set; }
        public SteamGame()
        {
            Platform = "Steam";
        }
        public override string GetLaunchCommand()
        {
            return $"steam://rungameid/{AppId}";
        }
    }
}