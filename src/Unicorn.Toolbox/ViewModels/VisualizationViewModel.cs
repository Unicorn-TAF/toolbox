using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox.ViewModels;

public class VisualizationViewModel : IDialogViewModel
{
    public VisualizationViewModel()
    {
        ExportVisualizationCommand = new ExportVisualizationCommand();
    }

    public VisualizationViewModel(IOrderedEnumerable<KeyValuePair<string, int>> data, IPalette palette, string title) : this()
    {
        BuildPlotModel(data, palette, title);
        IsStatsVisualization = true;
    }

    [Notify]
    public bool IsStatsVisualization { get; set; }

    public PlotModel VisualizationPlotModel { get; set; }

    public ICommand ExportVisualizationCommand { get; }

    private void BuildPlotModel(IOrderedEnumerable<KeyValuePair<string, int>> data, IPalette palette, string title)
    {
        VisualizationPlotModel = new PlotModel
        {
            Title = title,
        };

        IEnumerable<KeyValuePair<string, int>> reversedData = data.Reverse();

        BarSeries barSeries = new()
        {
            ItemsSource = reversedData.Select(x => new BarItem { Value = x.Value, Color = palette.MainColor }).ToList(),
        };

        VisualizationPlotModel.Series.Add(barSeries);

        VisualizationPlotModel.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Left,
            ItemsSource = reversedData.Select(p => p.Key).ToArray()
        });

        VisualizationPlotModel.SelectionColor = OxyColors.Black;
    }
}
