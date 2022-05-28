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
        private bool filterOnlyDisabledTests;
        private bool filterOnlyEnabledTests; 
        private bool showHideAllCheckboxes;
        private string currentFilterText;
        private bool dataLoaded;

        private bool tagsFilterActive;
        private bool categoriesFilterActive;
        private bool authorsFilterActive;

        public StatsViewModel(StatsCollector statsCollector)
        {
            _statsCollector = statsCollector;
            LoadTestsAssemblyCommand = new LoadTestsAssemblyCommand(this, _statsCollector);
            ApplyFilterCommand = new ApplyFilterCommand(this, _statsCollector);
            ExportStatisticsCommand = new ExportStatisticsCommand(_statsCollector);
            OpenSuiteDetailsCommand = new OpenSuiteDetailsCommand(this, _statsCollector);
            DataLoaded = false;
            TagsFilterActive = true;
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

        public FilterType CurrentFilter { get; private set; }

        public string CurrentFilterText
        {
            get => currentFilterText;

            set
            {
                currentFilterText = value;
                OnPropertyChanged(nameof(CurrentFilterText));
            }
        }

        public string Status { get; set; } = string.Empty;

        public bool TagsFilterActive
        {
            get => tagsFilterActive;

            set
            {
                tagsFilterActive = value;
                OnPropertyChanged(nameof(TagsFilterActive));

                if (value)
                {
                    CurrentFilter = FilterType.Tag;
                }
            }
        }

        public bool CategoriesFilterActive
        {
            get => categoriesFilterActive;

            set
            {
                categoriesFilterActive = value;
                OnPropertyChanged(nameof(CategoriesFilterActive));

                if (value)
                {
                    CurrentFilter = FilterType.Category;
                }
            }
        }

        public bool AuthorsFilterActive
        {
            get => authorsFilterActive;

            set
            {
                authorsFilterActive = value;
                OnPropertyChanged(nameof(AuthorsFilterActive));

                if (value)
                {
                    CurrentFilter = FilterType.Author;
                }
            }
        }

        public IEnumerable<FilterItemViewModel> Tags { get; set; }
        
        public IEnumerable<FilterItemViewModel> Categories { get; set; }
        
        public IEnumerable<FilterItemViewModel> Authors { get; set; }

        public IEnumerable<SuiteInfo> FilteredInfo => _statsCollector.Data?.FilteredInfo;

        public ICommand LoadTestsAssemblyCommand { get; }

        public ICommand ApplyFilterCommand { get; }

        public ICommand ExportStatisticsCommand { get; }

        public ICommand OpenSuiteDetailsCommand { get; }

        public void UpdateViewModel()
        {
            Status = $"Assembly: {_statsCollector.AssemblyFile} ({_statsCollector.AssemblyName})    |    {_statsCollector.Data}";

            FillFiltersFrom(_statsCollector.Data);

            _statsCollector.Data.ClearFilters();
            ApplyFilteredData();

            ShowHideAllCheckboxes = true;
        }

        public void ApplyFilteredData()
        {
            OnPropertyChanged(nameof(FilteredInfo));

            string filterText = string.Empty;

            if (_statsCollector.Data.FilteredInfo.Count() != _statsCollector.Data.SuitesInfos.Count())
            {
                var foundTestsCount = _statsCollector.Data.FilteredInfo.SelectMany(si => si.TestsInfos).Count();

                filterText = new StringBuilder()
                    .AppendFormat("Found {0} tests. Filters:\n", foundTestsCount)
                    .AppendFormat("Tags: {0}\n", string.Join(", ", Tags.Where(t => t.Selected).Select(t => t.Name)))
                    .AppendFormat("Categories: {0}\n", string.Join(", ", Categories.Where(c => c.Selected).Select(c => c.Name)))
                    .AppendFormat("Authors: {0}", string.Join(", ", Authors.Where(a => a.Selected).Select(a => a.Name)))
                    .ToString(); ;
            }

            CurrentFilterText = filterText;
        }

        private void FillFiltersFrom(AutomationData data)
        {
            Tags = new List<FilterItemViewModel>(data.UniqueFeatures.Select(f => new FilterItemViewModel(f)))
                .OrderBy(f => f.Name);

            Categories = new List<FilterItemViewModel>(data.UniqueCategories.Select(c => new FilterItemViewModel(c)))
                .OrderBy(c => c.Name);

            Authors = new List<FilterItemViewModel>(data.UniqueAuthors.Select(a => new FilterItemViewModel(a)))
                .OrderBy(a => a.Name);

            OnPropertyChanged(nameof(Tags));
            OnPropertyChanged(nameof(Categories));
            OnPropertyChanged(nameof(Authors));
        }

        private void SetCheckboxesCheckedState(bool isChecked)
        {
            IEnumerable<FilterItemViewModel> list;

            if (TagsFilterActive)
            {
                list = Tags;
            }
            else if (CategoriesFilterActive)
            {
                list = Categories;
            }
            else
            {
                list = Authors;
            }

            foreach (FilterItemViewModel item in list)
            {
                item.Selected = isChecked;
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
