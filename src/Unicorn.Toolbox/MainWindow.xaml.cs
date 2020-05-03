using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
        private Analyzer _analyzer;
        private SpecsCoverage _coverage;
        private LaunchResult _launchResult;
        private ExecutedTestsFilter _failedTestsFilter;

        private bool groupBoxVisualizationStateTemp = false;
        private bool trxLoaded = false;
        private ListCollectionView listCollectionView;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadTestsAssembly(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Unicorn tests assemblies|*.dll"
            };
            openFileDialog.ShowDialog();

            groupBoxVisualization.IsEnabled = true;
            groupBoxVisualizationStateTemp = true;
            var assemblyFile = openFileDialog.FileName;

            if (string.IsNullOrEmpty(assemblyFile))
            {
                return;
            }

            gridFilters.IsEnabled = true;

            _analyzer = new Analyzer(assemblyFile);
            _analyzer.GetTestsStatistics();

            var statusLine = $"Assembly: {_analyzer.AssemblyFileName} ({_analyzer.TestsAssemblyName})    |    " + this._analyzer.Data.ToString();
            textBoxStatistics.Text = statusLine;

            FillFiltersFrom(_analyzer.Data);
            ShowAll();
            checkBoxShowHide.IsChecked = true;
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

            _analyzer.Data.ClearFilters();
            _analyzer.Data.FilterBy(new FeaturesFilter(features));
            _analyzer.Data.FilterBy(new CategoriesFilter(categories));
            _analyzer.Data.FilterBy(new AuthorsFilter(authors));

            if (checkOnlyDisabledTests.IsChecked.Value)
            {
                _analyzer.Data.FilterBy(new OnlyDisabledFilter());
            }

            if (checkOnlyEnabledTests.IsChecked.Value)
            {
                _analyzer.Data.FilterBy(new OnlyEnabledFilter());
            }

            gridResults.ItemsSource = _analyzer.Data.FilteredInfo;

            var foundTestsCount = _analyzer.Data.FilteredInfo.SelectMany(si => si.TestsInfos).Count();

            var filterText = new StringBuilder()
                .AppendFormat("Found {0} tests. Filters:\nFeatures[{1}]\n", foundTestsCount, string.Join(",", features))
                .AppendFormat("Categories[{0}]\n", string.Join(", ", categories))
                .AppendFormat("Authors[{0}]", string.Join(", ", authors));

            textBoxCurrentFilter.Text = filterText.ToString();
        }

        private void ClickCellWithSuite(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                string testSuiteName = (sender as TextBlock).Text;

                var preview = new WindowTestPreview();
                preview.Title += testSuiteName;
                preview.ShowActivated = false;
                preview.Show();
                preview.gridResults.ItemsSource = _analyzer.Data.FilteredInfo.First(s => s.Name.Equals(testSuiteName)).TestsInfos;
            }
        }

        private void ShowAllClick(object sender, RoutedEventArgs e) => ShowAll();

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

        private void ShowAll()
        {
            _analyzer.Data.ClearFilters();
            gridResults.ItemsSource = _analyzer.Data.FilteredInfo;
            textBoxCurrentFilter.Text = string.Empty;
        }

        private void GetCoverage()
        {
            _coverage.Analyze(_analyzer.Data.FilteredInfo);
            gridCoverage.ItemsSource = null;
            gridCoverage.ItemsSource = _coverage.Specs.Modules;
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

        private void VisualizeStatistics()
        {
            var filter = GetFilter();

            var visualization = GetVisualizationWindow($"Overall tests statistics: {filter}");
            visualization.Show();

            if (checkBoxModern.IsChecked.HasValue && checkBoxModern.IsChecked.Value)
            {
                new VisualizerCircles(visualization.canvasVisualization, GetPalette()).VisualizeAutomationData(_analyzer.Data, filter);
            }
            else
            {
                new VisualizerBars(visualization.canvasVisualization, GetPalette()).VisualizeAutomationData(_analyzer.Data, filter);
                InjectExportToVisualization(visualization);
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
                        if (children is TextBlock)
                        {
                            var pair = ((TextBlock)children).Text.Split(':').Select(p => p.Trim());
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

        private void btnLoadTrx_Click(object sender, RoutedEventArgs e)
        {
            buttonVisualize.IsEnabled = false;
            checkBoxFullscreen.IsEnabled = false;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Trx files|*.trx",
                Multiselect = true
            };

            openFileDialog.ShowDialog();

            var trxFiles = openFileDialog.FileNames;

            if (!trxFiles.Any())
            {
                return;
            }

            _launchResult = new LaunchResult();

            foreach (var trxFile in trxFiles)
            {
                try
                {
                    _launchResult.AppendResultsFromTrx(trxFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error parsing {trxFile} file:" + ex.ToString());
                }
            }

            var obColl = new ObservableCollection<Execution>(_launchResult.Executions);

            gridTestResults.ItemsSource = null;
            listCollectionView = new ListCollectionView(obColl);

            gridTestResults.ItemsSource = listCollectionView;

            textBoxLaunchSummary.Text = _launchResult.ToString();

            buttonVisualize.IsEnabled = true;
            checkBoxFullscreen.IsEnabled = true;
            trxLoaded = true;

            stackPanelFails.Children.Clear();

            var results = ExecutedTestsFilter.GetTopErrors(_launchResult.Executions.SelectMany(exec => exec.TestResults));

            for (int i = 0; i < results.Count(); i++)
            {
                stackPanelFails.Children.Add(new FailedTestsGroup(results.ElementAt(i)));
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (listCollectionView == null)
            {
                return;
            }

            listCollectionView.Filter = (item) => { return ((Execution)item).Name.Contains(((TextBox)sender).Text); };
        }

        private void VisualizeResults()
        {
            var visualization = GetVisualizationWindow("Launch visualization");
            visualization.Show();

            new LaunchVisualizer(visualization.canvasVisualization, _launchResult.Executions).Visualize();
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

        private void checkOnlyDisabledTests_Checked(object sender, RoutedEventArgs e) =>
            checkOnlyEnabledTests.IsChecked = false;

        private void checkOnlyEnabledTests_Checked(object sender, RoutedEventArgs e) =>
            checkOnlyDisabledTests.IsChecked = false;

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            }
            else
            {
                buttonVisualize.IsEnabled = true;
                checkBoxFullscreen.IsEnabled = true;
                checkBoxModern.IsEnabled = true;
                comboBoxPalette.IsEnabled = true;
                groupBoxVisualization.IsEnabled = groupBoxVisualizationStateTemp;
            }
        }

        private void ButtonSearchByFailMessage_Click(object sender, RoutedEventArgs e)
        {
            if (comboFilterExecutedTestsBy.Text.Equals("By fail message"))
            {
                _failedTestsFilter = new ExecutedTestsFilter(textBoxFailMessage.Text, checkboxFailMessageRegex.IsChecked.Value);
                _failedTestsFilter.FilterTestsByFailMessage(_launchResult.Executions.SelectMany(exec => exec.TestResults));
            }
            else
            {
                _failedTestsFilter = new ExecutedTestsFilter(textBoxFailMessage.Text);
                _failedTestsFilter.FilterTestsByTime(_launchResult.Executions.SelectMany(exec => exec.TestResults));
            }

            labelFoundFailedTests.Content = "Tests found: " + _failedTestsFilter.MatchingTestsCount;
        }

        private void LabelFoundFailedTests_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = new WindowTestsByMessage();
            window.gridResults.ItemsSource = _failedTestsFilter.FilteredResults;
            window.ShowDialog();
        }

        private void ComboBoxItem_Selected(object sender, RoutedEventArgs e) =>
            checkboxFailMessageRegex.Visibility = Visibility.Visible;

        private void ComboBoxItem_Selected_1(object sender, RoutedEventArgs e) =>
            checkboxFailMessageRegex.Visibility = Visibility.Hidden;
    }
}
