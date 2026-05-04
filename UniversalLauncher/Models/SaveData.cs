
namespace UniversalLauncher.Models
{
    // Questo è l'oggetto "Master" che scriveremo nel file config.json
    public class LibrarySaveData
    {
        public List<GameFolder> Folders { get; set; } = new List<GameFolder>();
        public Dictionary<string, ImageCache> CachedImages { get; set; } = new Dictionary<string, ImageCache>();
    }

    // Questo conserva solo gli URL pesanti
    public class ImageCache
    {
        public string CoverUrl { get; set; } = "";
        public string IconUrl { get; set; } = "";
    }
}