using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Unicorn.Toolbox.Visualization
{
    /// <summary>
    /// Interaction logic for Visualizer
    /// </summary>
    public partial class WindowVisualization : Window
    {
        public WindowVisualization()
        {
            InitializeComponent();
        }

        public void InjectExportToVisualization()
        {
            var button = new Button
            {
                Content = "Export",
                Margin = new Thickness(0, 0, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            button.Click += ExportStats;
            visualizationGrid.Children.Add(button);

            void ExportStats(object sender, RoutedEventArgs e)
            {
                const string delimiter = ",";
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Csv files|*.csv"
                };

                if (saveDialog.ShowDialog().Value)
                {
                    var csv = new StringBuilder();

                    foreach (var children in canvasVisualization.Children)
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
    }
}
