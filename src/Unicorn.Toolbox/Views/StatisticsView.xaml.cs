using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Analysis.Filtering;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Views
{
    /// <summary>
    /// Interaction logic for StatisticsView.xaml
    /// </summary>
    public partial class StatisticsView : UserControl
    {

        public StatisticsView()
        {
            InitializeComponent();
        }

        internal Analyzer analyzer;

        public string Status { get; set; } = string.Empty;

        private void DataGridCell_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string testSuiteName = (sender as DataGridCell).Content.ToString();

            var window = new DialogHost("Suite preview: " + testSuiteName)
            {
                DataContext = new DialogHostViewModel(
                    analyzer.Data.FilteredInfo.First(s => s.Name.Equals(testSuiteName)).TestsInfos),
                ShowActivated = false
            };

            //window.SetDataSource(FailedResults);
            window.ShowDialog();

            // TODO
            //WindowTestPreview preview = new WindowTestPreview();
            //preview.Title += testSuiteName;
            //preview.ShowActivated = false;
            //preview.Show();
            //preview.SetDataSource(analyzer.Data.FilteredInfo.First(s => s.Name.Equals(testSuiteName)).TestsInfos);
        }

        private void ShowAllClick(object sender, RoutedEventArgs e) => ShowAll();

        private void ShowAll()
        {
            analyzer.Data.ClearFilters();
            gridResults.ItemsSource = analyzer.Data.FilteredInfo;
            textBoxCurrentFilter.Text = string.Empty;
        }

        private void HideAll(object sender, RoutedEventArgs e)
        {
            var activeGrid = GetActiveGrid();

            foreach (var child in activeGrid.Children)
            {
                ((CheckBox)child).IsChecked = false;
            }
        }

        private void ShowAll(object sender, RoutedEventArgs e)
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

        private void ApplyFilter(object sender, RoutedEventArgs e)
        {
            var features = from CheckBox cBox in gridFeatures.Children where cBox.IsChecked.Value select (string)cBox.Content;
            var categories = from CheckBox cBox in gridCategories.Children where cBox.IsChecked.Value select (string)cBox.Content;
            var authors = from CheckBox cBox in gridAuthors.Children where cBox.IsChecked.Value select (string)cBox.Content;

            analyzer.Data.ClearFilters();
            analyzer.Data.FilterBy(new FeaturesFilter(features));
            analyzer.Data.FilterBy(new CategoriesFilter(categories));
            analyzer.Data.FilterBy(new AuthorsFilter(authors));

            if (checkOnlyDisabledTests.IsChecked.Value)
            {
                analyzer.Data.FilterBy(new OnlyDisabledFilter());
            }

            if (checkOnlyEnabledTests.IsChecked.Value)
            {
                analyzer.Data.FilterBy(new OnlyEnabledFilter());
            }

            gridResults.ItemsSource = analyzer.Data.FilteredInfo;

            var foundTestsCount = analyzer.Data.FilteredInfo.SelectMany(si => si.TestsInfos).Count();

            var filterText = new StringBuilder()
                .AppendFormat("Found {0} tests. Filters:\nFeatures[{1}]\n", foundTestsCount, string.Join(",", features))
                .AppendFormat("Categories[{0}]\n", string.Join(", ", categories))
                .AppendFormat("Authors[{0}]", string.Join(", ", authors));

            textBoxCurrentFilter.Text = filterText.ToString();
        }

        private void FilterOnlyDisabledTests(object sender, RoutedEventArgs e) =>
            checkOnlyEnabledTests.IsChecked = false;

        private void FilterOnlyEnabledTests(object sender, RoutedEventArgs e) =>
            checkOnlyDisabledTests.IsChecked = false;

        private void LoadTestsAssembly(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Unicorn tests assemblies|*.dll"
            };

            openFileDialog.ShowDialog();

            var assemblyFile = openFileDialog.FileName;

            if (string.IsNullOrEmpty(assemblyFile))
            {
                return;
            }

            //TODO groupBoxVisualization.IsEnabled = true;
            //groupBoxVisualizationStateTemp = true;

            gridFilters.IsEnabled = true;
            buttonExportStats.IsEnabled = true;

            analyzer = new Analyzer(assemblyFile, checkboxTestData.IsChecked.Value);
            analyzer.GetTestsStatistics();

            Status = $"Assembly: {analyzer.AssemblyFileName} ({analyzer.TestsAssemblyName})    |    " + analyzer.Data.ToString();
            //TODO statusBarText.Text = Statistics;

            FillFiltersFrom(analyzer.Data);
            ShowAll();
            checkBoxShowHide.IsChecked = true;
        }

        private void FillFiltersFrom(AutomationData data)
        {
            UiUtils.FillGrid(gridFeatures, data.UniqueFeatures);
            UiUtils.FillGrid(gridCategories, data.UniqueCategories);
            UiUtils.FillGrid(gridAuthors, data.UniqueAuthors);
        }

        private void buttonExportStats_Click(object sender, RoutedEventArgs e)
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

                foreach (var suite in analyzer.Data.FilteredInfo)
                {
                    var tags = string.Join("#", suite.Tags).ToLowerInvariant();
                    var suiteName = suite.Name.IndexOfAny(chars) >= 0 ? '\"' + suite.Name.Replace("\"", "\"\"") + '\"' : suite.Name;

                    foreach (var test in suite.TestsInfos)
                    {
                        var disabled = test.Disabled ? "Y" : "N";
                        var categories = string.Join("#", test.Categories).ToLowerInvariant();
                        var testName = test.Name.IndexOfAny(chars) >= 0 ? '\"' + test.Name.Replace("\"", "\"\"") + '\"' : test.Name;

                        csv.AppendLine(string.Join(delimiter, suiteName, testName, test.Author, tags, categories, disabled));
                    }
                }

                File.WriteAllText(saveDialog.FileName, csv.ToString());
            }
        }
    }
}
