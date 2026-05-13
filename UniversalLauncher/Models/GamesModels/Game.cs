
namespace UniversalLauncher.Models.GamesModels
{
    public abstract class Game
    {
        public string? Title { get; set; }
        public string CoverImageUrl { get; set; } = ""; // For the grid view
        public string IconImageUrl { get; set; } = "";  // For the list view
        public string Platform { get; set; } = "Unknown";
        public abstract string GetLaunchCommand();//Every platform will have its own way to launch the game
    }
}