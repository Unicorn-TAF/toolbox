using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Unicorn.Toolbox.Models.Launch;
using Unicorn.Toolbox.ViewModels;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox.Commands;

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
            VisualizeStatistics(stats, _viewModel.CurrentVisualizationPalette);
        } 
        else if (_viewModel.CurrentViewModel is CoverageViewModel coverage)
        {
            VisualizeCoverage(coverage, _viewModel.CurrentVisualizationPalette);
        }
        else
        {
            VisualizeResults(_viewModel.CurrentViewModel as LaunchViewModel);
        }
    }

    private void VisualizeStatistics(StatsViewModel stats, IPalette palette)
    {
        string title = $"Overall tests statistics :: {stats.CurrentFilter.FilterName}";
        VisualizationViewModel visualizationVm = new(stats.GetVisualizationData(), palette, title);
        ShowChartVisualizationDialog(title, visualizationVm);
    }

    private void VisualizeCoverage(CoverageViewModel coverage, IPalette palette)
    {
        string title = "Modules coverage by tests";
        VisualizationViewModel visualizationVm = new(coverage.GetVisualizationData(), palette, title);
        ShowChartVisualizationDialog(title, visualizationVm);
    }

    private void ShowChartVisualizationDialog(string title, VisualizationViewModel visualizationVm)
    {
        DialogHost window = new(title)
        {
            DataContext = new DialogHostViewModel(visualizationVm),
            ShowActivated = true,
            ResizeMode = ResizeMode.CanResize
        };

        if (_viewModel.FullscreenVisualization)
        {
            window.WindowState = WindowState.Maximized;
        }

        window.Show();
    }

    private void VisualizeResults(LaunchViewModel launchResults)
    {
        DialogHost visualization = GetVisualizationDialog("Launch visualization", false);
        List<Execution> executions = launchResults.ExecutionsList.Cast<Execution>().ToList();

        new LaunchVisualizer(GetCanvasFrom(visualization), executions)
            .Visualize();
    }

    private DialogHost GetVisualizationDialog(string title, bool statsVisualization)
    {
        VisualizationViewModel visualizationVm = new VisualizationViewModel
        {
            IsStatsVisualization = statsVisualization
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

    private static Canvas GetCanvasFrom(DependencyObject depObj)
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
