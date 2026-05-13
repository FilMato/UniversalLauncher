
namespace UniversalLauncher.Models.GamesModels
{
    public class MicrosoftStoreGame : Game
    {
        // The AUMID is the unique identifier to launch a Store app (Format: "PackageFamilyName!AppId")
        public string AUMID { get; set; } = "";
        public MicrosoftStoreGame()
        {
            Platform = "Microsoft Store / Xbox";
        }
        public override string GetLaunchCommand()
        {
            return $"explorer.exe shell:AppsFolder\\{AUMID}"; // Windows command to launch a Store app using its AUMID
        }
    }
}