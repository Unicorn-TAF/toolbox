using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Coverage;

namespace Unicorn.Toolbox.ViewModels
{
    public class CoverageViewModel : ViewModelBase
    {
        private readonly SpecsCoverage _coverage;
        private readonly Analyzer _analyzer;
        private bool canGetCoverage;
        private string runTags;
        private readonly ObservableCollection<CoverageModuleViewModel> _modulesList;

        public CoverageViewModel(Analyzer analyzer)
        {
            _coverage = new SpecsCoverage();
            _analyzer = analyzer;
            _modulesList = new ObservableCollection<CoverageModuleViewModel>();
            _modulesList.CollectionChanged += OnCollectionChange;

            LoadSpecsCommand = new LoadSpecsCommand(this, _coverage);
            GetCoverageCommand = new GetCoverageCommand(this, _coverage, _analyzer);
        }

        public ICommand LoadSpecsCommand { get; }

        public ICommand GetCoverageCommand { get; }

        public bool CanGetCoverage
        {
            get => canGetCoverage;

            set
            {
                canGetCoverage = value;
                OnPropertyChanged(nameof(CanGetCoverage));
            }
        }

        public ObservableCollection<CoverageModuleViewModel> ModulesList => _modulesList;

        public string Status { get; set; } = string.Empty;

        public string RunTags
        {
            get => runTags;

            set
            {
                runTags = value;
                OnPropertyChanged(nameof(RunTags));
            }
        }

        public IOrderedEnumerable<KeyValuePair<string, int>> GetVisualizationData()
        {
            Dictionary<string, int> featuresStats = new Dictionary<string, int>();

            foreach (var module in _coverage.Specs.Modules)
            {
                IEnumerable<List<TestInfo>> tests = 
                    from SuiteInfo s
                    in module.Suites
                    select s.TestsInfos;

                featuresStats.Add(module.Name, tests.Sum(t => t.Count));
            }

            IOrderedEnumerable<KeyValuePair<string, int>> items = 
                from pair in featuresStats
                orderby pair.Value descending
                select pair;

            return items;
        }

        private void OnCollectionChange(object sender, NotifyCollectionChangedEventArgs e) =>
            OnPropertyChanged(nameof(ModulesList));
    }
}
