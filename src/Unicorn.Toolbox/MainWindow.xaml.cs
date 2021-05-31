using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Unicorn.Toolbox.Visualization;
using Unicorn.Toolbox.Visualization.Palettes;

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

        private void Visualize(object sender, RoutedEventArgs e)
        {
            if (tabStatistics.IsSelected)
            {
                VisualizeStatistics();
            }
            else if (tabCoverage.IsSelected)
            {
                VisualizeCoverage();
            }
            else if (tabResultsAnalysis.IsSelected)
            {
                VisualizeResults();
            }
        }

        private void InjectExportToVisualization(WindowVisualization visualization)
        {
            var button = new Button
            {
                Content = "Export",
                Margin = new Thickness(0, 0, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            button.Click += ExportStats;
            visualization.visualizationGrid.Children.Add(button);

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

                    foreach (var children in visualization.canvasVisualization.Children)
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

        private IPalette GetPalette()
        {
            switch (comboBoxPalette.Text)
            {
                case "Deep Purple":
                    return new DeepPurple();
                case "Orange":
                    return new Orange();
                default:
                    return new LightGreen();
            }
        }

        private WindowVisualization GetVisualizationWindow(string title)
        {
            var visualization = new WindowVisualization
            {
                Title = title
            };

            if (checkBoxFullscreen.IsChecked.HasValue && checkBoxFullscreen.IsChecked.Value)
            {
                visualization.WindowState = WindowState.Maximized;
            }
            else
            {
                visualization.ShowActivated = false;
            }

            return visualization;
        }

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabResultsAnalysis.IsSelected)
            {
                groupBoxVisualizationStateTemp = groupBoxVisualization.IsEnabled;
                groupBoxVisualization.IsEnabled = true;
                buttonVisualize.IsEnabled = trxLoaded;
                checkBoxFullscreen.IsEnabled = trxLoaded;
                checkBoxModern.IsEnabled = false;
                comboBoxPalette.IsEnabled = false;

                if (comboFilterExecutedTestsBy.SelectedIndex.Equals(-1))
                {
                    comboFilterExecutedTestsBy.SelectedIndex = 0;
                }

                statusBarText.Text = StatusLineResults;
            }
            else if (tabCoverage.IsSelected)
            {
                buttonVisualize.IsEnabled = true;
                checkBoxFullscreen.IsEnabled = true;
                checkBoxModern.IsEnabled = true;
                comboBoxPalette.IsEnabled = true;
                groupBoxVisualization.IsEnabled = groupBoxVisualizationStateTemp;

                statusBarText.Text = StatusLineCoverage;
            }
            else if (tabStatistics.IsSelected)
            {
                statusBarText.Text = StatusLineStatistics;
            }
            else
            {
                throw new NotImplementedException($"Please update {nameof(statusBarText)} on tab change");
            }
        }
    }
}
