
namespace UniversalLauncher.Models.GamesModels
{
    public abstract class Game
    {
        public string? Title { get; set; }
        public string CoverImageUrl { get; set; } = ""; // Per la visualizzazione a Griglia
        public string IconImageUrl { get; set; } = "";  // Per la visualizzazione a Lista
        public string Platform { get; set; } = "Unknown";
        public abstract string GetLaunchCommand();//Ogni piattaforma dovrà specificarne uno proprio
    }
}