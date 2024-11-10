using Microsoft.Win32;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System.IO;
using System.Linq;
using System.Text;

namespace Unicorn.Toolbox.Commands;

public class ExportVisualizationCommand : CommandBase
{
    public override void Execute(object parameter)
    {
        const string delimiter = ",";
        var saveDialog = new SaveFileDialog
        {
            Filter = "Csv files|*.csv"
        };

        if (saveDialog.ShowDialog().Value)
        {
            var csv = new StringBuilder();

            PlotView plotView = parameter as PlotView;

            double[] values = (plotView.ActualModel.Series[0] as BarSeries).ActualItems.Select(i => i.Value).ToArray();
            string[] keys = (plotView.ActualModel.Axes[0] as CategoryAxis).ItemsSource.Cast<string>().ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                csv.Append(keys[i]).Append(delimiter).Append(values[i]).AppendLine();
            }

            File.WriteAllText(saveDialog.FileName, csv.ToString());
        }
    }
}
