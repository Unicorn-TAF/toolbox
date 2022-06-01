using System.Linq;
using System.Windows;
using Unicorn.Toolbox.Models.Launch;
using Unicorn.Toolbox.ViewModels;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox.Commands
{
    public class VisualizeCommand : CommandBase
    {
        private readonly MainViewModel _viewModel;

        public VisualizeCommand(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public override void Execute(object parameter)
        {
            if (_viewModel.CurrentViewModel is StatsViewModel stats)
            {
                VisualizeStatistics(stats);
            } 
            else if (_viewModel.CurrentViewModel is CoverageViewModel coverage)
            {
                VisualizeCoverage(coverage);
            }
            else
            {
                VisualizeResults(_viewModel.CurrentViewModel as LaunchViewModel);
            }
        }

        private void VisualizeStatistics(StatsViewModel stats)
        {
            WindowVisualization visualization = GetVisualizationWindow($"Overall tests statistics: {stats.CurrentFilter}");

            var data = stats.GetVisualizationData();

            if (_viewModel.CirclesVisualization)
            {
                new VisualizerCircles(visualization.canvasVisualization, _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(data);
            }
            else
            {
                new VisualizerBars(visualization.canvasVisualization, _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(data);

                visualization.InjectExportToVisualization();
            }
        }

        private void VisualizeCoverage(CoverageViewModel coverage)
        {
            WindowVisualization visualization = GetVisualizationWindow("Modules coverage by tests");

            var vizData = coverage.GetVisualizationData();

            if (_viewModel.CirclesVisualization)
            {
                new VisualizerCircles(visualization.canvasVisualization, _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(vizData);
            }
            else
            {
                new VisualizerBars(visualization.canvasVisualization, _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(vizData);

                visualization.InjectExportToVisualization();
            }
        }

        private void VisualizeResults(LaunchViewModel launchResults)
        {
            var visualization = GetVisualizationWindow("Launch visualization");

            new LaunchVisualizer(visualization.canvasVisualization, launchResults.ExecutionsList.Cast<Execution>().ToList())
                .Visualize();
        }

        private WindowVisualization GetVisualizationWindow(string title)
        {
            var visualization = new WindowVisualization
            {
                Title = title
            };

            if (_viewModel.FullscreenVisualization)
            {
                visualization.WindowState = WindowState.Maximized;
            }
            else
            {
                visualization.ShowActivated = false;
            }

            visualization.Show();

            return visualization;
        }
    }
}
