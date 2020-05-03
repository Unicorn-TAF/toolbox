using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox
{
    public partial class MainWindow
    {
        private SpecsCoverage _coverage;

        private void LoadSpecs(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Application specs|*.json"
            };

            openFileDialog.ShowDialog();

            string specFileName = openFileDialog.FileName;

            if (string.IsNullOrEmpty(specFileName))
            {
                return;
            }

            buttonGetCoverage.IsEnabled = true;

            _coverage = new SpecsCoverage(specFileName);

            FillGrid(gridModules, new HashSet<string>(_coverage.Specs.Modules.Select(m => m.Name)));

            foreach (var checkbox in gridModules.Children)
            {
                ((CheckBox)checkbox).IsChecked = false;
                ((CheckBox)checkbox).Checked += new RoutedEventHandler(UpdateRunTagsText);
                ((CheckBox)checkbox).Unchecked += new RoutedEventHandler(UpdateRunTagsText);
            }

            if (!gridStatistics.IsEnabled)
            {
                buttonGetCoverage.IsEnabled = true;
            }
        }

        private void GetAutomationCoverage(object sender, RoutedEventArgs e) =>
            GetCoverage();

        private void GetCoverage()
        {
            _coverage.Analyze(analyzer.Data.FilteredInfo);
            gridCoverage.ItemsSource = null;
            gridCoverage.ItemsSource = _coverage.Specs.Modules;
        }

        private void VisualizeCoverage()
        {
            var visualization = GetVisualizationWindow("Modules coverage by tests");
            visualization.Show();

            if (checkBoxModern.IsChecked.HasValue && checkBoxModern.IsChecked.Value)
            {
                new VisualizerCircles(visualization.canvasVisualization, GetPalette()).VisualizeCoverage(_coverage.Specs);
            }
            else
            {
                new VisualizerBars(visualization.canvasVisualization, GetPalette()).VisualizeCoverage(_coverage.Specs);
                InjectExportToVisualization(visualization);
            }
        }
    }
}
