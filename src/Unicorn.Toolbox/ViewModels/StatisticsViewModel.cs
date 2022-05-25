using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Analysis.Filtering;
using Unicorn.Toolbox.Commands;

namespace Unicorn.Toolbox.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        private readonly Analyzer _analyzer;
        private readonly MainWindow _window;
        private bool considerTestData;
        private bool filterOnlyDisabledTests;
        private bool filterOnlyEnabledTests; 
        private bool showHideAllCheckboxes;

        public StatisticsViewModel(Analyzer analyzer)
        {
            _window = App.Current.MainWindow as MainWindow;
            _analyzer = analyzer;
            LoadTestsAssemblyCommand = new LoadTestsAssemblyCommand(this, _analyzer);
            ApplyFilterCommand = new ApplyFilterCommand(this, _analyzer);
            ExportStatisticsCommand = new ExportStatisticsCommand(_analyzer);
            ShowAllStatisticsCommand = new ShowAllStatisticsCommand(this, _analyzer);
            OpenSuiteDetailsCommand = new OpenSuiteDetailsCommand(this, _analyzer);
        }

        public ICommand LoadTestsAssemblyCommand { get; }

        public ICommand ApplyFilterCommand { get; }

        public ICommand ExportStatisticsCommand { get; }

        public ICommand ShowAllStatisticsCommand { get; }

        public ICommand OpenSuiteDetailsCommand { get; }

        public bool ConsiderTestData
        {
            get => considerTestData;
            
            set
            {
                considerTestData = value;
                OnPropertyChanged(nameof(ConsiderTestData));
            }
        }

        public bool FilterOnlyDisabledTests
        {
            get =>  filterOnlyDisabledTests;
            
            set
            {
                filterOnlyDisabledTests = value;
                OnPropertyChanged(nameof(FilterOnlyDisabledTests));

                if (filterOnlyDisabledTests)
                {
                    FilterOnlyEnabledTests = false;
                }
            }
        }

        public bool FilterOnlyEnabledTests
        {
            get => filterOnlyEnabledTests;
            
            set
            {
                filterOnlyEnabledTests = value;
                OnPropertyChanged(nameof(FilterOnlyEnabledTests));

                if (filterOnlyEnabledTests)
                {
                    FilterOnlyDisabledTests = false;
                }
            }
        }

        public bool ShowHideAllCheckboxes
        {
            get => showHideAllCheckboxes;

            set
            {
                showHideAllCheckboxes = value;
                OnPropertyChanged(nameof(ShowHideAllCheckboxes));
                SetCheckboxesCheckedState(showHideAllCheckboxes);
            }
        }

        public string Status { get; set; } = string.Empty;

        public IEnumerable<string> Features { get; set; }
        
        public IEnumerable<string> Categories { get; set; }
        
        public IEnumerable<string> Authors { get; set; }


        public void UpdateModel()
        {
            _window.groupBoxVisualization.IsEnabled = true;
            //groupBoxVisualizationStateTemp = true;

            _window.StatisticsView.gridFilters.IsEnabled = true;
            _window.StatisticsView.buttonExportStats.IsEnabled = true;

            Status = $"Assembly: {_analyzer.AssemblyFile} ({_analyzer.AssemblyName})    |    {_analyzer.Data}";
            _window.statusBarText.Text = Status;

            FillFiltersFrom(_analyzer.Data);
            ShowAll();
            _window.StatisticsView.checkBoxShowHide.IsChecked = true;
        }

        public void ApplyFilteredData(bool emptyText)
        {
            _window.StatisticsView.gridResults.ItemsSource = _analyzer.Data.FilteredInfo;

            var foundTestsCount = _analyzer.Data.FilteredInfo.SelectMany(si => si.TestsInfos).Count();

            var filterText = new StringBuilder()
                .AppendFormat("Found {0} tests. Filters:\nFeatures[{1}]\n", foundTestsCount, string.Join(",", Features))
                .AppendFormat("Categories[{0}]\n", string.Join(", ", Categories))
                .AppendFormat("Authors[{0}]", string.Join(", ", Authors));

            _window.StatisticsView.textBoxCurrentFilter.Text = emptyText ? string.Empty : filterText.ToString();
        }

        private void ShowAll()
        {
            _analyzer.Data.ClearFilters();
            _window.StatisticsView.gridResults.ItemsSource = _analyzer.Data.FilteredInfo;
            _window.StatisticsView.textBoxCurrentFilter.Text = string.Empty;
        }

        private void FillFiltersFrom(AutomationData data)
        {
            UiUtils.FillGrid(_window.StatisticsView.gridFeatures, data.UniqueFeatures);
            UiUtils.FillGrid(_window.StatisticsView.gridCategories, data.UniqueCategories);
            UiUtils.FillGrid(_window.StatisticsView.gridAuthors, data.UniqueAuthors);
        }

        public void PopulateDataFromFilters()
        {
            Features = GetCheckedCheckboxesNames(_window.StatisticsView.gridFeatures);
            Categories = GetCheckedCheckboxesNames(_window.StatisticsView.gridCategories);
            Authors = GetCheckedCheckboxesNames(_window.StatisticsView.gridAuthors);
        }

        private IEnumerable<string> GetCheckedCheckboxesNames(Grid grid) =>
            from CheckBox cBox in grid.Children where cBox.IsChecked.Value select (string)cBox.Content;

        private void SetCheckboxesCheckedState(bool isChecked)
        {
            foreach (var child in GetActiveGrid().Children)
            {
                ((CheckBox)child).IsChecked = isChecked;
            }
        }

        private Grid GetActiveGrid()
        {
            if (_window.StatisticsView.tabFeaures.IsSelected)
            {
                return _window.StatisticsView.gridFeatures;
            }
            else if (_window.StatisticsView.tabCategories.IsSelected)
            {
                return _window.StatisticsView.gridCategories;
            }
            else
            {
                return _window.StatisticsView.gridAuthors;
            }
        }

        public IOrderedEnumerable<KeyValuePair<string, int>> GetVisualizationData(FilterType filterType)
        {
            var data = _analyzer.Data;
            var stats = new Dictionary<string, int>();

            switch (filterType)
            {
                case FilterType.Feature:
                    {
                        foreach (var feature in data.UniqueFeatures)
                        {
                            var suites = data.FilteredInfo.Where(s => s.Tags.Contains(feature));
                            var tests = from SuiteInfo s
                                        in suites
                                        select s.TestsInfos;

                            stats.Add(feature, tests.Sum(t => t.Count));
                        }
                        break;
                    }

                case FilterType.Category:
                    {
                        foreach (var category in data.UniqueCategories)
                        {
                            var tests = from SuiteInfo s
                                        in data.FilteredInfo
                                        select s.TestsInfos.Where(ti => ti.Categories.Contains(category));

                            stats.Add(category, tests.Sum(t => t.Count()));
                        }

                        break;
                    }

                case FilterType.Author:
                    {
                        foreach (var author in data.UniqueAuthors)
                        {
                            var tests = from SuiteInfo s
                                        in data.FilteredInfo
                                        select s.TestsInfos.Where(ti => ti.Author.Equals(author));

                            stats.Add(author, tests.Sum(t => t.Count()));
                        }

                        break;
                    }
            }

            var items = from pair in stats
                        orderby pair.Value descending
                        select pair;

            return items;
        }
    }
}
