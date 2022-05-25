using Microsoft.Win32;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class LoadTestsAssemblyCommand : CommandBase
    {
        private readonly StatisticsViewModel _viewModel;
        private readonly StatsCollector _analyzer;

        public LoadTestsAssemblyCommand(StatisticsViewModel viewModel, StatsCollector analyzer)
        {
            _viewModel = viewModel;
            _analyzer = analyzer;
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

            _analyzer.GetTestsStatistics(assemblyFile, _viewModel.ConsiderTestData);
            _viewModel.UpdateModel();
        }
    }
}
