using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

            if (openFileDialog.ShowDialog().Value)
            {
                string specFileName = openFileDialog.FileName;

                _coverage.ReadSpecs(specFileName);
                _viewModel.DataLoaded = true;
                _viewModel.GetCoverageCommand.Execute(null);

                foreach (CoverageModuleViewModel module in _viewModel.ModulesList)
                {
                    module.PropertyChanged += new PropertyChangedEventHandler(OnCheckboxCheck);
                }
            }
        }

        private void OnCheckboxCheck(object sender, PropertyChangedEventArgs e)
        {
            IEnumerable<string> runTags =
                _viewModel.ModulesList
                .Where(m => m.Selected)
                .SelectMany(m => m.Features)
                .Select(f => f.ToLowerInvariant())
                .Distinct();

            _viewModel.RunTags = "#" + string.Join(" #", runTags);
        }
    }
}
