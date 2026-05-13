
namespace UniversalLauncher.Models
{
    public class GameFolder
    {
        public string Name { get; set; } = "";
        public HashSet<string> Games { get; set; } = new HashSet<string>();
        public bool IsSystemFolder { get; set; }//Tells the app if this is a default, unremovable folder

        public GameFolder() { } // Empty constructor is mandatory for JSON deserialization
        public GameFolder(string name, bool isSystemFolder = false)
        {
            Name = name;
            IsSystemFolder = isSystemFolder;
        }
        public string BackgroundColor { get; set; } = "#28282D";
        public string IconSymbolName { get; set; } = "Folder24";
    }

}