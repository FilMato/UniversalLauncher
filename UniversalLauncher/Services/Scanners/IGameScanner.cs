using UniversalLauncher.Models.GamesModels;

namespace UniversalLauncher.Services.Scanners
{
    public interface IGameScanner
    {
        List<Game> GetInstalledGames();
    }
}