using Unicorn.Toolbox.Models.Stats;

namespace Unicorn.Toolbox.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(StatsCollector statsCollector)
        {
            StatsViewModel = new StatsViewModel(statsCollector);
            CoverageViewModel = new CoverageViewModel(statsCollector);
            LaunchViewModel = new LaunchViewModel();
            VisualizationViewModel = new VisualizationViewModel();
        }

        public ViewModelBase StatsViewModel { get; }

        public ViewModelBase CoverageViewModel { get; }

        public ViewModelBase LaunchViewModel { get; }

        public ViewModelBase VisualizationViewModel { get; }
    }
}
