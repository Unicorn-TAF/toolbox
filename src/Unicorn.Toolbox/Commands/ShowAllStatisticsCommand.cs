using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class ShowAllStatisticsCommand : CommandBase
    {
        private readonly StatisticsViewModel _viewModel;
        private readonly StatsCollector _analyzer;

        public ShowAllStatisticsCommand(StatisticsViewModel viewModel, StatsCollector analyzer)
        {
            _viewModel = viewModel;
            _analyzer = analyzer;
        }

        public override void Execute(object parameter)
        {
            _analyzer.Data.ClearFilters();
            _viewModel.ApplyFilteredData(true);
        }
    }
}
