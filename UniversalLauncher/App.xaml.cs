using System.Windows;

namespace UniversalLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e); // Avvia i processi base di WPF

            // Creiamo la finestra principale manualmente
            MainWindow window = new MainWindow();

            // La mostriamo a schermo
            window.Show();
        }
    }
}