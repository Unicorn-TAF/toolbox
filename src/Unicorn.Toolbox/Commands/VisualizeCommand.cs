using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
            var data = stats.GetVisualizationData();

            DialogHost visualization = GetVisualizationDialog(
                $"Overall tests statistics: {stats.CurrentFilter}", !_viewModel.CirclesVisualization);

            if (_viewModel.CirclesVisualization)
            {
                new VisualizerCircles(GetCanvasFrom(visualization), _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(data);
            }
            else
            {
                new VisualizerBars(GetCanvasFrom(visualization), _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(data);
            }
        }

        private void VisualizeCoverage(CoverageViewModel coverage)
        {
            var vizData = coverage.GetVisualizationData();
            DialogHost visualization = GetVisualizationDialog("Modules coverage by tests", !_viewModel.CirclesVisualization);

            if (_viewModel.CirclesVisualization)
            {
                new VisualizerCircles(GetCanvasFrom(visualization), _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(vizData);
            }
            else
            {
                new VisualizerBars(GetCanvasFrom(visualization), _viewModel.CurrentVisualizationPalette)
                    .VisualizeData(vizData);
            }
        }

        private void VisualizeResults(LaunchViewModel launchResults)
        {
            DialogHost visualization = GetVisualizationDialog("Launch visualization", false);
            List<Execution> executions = launchResults.ExecutionsList.Cast<Execution>().ToList();

            new LaunchVisualizer(GetCanvasFrom(visualization), executions)
                .Visualize();
        }

        private DialogHost GetVisualizationDialog(string title, bool exportable)
        {
            VisualizationViewModel visualizationVm = new VisualizationViewModel
            {
                Exportable = exportable
            };

            var visualization = new DialogHost(title)
            {
                DataContext = new DialogHostViewModel(visualizationVm),
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

        public Canvas GetCanvasFrom(DependencyObject depObj)
        {
            if (depObj == null)
            {
                return null;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(depObj);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as Canvas) ?? GetCanvasFrom(child);

                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
