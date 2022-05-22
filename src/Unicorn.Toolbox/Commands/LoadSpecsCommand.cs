using Microsoft.Win32;
using Unicorn.Toolbox.Coverage;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class LoadSpecsCommand : CommandBase
    {
        private readonly CoverageViewModel _viewModel;
        private readonly SpecsCoverage _coverage;

        public LoadSpecsCommand(CoverageViewModel viewModel, SpecsCoverage coverage)
        {
            _viewModel = viewModel;
            _coverage = coverage;
        }

        public override void Execute(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Application specs|*.json"
            };

            openFileDialog.ShowDialog();

            string specFileName = openFileDialog.FileName;

            if (string.IsNullOrEmpty(specFileName))
            {
                return;
            }

            _coverage.ReadSpecs(specFileName);
            _viewModel.UpdateModel();
        }
    }
}
