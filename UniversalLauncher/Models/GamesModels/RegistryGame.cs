
namespace UniversalLauncher.Models.GamesModels
{
    // Va bene per EA, GOG, Ubisoft e quasi qualsiasi gioco installato su Windows!
    public class RegistryGame : Game
    {
        public string? ExecutablePath { get; set; }

        public override string GetLaunchCommand()
        {
            return $"\"{ExecutablePath}\"";
        }
    }
}