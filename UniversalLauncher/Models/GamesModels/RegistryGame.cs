
namespace UniversalLauncher.Models.GamesModels
{
    //This is a generic game class that will work for most of the platforms (such as EA, GOG, Ubisoft, ecc) and any game that can be launched with a simple executable path.
    public class RegistryGame : Game
    {
        public string? ExecutablePath { get; set; }

        public override string GetLaunchCommand()
        {
            return $"\"{ExecutablePath}\"";
        }
    }
}