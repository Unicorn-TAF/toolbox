using System.Linq;
using Unicorn.Toolbox.Stats;
using Unicorn.Toolbox.Stats.Filtering;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands;

public class ApplyFilterCommand : CommandBase
{
    private readonly StatsViewModel _viewModel;
    private readonly StatsCollector _analyzer;

    public ApplyFilterCommand(StatsViewModel viewModel, StatsCollector analyzer)
    {
        _viewModel = viewModel;
        _analyzer = analyzer;
    }

    public override void Execute(object parameter)
    {
        _analyzer.Data.ClearFilters();
        _analyzer.Data.FilterBy(new TagsFilter(_viewModel.Filters.First(f => f.Filter == FilterType.Tag).SelectedValues));
        _analyzer.Data.FilterBy(new CategoriesFilter(_viewModel.Filters.First(f => f.Filter == FilterType.Category).SelectedValues));
        _analyzer.Data.FilterBy(new AuthorsFilter(_viewModel.Filters.First(f => f.Filter == FilterType.Author).SelectedValues));

        if (_viewModel.FilterOnlyDisabledTests)
        {
            _analyzer.Data.FilterBy(new OnlyDisabledFilter());
        }

        if (_viewModel.FilterOnlyEnabledTests)
        {
            _analyzer.Data.FilterBy(new OnlyEnabledFilter());
        }

        _viewModel.ApplyFilteredData();
    }
}
