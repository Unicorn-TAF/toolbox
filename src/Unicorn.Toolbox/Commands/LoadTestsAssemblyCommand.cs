using Microsoft.Win32;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class LoadTestsAssemblyCommand : CommandBase
    {
        private readonly StatsViewModel _viewModel;
        private readonly StatsCollector _statsCollector;

        public LoadTestsAssemblyCommand(StatsViewModel viewModel, StatsCollector statsCollector)
        {
            _viewModel = viewModel;
            _statsCollector = statsCollector;
        }

        public override void Execute(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Unicorn tests assemblies|*.dll"
            };

            openFileDialog.ShowDialog();

            var assemblyFile = openFileDialog.FileName;

            if (string.IsNullOrEmpty(assemblyFile))
            {
                return;
            }

            _statsCollector.GetTestsStatistics(assemblyFile, _viewModel.ConsiderTestData);
            _viewModel.DataLoaded = true;
            _viewModel.UpdateViewModel();
        }
    }
}
