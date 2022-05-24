using System.ComponentModel;
using System.Linq;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class GetCoverageCommand : CommandBase
    {
        private readonly CoverageViewModel _viewModel;
        private readonly SpecsCoverage _coverage;
        private readonly Analyzer _analyzer;

        public GetCoverageCommand(CoverageViewModel viewModel, SpecsCoverage coverage, Analyzer analyzer)
        {
            _viewModel = viewModel;
            _coverage = coverage;
            _analyzer = analyzer;
            _viewModel.PropertyChanged += OnViewPropertyChanged;
        }

        public override void Execute(object parameter)
        {
            _coverage.Analyze(_analyzer.Data.FilteredInfo);
            _viewModel.ModulesList.Clear();
            
            foreach (var module in _coverage.Specs.Modules.Distinct())
            {
                _viewModel.ModulesList.Add(new CoverageModuleViewModel(module));
            }
        }

        public override bool CanExecute(object parameter) =>
            _viewModel.CanGetCoverage && base.CanExecute(parameter);

        private void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e) 
        { 
            if (e.PropertyName == nameof(CoverageViewModel.CanGetCoverage))
            {
                OnCanExecutedChanged();
            }
        }
    }
}
