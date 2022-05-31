using System.Collections.Generic;
using System.Linq;
using Unicorn.Toolbox.Models.Launch;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class SearchInExecutedTestsCommand : CommandBase
    {
        private readonly LaunchViewModel _viewModel;
        private readonly LaunchResult _launchResult;

        public SearchInExecutedTestsCommand(
            LaunchViewModel viewModel, 
            LaunchResult launchResult)
        {
            _viewModel = viewModel;
            _launchResult = launchResult;
        }

        public override void Execute(object parameter)
        {
            ExecutedTestsFilter testsFilter = new ExecutedTestsFilter();
            IEnumerable<TestResult> results = _launchResult.Executions.SelectMany(exec => exec.TestResults);

            switch (_viewModel.FilterFailsBy)
            {
                case FailsFilter.ErrorMessage:
                    testsFilter.FilterByFailMessage(results, _viewModel.FailSearchCriteria, false);
                    break;
                case FailsFilter.ErrorMessageRegex:
                    testsFilter.FilterByFailMessage(results, _viewModel.FailSearchCriteria, true);
                    break;
                case FailsFilter.Time:
                    testsFilter.FilterByTime(results, _viewModel.FailSearchCriteria);
                    break;
            }

            _viewModel.Filter = testsFilter;

            var window = new DialogHost($"Failed tests by error message filter ({_viewModel.Filter.MatchingTestsCount})")
            {
                DataContext = new DialogHostViewModel(_viewModel.Filter.FilteredData)
            };
            //window.SetFailedTestsDataSource(failedTestsFilter.FilteredResults);
            window.ShowDialog();
        }
    }
}
