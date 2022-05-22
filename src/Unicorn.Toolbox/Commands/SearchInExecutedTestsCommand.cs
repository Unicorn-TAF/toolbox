using System;
using System.Linq;
using Unicorn.Toolbox.LaunchAnalysis;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class SearchInExecutedTestsCommand : CommandBase
    {
        private readonly LaunchResultsViewModel _viewModel;
        private LaunchResult _launchResult;

        public SearchInExecutedTestsCommand(
            LaunchResultsViewModel viewModel, 
            LaunchResult launchResult)
        {
            _viewModel = viewModel;
            _launchResult = launchResult;
        }

        public override void Execute(object parameter)
        {
            if (_viewModel.SearchByMessage)
            {
                ExecutedTestsFilter testsFilter = new ExecutedTestsFilter(_viewModel.FailMessage, _viewModel.Regex);
                testsFilter.FilterTestsByFailMessage(_launchResult.Executions.SelectMany(exec => exec.TestResults));

                _viewModel.Filter = testsFilter;
            }
            else
            {
                ExecutedTestsFilter testsFilter = new ExecutedTestsFilter(_viewModel.FailMessage);
                testsFilter.FilterTestsByTime(_launchResult.Executions.SelectMany(exec => exec.TestResults));

                _viewModel.Filter = testsFilter;
            }

            _viewModel.UpdateFilteredTestsCount();
        }
    }
}
