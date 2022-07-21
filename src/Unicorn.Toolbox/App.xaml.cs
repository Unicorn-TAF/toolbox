using System.Windows;
using Unicorn.Toolbox.Stats;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox
{
    /// <summary>
    /// Interaction logic for App
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(new StatsCollector())
            };

            MainWindow.Show();

            base.OnStartup(e);
        }
    }
}
