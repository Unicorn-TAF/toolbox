using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Unicorn.Toolbox.Coverage;

namespace Unicorn.Toolbox.Views
{
    /// <summary>
    /// Interaction logic for CoverageView.xaml
    /// </summary>
    public partial class CoverageView : UserControl
    {
        internal SpecsCoverage _coverage; // TODO

        public CoverageView()
        {
            InitializeComponent();
        }

        public string Status { get; set; } = string.Empty;

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

            UiUtils.FillGrid(gridModules, new HashSet<string>(_coverage.Specs.Modules.Select(m => m.Name)));

            foreach (var checkbox in gridModules.Children)
            {
                ((CheckBox)checkbox).IsChecked = false;
                ((CheckBox)checkbox).Checked += new RoutedEventHandler(UpdateRunTagsText);
                ((CheckBox)checkbox).Unchecked += new RoutedEventHandler(UpdateRunTagsText);
            }

            // TODO
            //if (!StatisticsView.gridStatistics.IsEnabled)
            //{
            //    buttonGetCoverage.IsEnabled = true;
            //}

            GetCoverage();
        }

        private void GetCoverage()
        {
            //TODO 
            //_coverage.Analyze(StatisticsView.analyzer.Data.FilteredInfo);
            gridCoverage.ItemsSource = null;
            gridCoverage.ItemsSource = _coverage.Specs.Modules;
        }

        private void UpdateRunTagsText(object sender, RoutedEventArgs e)
        {
            var runTags = new HashSet<string>();

            foreach (var child in gridModules.Children)
            {
                var checkbox = child as CheckBox;

                if (checkbox.IsChecked.Value)
                {
                    runTags.UnionWith(_coverage.Specs.Modules
                        .First(m => m.Name.Equals(checkbox.Content.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        .Features);
                }
            }

            textBoxRunTags.Text = "#" + string.Join(" #", runTags);
        }

        private void GetAutomationCoverage(object sender, RoutedEventArgs e) =>
            GetCoverage();
    }
}
