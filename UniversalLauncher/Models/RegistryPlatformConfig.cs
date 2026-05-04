
namespace UniversalLauncher.Models
{
    // Questa classe contiene solo i dati che cambiano tra un launcher e l'altro!
    public class RegistryPlatformConfig
    {
        public string PlatformName { get; set; } = "";
        // Usiamo una lista così possiamo cercare più parole chiave (es. "Electronic Arts" o "EA")
        public List<string> PublisherKeywords { get; set; } = new List<string>();
        //Usiamo una lista in cui ignorare alcune parole chiave, come i nomi stessi dei launcher, per evitare che vengano erroneamente considerati giochi
        public List<string> IgnoredTitles { get; set; } = new List<string>();
        // Possiamo aggiungere altre particolarità in futuro, se serve
    }
}