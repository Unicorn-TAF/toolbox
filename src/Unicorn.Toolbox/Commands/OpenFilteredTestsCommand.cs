using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class OpenFilteredTestsCommand : CommandBase
    {
        private readonly LaunchResultsViewModel _viewModel;

        public OpenFilteredTestsCommand(LaunchResultsViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public override void Execute(object parameter)
        {
            var window = new DialogHost("Failed tests by error message filter")
            {
                DataContext = new DialogHostViewModel(_viewModel.Filter.FilteredResults)
            };
            //window.SetFailedTestsDataSource(failedTestsFilter.FilteredResults);
            window.ShowDialog();
        }
    }
}
