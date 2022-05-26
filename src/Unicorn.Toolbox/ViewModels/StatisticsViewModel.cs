using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.Models.Stats.Filtering;

namespace Unicorn.Toolbox.ViewModels
{
    public class StatsViewModel : ViewModelBase
    {
        private readonly StatsCollector _statsCollector;
        private readonly MainWindow _window;
        private bool considerTestData;
        private bool filterOnlyDisabledTests;
        private bool filterOnlyEnabledTests; 
        private bool showHideAllCheckboxes;

        public StatsViewModel(StatsCollector statsCollector)
        {
            _window = App.Current.MainWindow as MainWindow;
            _statsCollector = statsCollector;
            LoadTestsAssemblyCommand = new LoadTestsAssemblyCommand(this, _statsCollector);
            ApplyFilterCommand = new ApplyFilterCommand(this, _statsCollector);
            ExportStatisticsCommand = new ExportStatisticsCommand(_statsCollector);
            ShowAllStatisticsCommand = new ShowAllStatisticsCommand(this, _statsCollector);
            OpenSuiteDetailsCommand = new OpenSuiteDetailsCommand(this, _statsCollector);
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

        public FilterType CurrentFilter
        {
            get
            {
                if (_window.StatsView.tabFeaures.IsSelected)
                {
                    return FilterType.Tag;
                }
                else if (_window.StatsView.tabCategories.IsSelected)
                {
                    return FilterType.Category;
                }
                else
                {
                    return FilterType.Author;
                }
            }
        }

        public string Status { get; set; } = string.Empty;

        public IEnumerable<string> Features { get; set; }
        
        public IEnumerable<string> Categories { get; set; }
        
        public IEnumerable<string> Authors { get; set; }

        public void UpdateViewModel()
        {
            _window.VisualizationView.groupBoxVisualization.IsEnabled = true;
            //groupBoxVisualizationStateTemp = true;

            _window.StatsView.gridFilters.IsEnabled = true;
            _window.StatsView.buttonExportStats.IsEnabled = true;

            Status = $"Assembly: {_statsCollector.AssemblyFile} ({_statsCollector.AssemblyName})    |    {_statsCollector.Data}";
            _window.statusBarText.Text = Status;

            FillFiltersFrom(_statsCollector.Data);
            ShowAll();
            _window.StatsView.checkBoxShowHide.IsChecked = true;
        }

        public void ApplyFilteredData(bool emptyText)
        {
            _window.StatsView.gridResults.ItemsSource = _statsCollector.Data.FilteredInfo;

            var foundTestsCount = _statsCollector.Data.FilteredInfo.SelectMany(si => si.TestsInfos).Count();

            var filterText = new StringBuilder()
                .AppendFormat("Found {0} tests. Filters:\nFeatures[{1}]\n", foundTestsCount, string.Join(",", Features))
                .AppendFormat("Categories[{0}]\n", string.Join(", ", Categories))
                .AppendFormat("Authors[{0}]", string.Join(", ", Authors));

            _window.StatsView.textBoxCurrentFilter.Text = emptyText ? string.Empty : filterText.ToString();
        }

        private void ShowAll()
        {
            _statsCollector.Data.ClearFilters();
            _window.StatsView.gridResults.ItemsSource = _statsCollector.Data.FilteredInfo;
            _window.StatsView.textBoxCurrentFilter.Text = string.Empty;
        }

        private void FillFiltersFrom(AutomationData data)
        {
            UiUtils.FillGrid(_window.StatsView.gridFeatures, data.UniqueFeatures);
            UiUtils.FillGrid(_window.StatsView.gridCategories, data.UniqueCategories);
            UiUtils.FillGrid(_window.StatsView.gridAuthors, data.UniqueAuthors);
        }

        public void PopulateDataFromFilters()
        {
            Features = GetCheckedCheckboxesNames(_window.StatsView.gridFeatures);
            Categories = GetCheckedCheckboxesNames(_window.StatsView.gridCategories);
            Authors = GetCheckedCheckboxesNames(_window.StatsView.gridAuthors);
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
            if (_window.StatsView.tabFeaures.IsSelected)
            {
                return _window.StatsView.gridFeatures;
            }
            else if (_window.StatsView.tabCategories.IsSelected)
            {
                return _window.StatsView.gridCategories;
            }
            else
            {
                return _window.StatsView.gridAuthors;
            }
        }

        public IOrderedEnumerable<KeyValuePair<string, int>> GetVisualizationData()
        {
            var data = _statsCollector.Data;
            var stats = new Dictionary<string, int>();

            switch (CurrentFilter)
            {
                case FilterType.Tag:
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
