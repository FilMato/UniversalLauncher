namespace UniversalLauncher.Models.GamesModels
{
    // This class inherits from Game, representing a title added manually by the user
    public class ManualGame : Game
    {
        // The path to the main executable (the emulator executable if is an emulated game)
        public string ExePath { get; set; } = "";

        // Optional arguments passed to the executable (e.g., the path to the ROM file)
        public string Arguments { get; set; } = "";

        //Stores the physical path of the ROM file to verify if the game was deleted from the PC
        public string RomPath { get; set; } = "";

        // Command string used by the GameLauncher
        public override string GetLaunchCommand()
        {
            if (!string.IsNullOrWhiteSpace(Arguments))
            {
                return $"{ExePath} {Arguments}";
            }
            return ExePath;
        }
    }
}