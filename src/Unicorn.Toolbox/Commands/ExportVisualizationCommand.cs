using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

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

            foreach (var children in (parameter as Canvas).Children)
            {
                if (children is TextBlock block)
                {
                    var pair = block.Text.Split(':').Select(p => p.Trim());
                    csv.AppendLine(string.Join(delimiter, pair));
                }
            }

            File.WriteAllText(saveDialog.FileName, csv.ToString());
        }
    }
}
