using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Analysis.Filtering;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class ApplyFilterCommand : CommandBase
    {
        private readonly StatisticsViewModel _viewModel;
        private readonly Analyzer _analyzer;

        public ApplyFilterCommand(StatisticsViewModel viewModel, Analyzer analyzer)
        {
            _viewModel = viewModel;
            _analyzer = analyzer;
        }

        public override void Execute(object parameter)
        {
            _viewModel.PopulateDataFromFilters();

            _analyzer.Data.ClearFilters();
            _analyzer.Data.FilterBy(new FeaturesFilter(_viewModel.Features));
            _analyzer.Data.FilterBy(new CategoriesFilter(_viewModel.Categories));
            _analyzer.Data.FilterBy(new AuthorsFilter(_viewModel.Authors));

            if (_viewModel.FilterOnlyDisabledTests)
            {
                _analyzer.Data.FilterBy(new OnlyDisabledFilter());
            }

            if (_viewModel.FilterOnlyEnabledTests)
            {
                _analyzer.Data.FilterBy(new OnlyEnabledFilter());
            }

            _viewModel.ApplyFilteredData(false);
        }
    }
}
