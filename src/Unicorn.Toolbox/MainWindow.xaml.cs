using System;
using System.Windows;
using System.Windows.Controls;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox
{
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private VisualizationViewModel Visualization => VisualizationView.DataContext as VisualizationViewModel;

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabResultsAnalysis.IsSelected)
            {
                var launchResultsViewModel = LaunchResultsView.DataContext as LaunchResultsViewModel;
                Visualization.CurrentViewModel = launchResultsViewModel;

                Visualization.Available = launchResultsViewModel.TrxLoaded;
                Visualization.CanCustomize = false;
                statusBarText.Text = launchResultsViewModel.Status;
            }
            else if (tabCoverage.IsSelected)
            {
                var coverageViewModel = CoverageView.DataContext as CoverageViewModel;
                Visualization.CurrentViewModel = coverageViewModel;

                Visualization.Available = coverageViewModel.DataLoaded;
                Visualization.CanCustomize = true;
                statusBarText.Text = coverageViewModel.Status;
            }
            else if (tabStatistics.IsSelected)
            {
                var statsViewModel = StatisticsView.DataContext as StatisticsViewModel;
                
                if (statsViewModel != null)
                {
                    Visualization.CurrentViewModel = statsViewModel;
                    statusBarText.Text = statsViewModel.Status;
                    Visualization.CanCustomize = true;
                }
            }
            else
            {
                throw new NotImplementedException($"Please update {nameof(statusBarText)} on tab change");
            }
        }

    }
}
