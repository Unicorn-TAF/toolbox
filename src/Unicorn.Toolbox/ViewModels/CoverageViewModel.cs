using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Models.Coverage;
using Unicorn.Toolbox.Stats;

namespace Unicorn.Toolbox.ViewModels
{
    public class CoverageViewModel : FunctionalityViewModelBase
    {
        private readonly SpecsCoverage _coverage;
        private readonly StatsCollector _statsCollector;
        private readonly ObservableCollection<CoverageModuleViewModel> _modulesList;
        
        private string runTags;

        public CoverageViewModel(StatsCollector statsCollector)
        {
            _coverage = new SpecsCoverage();
            _statsCollector = statsCollector;
            _modulesList = new ObservableCollection<CoverageModuleViewModel>();
            _modulesList.CollectionChanged += OnCollectionChange;

            LoadSpecsCommand = new LoadSpecsCommand(this, _coverage);
            GetCoverageCommand = new GetCoverageCommand(this, _coverage, _statsCollector);
        }

        public ObservableCollection<CoverageModuleViewModel> ModulesList => _modulesList;

        public string RunTags
        {
            get => runTags;

            set
            {
                runTags = value;
                OnPropertyChanged(nameof(RunTags));
            }
        }

        public override bool CanCustomizeVisualization { get; } = true;

        public ICommand GetCoverageCommand { get; }

        public ICommand LoadSpecsCommand { get; }

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
