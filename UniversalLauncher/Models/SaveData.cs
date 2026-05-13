
namespace UniversalLauncher.Models
{
    // This is the "Master" object that we will write to the config.json file
    public class LibrarySaveData
    {
        public List<GameFolder> Folders { get; set; } = new List<GameFolder>();
        public Dictionary<string, ImageCache> CachedImages { get; set; } = new Dictionary<string, ImageCache>();
    }
    // This class will store only the heavy URLs of images and icons, so we don't have to fetch them every time we load the app.
    public class ImageCache
    {
        public string CoverUrl { get; set; } = "";
        public string IconUrl { get; set; } = "";
    }
}