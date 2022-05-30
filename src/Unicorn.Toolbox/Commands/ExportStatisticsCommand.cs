using Microsoft.Win32;
using System.IO;
using System.Text;
using Unicorn.Toolbox.Models.Stats;

namespace Unicorn.Toolbox.Commands
{
    public class ExportStatsCommand : CommandBase
    {
        private readonly StatsCollector _analyzer;

        public ExportStatsCommand(StatsCollector analyzer)
        {
            _analyzer = analyzer;
        }

        public override void Execute(object parameter)
        {
            const string delimiter = ",";

            char[] chars = { '\t', '\r', '\n', '\"', ',' };

            var saveDialog = new SaveFileDialog
            {
                Filter = "Csv files|*.csv"
            };

            if (saveDialog.ShowDialog().Value)
            {
                var csv = new StringBuilder();

                csv.AppendLine(string.Join(delimiter, "Suite", "Test", "Author", "Tags", "Categories", "Disabled"));

                foreach (var suite in _analyzer.Data.FilteredInfo)
                {
                    var tags = string.Join("#", suite.Tags).ToLowerInvariant();
                    
                    var suiteName = suite.Name.IndexOfAny(chars) >= 0 ? 
                        '\"' + suite.Name.Replace("\"", "\"\"") + '\"' : 
                        suite.Name;

                    foreach (var test in suite.TestsInfos)
                    {
                        var disabled = test.Disabled ? "Y" : "N";
                        var categories = string.Join("#", test.Categories).ToLowerInvariant();

                        var testName = test.Name.IndexOfAny(chars) >= 0 ? 
                            '\"' + test.Name.Replace("\"", "\"\"") + '\"' : 
                            test.Name;

                        csv.AppendLine(string.Join(
                            delimiter, suiteName, testName, test.Author, tags, categories, disabled));
                    }
                }

                File.WriteAllText(saveDialog.FileName, csv.ToString());
            }
        }
    }
}
