﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Analysis.Filtering;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.LaunchAnalysis;
using Unicorn.Toolbox.Visualization;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox
{
    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        private Analyzer analyzer;
        private SpecsCoverage coverage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadTestsAssembly(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Unicorn tests assemblies|*.dll";
            openFileDialog.ShowDialog();

            string assemblyFile = openFileDialog.FileName;

            if (string.IsNullOrEmpty(assemblyFile))
            {
                return;
            }

            this.gridStatistics.IsEnabled = true;
            this.buttonVisualize.IsEnabled = true;
            this.comboBoxPalette.IsEnabled = true;
            this.checkBoxModern.IsEnabled = true;
            this.checkBoxFullscreen.IsEnabled = true;

            this.analyzer = new Analyzer(assemblyFile);
            this.analyzer.GetTestsStatistics();

            var statusLine = $"Assembly: {this.analyzer.AssemblyFileName} ({this.analyzer.TestsAssemblyName})    |    " + this.analyzer.Data.ToString();
            this.textBoxStatistics.Text = statusLine;

            FillFiltersFrom(analyzer.Data);
            ShowAll();
            this.checkBoxShowHide.IsChecked = true;
        }

        private void FillFiltersFrom(AutomationData data)
        {
            FillGrid(gridFeatures, data.UniqueFeatures);
            FillGrid(gridCategories, data.UniqueCategories);
            FillGrid(gridAuthors, data.UniqueAuthors);
        }

        private void FillGrid(Grid grid, HashSet<string> items)
        {
            var sortedItems = items.OrderBy(s => s).ToList();

            grid.Children.Clear();
            grid.RowDefinitions.Clear();

            grid.Height = (sortedItems.Count + 2) * 20;

            for (int i = 0; i < sortedItems.Count + 2; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            int index = 2;

            foreach (var item in sortedItems)
            {
                var itemCheckbox = new CheckBox();
                itemCheckbox.Content = item;
                itemCheckbox.IsChecked = true;
                grid.Children.Add(itemCheckbox);
                Grid.SetRow(itemCheckbox, index++);
            }
        }

        private void ApplyFilter(object sender, RoutedEventArgs e)
        {
            var features = from CheckBox cBox in gridFeatures.Children where cBox.IsChecked.Value select (string)cBox.Content;
            var categories = from CheckBox cBox in gridCategories.Children where cBox.IsChecked.Value select (string)cBox.Content;
            var authors = from CheckBox cBox in gridAuthors.Children where cBox.IsChecked.Value select (string)cBox.Content;

            this.analyzer.Data.ClearFilters();
            this.analyzer.Data.FilterBy(new FeaturesFilter(features));
            this.analyzer.Data.FilterBy(new CategoriesFilter(categories));
            this.analyzer.Data.FilterBy(new AuthorsFilter(authors));

            if (checkOnlyDisabledTests.IsChecked.Value)
            {
                this.analyzer.Data.FilterBy(new OnlyDisabledFilter());
            }

            if (checkOnlyEnabledTests.IsChecked.Value)
            {
                this.analyzer.Data.FilterBy(new OnlyEnabledFilter());
            }

            gridResults.ItemsSource = analyzer.Data.FilteredInfo;

            textBoxCurrentFilter.Text = $"Filter by:\nFeatures[{string.Join(",", features)}]\n";
            textBoxCurrentFilter.Text += $"Categories[{string.Join(",", categories)}]\n";
            textBoxCurrentFilter.Text += $"Authors[{string.Join(",", authors)}]";
        }

        private void ClickCellWithSuite(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                string testSuiteName = (sender as TextBlock).Text;

                var preview = new WindowTestPreview();
                preview.ShowActivated = false;
                preview.Show();
                preview.gridResults.ItemsSource = analyzer.Data.FilteredInfo.First(s => s.Name.Equals(testSuiteName)).TestsInfos;
            }
        }

        private void ShowAllClick(object sender, RoutedEventArgs e)
        {
            ShowAll();
        }

        private void LoadSpecs(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Application specs|*.json";
            openFileDialog.ShowDialog();

            string specFileName = openFileDialog.FileName;

            if (string.IsNullOrEmpty(specFileName))
            {
                return;
            }

            this.buttonGetCoverage.IsEnabled = true;

            this.coverage = new SpecsCoverage(specFileName);

            FillGrid(gridModules, new HashSet<string>(this.coverage.Specs.Modules.Select(m => m.Name)));

            foreach (var checkbox in gridModules.Children)
            {
                ((CheckBox)checkbox).IsChecked = false;
                ((CheckBox)checkbox).Checked += new RoutedEventHandler(this.UpdateRunTagsText);
                ((CheckBox)checkbox).Unchecked += new RoutedEventHandler(this.UpdateRunTagsText);
            }

            if (!gridStatistics.IsEnabled)
            {
                this.buttonGetCoverage.IsEnabled = true;
            }
        }


        private void UpdateRunTagsText(object sender, RoutedEventArgs e)
        {
            var runTags = new HashSet<string>();

            foreach (var child in gridModules.Children)
            {
                var checkbox = child as CheckBox;

                if (checkbox.IsChecked.Value)
                {
                    runTags.UnionWith(this.coverage.Specs.Modules
                        .First(m => m.Name.Equals(checkbox.Content.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        .Features);
                }
            }

            this.textBoxRunTags.Text = "#" + string.Join(" #", runTags);
        }

        private void GetAutomationCoverage(object sender, RoutedEventArgs e) =>
            GetCoverage();

        private void Visualize(object sender, RoutedEventArgs e)
        {
            if (tabStatistics.IsSelected)
            {
                VisualizeStatistics();
            }
            else
            {
                VisualizeCoverage();
            }
        }

        private void ShowAll()
        {
            this.analyzer.Data.ClearFilters();
            gridResults.ItemsSource = analyzer.Data.FilteredInfo;
        }

        private void GetCoverage()
        {
            this.coverage.Analyze(this.analyzer.Data.FilteredInfo);
            this.gridCoverage.ItemsSource = null;
            this.gridCoverage.ItemsSource = coverage.Specs.Modules;
        }

        private FilterType GetFilter()
        {
            if (tabFeaures.IsSelected)
            {
                return FilterType.Feature;
            }
            else if (tabCategories.IsSelected)
            {
                return FilterType.Category;
            }
            else
            {
                return FilterType.Author;
            }
        }

        private void VisualizeCoverage()
        {
            var visualization = new WindowVisualization();
            
            visualization.Title = "Modules coverage by tests";

            if (this.checkBoxFullscreen.IsChecked.HasValue && this.checkBoxFullscreen.IsChecked.Value)
            {
                visualization.WindowState = WindowState.Maximized;
            }
            else
            {
                visualization.ShowActivated = false;
            }

            visualization.Show();

            if (checkBoxModern.IsChecked.HasValue && checkBoxModern.IsChecked.Value)
            {
                VisualizerCircles.VisualizeCoverage(coverage.Specs, GetPalette(), visualization.canvasVisualization);
            }
            else
            {
                VizualizerBars.VisualizeCoverage(coverage.Specs, GetPalette(), visualization.canvasVisualization);
            }
        }

        private void VisualizeStatistics()
        {
            var filter = GetFilter();
            var visualization = new WindowVisualization();
            
            visualization.Title = $"Overall tests statistics: {filter}";

            if (this.checkBoxFullscreen.IsChecked.HasValue && this.checkBoxFullscreen.IsChecked.Value)
            {
                visualization.WindowState = WindowState.Maximized;
            }
            else
            {
                visualization.ShowActivated = false;
            }

            visualization.Show();

            if (checkBoxModern.IsChecked.HasValue && checkBoxModern.IsChecked.Value)
            {
                VisualizerCircles.VisualizeAllData(analyzer.Data, filter, GetPalette(), visualization.canvasVisualization);
            }
            else
            {
                VizualizerBars.VisualizeAllData(analyzer.Data, filter, GetPalette(), visualization.canvasVisualization);
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

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var activeGrid = GetActiveGrid();

            foreach (var child in activeGrid.Children)
            {
                ((CheckBox)child).IsChecked = false;
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            var activeGrid = GetActiveGrid();

            foreach (var child in activeGrid.Children)
            {
                ((CheckBox)child).IsChecked = true;
            }
        }

        private Grid GetActiveGrid()
        {
            if (tabFeaures.IsSelected)
            {
                return gridFeatures;
            }
            else if (tabCategories.IsSelected)
            {
                return gridCategories;
            }
            else
            {
                return gridAuthors;
            }
        }

        private void btnVisualizeLaunch_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Trx files|*.trx";
            openFileDialog.Multiselect = true;
            openFileDialog.ShowDialog();

            var trxFiles = openFileDialog.FileNames;

            if (!trxFiles.Any())
            {
                return;
            }

            List<List<TestResult>> resultsList = new List<List<TestResult>>();

            foreach (var trxFile in trxFiles)
            {
                try
                {
                    var results = new TrxParser(trxFile).GetAllTests();

                    if (results.Any())
                    {
                        resultsList.Add(results);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing {trxFile} file:" + ex.ToString());
                }

            }

            var visualization = new WindowVisualization();
            visualization.ShowActivated = false;
            visualization.Title = "Launch visualization";
            
            visualization.Show();
            ////visualization.WindowState = WindowState.Maximized;

            new LaunchVisualizer(visualization.canvasVisualization, resultsList).Visualize();
        }

        private void checkOnlyDisabledTests_Checked(object sender, RoutedEventArgs e) =>
            checkOnlyEnabledTests.IsChecked = false;

        private void checkOnlyEnabledTests_Checked(object sender, RoutedEventArgs e) =>
            checkOnlyDisabledTests.IsChecked = false;
    }
}
