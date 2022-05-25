using Unicorn.Toolbox.Analysis;

namespace Unicorn.Toolbox.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(Analyzer analyzer)
        {
            StatisticsViewModel = new StatisticsViewModel(analyzer);
            CoverageViewModel = new CoverageViewModel(analyzer);
            LaunchResultsViewModel = new LaunchResultsViewModel();
            VisualizationViewModel = new VisualizationViewModel();
        }

        public ViewModelBase StatisticsViewModel { get; }

        public ViewModelBase CoverageViewModel { get; }

        public ViewModelBase LaunchResultsViewModel { get; }

        public ViewModelBase VisualizationViewModel { get; }
    }
}
