using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class OpenSuiteDetailsCommand : CommandBase
    {
        private readonly StatisticsViewModel _viewModel;
        private readonly Analyzer _analyzer;

        public OpenSuiteDetailsCommand(StatisticsViewModel viewModel, Analyzer analyzer)
        {
            _viewModel = viewModel;
            _analyzer = analyzer;
        }

        public override void Execute(object parameter)
        {
            throw new NotImplementedException();
        }
    }
}
