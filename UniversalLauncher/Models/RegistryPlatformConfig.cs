
namespace UniversalLauncher.Models
{
    // This class contains only the data that changes between one launcher and another!
    public class RegistryPlatformConfig
    {
        public string PlatformName { get; set; } = "";
        // We use a list so we can search for multiple keywords (e.g. "Electronic Arts" or "EA")
        public List<string> PublisherKeywords { get; set; } = new List<string>();
        // We use a list to ignore some keywords, such as the names of the launchers themselves, to avoid them being mistakenly considered games
        public List<string> IgnoredTitles { get; set; } = new List<string>();
    }
}