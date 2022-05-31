using System;
using Unicorn.Toolbox.Models.Stats;

namespace Unicorn.Toolbox.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string status;
        private int selectedTab;

        public MainViewModel(StatsCollector statsCollector)
        {
            StatsViewModel = new StatsViewModel(statsCollector);
            CoverageViewModel = new CoverageViewModel(statsCollector);
            LaunchViewModel = new LaunchViewModel();
            VisualizationViewModel = new VisualizationViewModel();
        }

        public StatsViewModel StatsViewModel { get; }

        public CoverageViewModel CoverageViewModel { get; }

        public LaunchViewModel LaunchViewModel { get; }

        public VisualizationViewModel VisualizationViewModel { get; }

        public string Status
        {
            get => status;

            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public int SelectedTab
        {
            get => selectedTab;

            set
            {
                selectedTab = value;
                OnPropertyChanged(nameof(SelectedTab));
                TabChanged(selectedTab);
            }
        }

        private void TabChanged(int selectedTab)
        {
            switch(selectedTab)
            {
                case 0:
                    Status = StatsViewModel.Status;
                    VisualizationViewModel.CurrentViewModel = StatsViewModel;
                    VisualizationViewModel.Available = StatsViewModel.DataLoaded;
                    VisualizationViewModel.CanCustomize = true;
                    break;
                case 1:
                    Status = CoverageViewModel.Status;
                    VisualizationViewModel.CurrentViewModel = CoverageViewModel;
                    VisualizationViewModel.Available = CoverageViewModel.DataLoaded;
                    VisualizationViewModel.CanCustomize = true;
                    break;
                case 2:
                    Status = LaunchViewModel.Status;
                    VisualizationViewModel.CurrentViewModel = LaunchViewModel;
                    VisualizationViewModel.Available = LaunchViewModel.TrxLoaded;
                    VisualizationViewModel.CanCustomize = false;
                    break;
                default:
                    throw new NotImplementedException($"Please update {nameof(Status)} on tab change");
            }
        }
    }
}
