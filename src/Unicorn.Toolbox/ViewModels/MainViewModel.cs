using System;
using System.Collections.Generic;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.Visualization;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string status;
        private int selectedTab;

        private IPalette currentPalette;
        private bool fullscreen;
        private bool circles;
        private bool available;
        private bool canCustomize;

        public MainViewModel(StatsCollector statsCollector)
        {
            StatsViewModel = new StatsViewModel(statsCollector);
            CoverageViewModel = new CoverageViewModel(statsCollector);
            LaunchViewModel = new LaunchViewModel();

            CurrentVisualizationPalette = new LightGreen();
            VisualizationPalettes = new List<IPalette>() { CurrentVisualizationPalette, new Orange(), new DeepPurple() };
            VisualizeCommand = new VisualizeCommand(this);
            CurrentViewModel = StatsViewModel;
        }

        public StatsViewModel StatsViewModel { get; }

        public CoverageViewModel CoverageViewModel { get; }

        public LaunchViewModel LaunchViewModel { get; }

        public ViewModelBase CurrentViewModel { get; set; }

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

        public bool VisualizationAvailable
        {
            get => available;
            set
            {
                available = value;
                OnPropertyChanged(nameof(VisualizationAvailable));
            }
        }

        public bool CanCustomizeVisualization
        {
            get => canCustomize;
            set
            {
                canCustomize = value;
                OnPropertyChanged(nameof(CanCustomizeVisualization));
            }
        }

        public bool FullscreenVisualization
        {
            get => fullscreen;
            set
            {
                fullscreen = value;
                OnPropertyChanged(nameof(FullscreenVisualization));
            }
        }

        public bool CirclesVisualization
        {
            get => circles;
            set
            {
                circles = value;
                OnPropertyChanged(nameof(CirclesVisualization));
            }
        }

        public IEnumerable<IPalette> VisualizationPalettes { get; }

        public IPalette CurrentVisualizationPalette
        {
            get => currentPalette;
            set
            {
                currentPalette = value;
                OnPropertyChanged(nameof(CurrentVisualizationPalette));
            }
        }

        public ICommand VisualizeCommand { get; }

        private void TabChanged(int selectedTab)
        {
            switch(selectedTab)
            {
                case 0:
                    Status = StatsViewModel.Status;
                    CurrentViewModel = StatsViewModel;
                    VisualizationAvailable = StatsViewModel.DataLoaded;
                    CanCustomizeVisualization = true;
                    break;
                case 1:
                    Status = CoverageViewModel.Status;
                    CurrentViewModel = CoverageViewModel;
                    VisualizationAvailable = CoverageViewModel.DataLoaded;
                    CanCustomizeVisualization = true;
                    break;
                case 2:
                    Status = LaunchViewModel.Status;
                    CurrentViewModel = LaunchViewModel;
                    VisualizationAvailable = LaunchViewModel.DataLoaded;
                    CanCustomizeVisualization = false;
                    break;
                default:
                    throw new NotImplementedException($"Please update {nameof(Status)} on tab change");
            }
        }
    }
}
