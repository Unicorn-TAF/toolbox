using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.Models.Stats.Filtering;

namespace Unicorn.Toolbox.ViewModels
{
    public class StatsViewModel : ViewModelBase
    {
        private readonly StatsCollector _statsCollector;
        private bool considerTestData;
        private bool dataLoaded;
        private bool filterDisabledTestsOnly;
        private bool filterEnabledTestsOnly; 
        private bool filterAll;
        private string foundTestsCount;
        private string currentFilterQuery;
        private StatsFilterViewModel currentFilter;

        public StatsViewModel(StatsCollector statsCollector)
        {
            _statsCollector = statsCollector;
            LoadTestsAssemblyCommand = new LoadTestsAssemblyCommand(this, _statsCollector);
            ApplyFilterCommand = new ApplyFilterCommand(this, _statsCollector);
            ExportStatsCommand = new ExportStatsCommand(_statsCollector);
            OpenSuiteDetailsCommand = new OpenSuiteDetailsCommand(_statsCollector);
            DataLoaded = false;

            Filters = new List<StatsFilterViewModel>
            {
                new StatsFilterViewModel(FilterType.Tag),
                new StatsFilterViewModel(FilterType.Category),
                new StatsFilterViewModel(FilterType.Author)
            };

            CurrentFilter = Filters.ElementAt(0);
        }

        public bool DataLoaded
        {
            get => dataLoaded;

            set
            {
                dataLoaded = value;
                OnPropertyChanged(nameof(DataLoaded));
            }
        }

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
            get =>  filterDisabledTestsOnly;
            
            set
            {
                filterDisabledTestsOnly = value;
                OnPropertyChanged(nameof(FilterOnlyDisabledTests));

                if (filterDisabledTestsOnly)
                {
                    FilterOnlyEnabledTests = false;
                }
            }
        }

        public bool FilterOnlyEnabledTests
        {
            get => filterEnabledTestsOnly;
            
            set
            {
                filterEnabledTestsOnly = value;
                OnPropertyChanged(nameof(FilterOnlyEnabledTests));

                if (filterEnabledTestsOnly)
                {
                    FilterOnlyDisabledTests = false;
                }
            }
        }

        public bool FilterAll
        {
            get => filterAll;

            set
            {
                filterAll = value;
                OnPropertyChanged(nameof(FilterAll));
                SetCheckboxesCheckedState(filterAll);
            }
        }

        public string FoundTestsCount
        {
            get => foundTestsCount;

            set
            {
                foundTestsCount = value;
                OnPropertyChanged(nameof(FoundTestsCount));
            }
        }

        public StatsFilterViewModel CurrentFilter
        {
            get => currentFilter;

            set
            {
                currentFilter = value;
                OnPropertyChanged(nameof(CurrentFilter));
            }
        }

        public string CurrentFilterQuery
        {
            get => currentFilterQuery;

            set
            {
                currentFilterQuery = value;
                OnPropertyChanged(nameof(CurrentFilterQuery));
            }
        }

        public string Status { get; set; } = string.Empty;

        public IEnumerable<StatsFilterViewModel> Filters { get; set; }

        public IEnumerable<SuiteInfo> FilteredInfo => _statsCollector.Data?.FilteredInfo;

        public ICommand LoadTestsAssemblyCommand { get; }

        public ICommand ApplyFilterCommand { get; }

        public ICommand ExportStatsCommand { get; }

        public ICommand OpenSuiteDetailsCommand { get; }

        public void ApplyFilteredData()
        {
            OnPropertyChanged(nameof(FilteredInfo));

            string filterText = string.Empty;

            if (_statsCollector.Data.FilteredInfo.Count() != _statsCollector.Data.SuitesInfos.Count())
            {
                var foundTestsCount = _statsCollector.Data.FilteredInfo.SelectMany(si => si.TestsInfos).Count();
                FoundTestsCount = $"Found {foundTestsCount} tests";

                filterText = new StringBuilder()
                    .AppendFormat("Categories: {0}\n", string.Join(", ", Filters.First(f => f.Filter == FilterType.Category).SelectedValues))
                    .AppendFormat("Tags: {0}\n", string.Join(", ", Filters.First(f => f.Filter == FilterType.Tag).SelectedValues))
                    .AppendFormat("Authors: {0}", string.Join(", ", Filters.First(f => f.Filter == FilterType.Author).SelectedValues))
                    .ToString();
            }

            CurrentFilterQuery = filterText;
        }

        private void SetCheckboxesCheckedState(bool isChecked)
        {
            foreach (FilterItemViewModel item in CurrentFilter.Values)
            {
                item.Selected = isChecked;
            }
        }

        public IOrderedEnumerable<KeyValuePair<string, int>> GetVisualizationData()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            switch (CurrentFilter.Filter)
            {
                case FilterType.Tag:
                    stats = GetDataByTags();
                    break;

                case FilterType.Category:
                    stats = GetDataByCategories();
                    break;

                case FilterType.Author:
                    stats = GetDataByAuthors();
                    break;
            }

            var items = from pair in stats
                        orderby pair.Value descending
                        select pair;

            return items;
        }

        private Dictionary<string, int> GetDataByTags()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (string feature in _statsCollector.Data.UniqueTags)
            {
                var suites = _statsCollector.Data.FilteredInfo.Where(s => s.Tags.Contains(feature));
                var tests = from SuiteInfo s
                            in suites
                            select s.TestsInfos;

                stats.Add(feature, tests.Sum(t => t.Count));
            }

            return stats;
        }

        private Dictionary<string, int> GetDataByCategories()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (string category in _statsCollector.Data.UniqueCategories)
            {
                var tests = from SuiteInfo s
                            in _statsCollector.Data.FilteredInfo
                            select s.TestsInfos.Where(ti => ti.Categories.Contains(category));

                stats.Add(category, tests.Sum(t => t.Count()));
            }

            return stats;
        }

        private Dictionary<string, int> GetDataByAuthors()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (string author in _statsCollector.Data.UniqueAuthors)
            {
                var tests = from SuiteInfo s
                            in _statsCollector.Data.FilteredInfo
                            select s.TestsInfos.Where(ti => ti.Author.Equals(author));

                stats.Add(author, tests.Sum(t => t.Count()));
            }

            return stats;
        }
    }
}
