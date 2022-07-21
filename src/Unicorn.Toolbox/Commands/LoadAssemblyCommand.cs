using Microsoft.Win32;
using System.Linq;
using Unicorn.Toolbox.Stats;
using Unicorn.Toolbox.Stats.Filtering;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class LoadAssemblyCommand : CommandBase
    {
        private readonly StatsViewModel _viewModel;
        private readonly StatsCollector _statsCollector;

        public LoadAssemblyCommand(StatsViewModel viewModel, StatsCollector statsCollector)
        {
            _viewModel = viewModel;
            _statsCollector = statsCollector;
        }

        public override void Execute(object parameter)
        {
            bool considerData = (bool)parameter;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Unicorn tests assemblies|*.dll"
            };

            if (openFileDialog.ShowDialog().Value)
            {
                string assemblyFile = openFileDialog.FileName;

                _statsCollector.GetTestsStatistics(assemblyFile, considerData);
                _statsCollector.Data.ClearFilters();

                _viewModel.Filters.First(f => f.Filter == FilterType.Tag)
                    .Populate(_statsCollector.Data.UniqueTags);
                
                _viewModel.Filters.First(f => f.Filter == FilterType.Category)
                    .Populate(_statsCollector.Data.UniqueCategories);

                _viewModel.Filters.First(f => f.Filter == FilterType.Author)
                    .Populate(_statsCollector.Data.UniqueAuthors);

                _viewModel.FilterAll = true;

                _viewModel.Status = $"assembly {_statsCollector.AssemblyFile} was loaded >> " +
                    $"{_statsCollector.AssemblyProps}  |  {_statsCollector.Data}";

                _viewModel.DataLoaded = true;

                _viewModel.ApplyFilteredData();
            }
        }
    }
}
