
namespace UniversalLauncher.Models.GamesModels
{
    public class MicrosoftStoreGame : Game
    {
        // L'AUMID è l'identificatore univoco per lanciare un'app Store(Formato: "PackageFamilyName!AppId")
        public string AUMID { get; set; } = "";
        public MicrosoftStoreGame()
        {
            Platform = "Microsoft Store / Xbox";
        }
        public override string GetLaunchCommand()
        {
            return $"explorer.exe shell:AppsFolder\\{AUMID}"; // Il comando della shell di Windows
        }
    }
}