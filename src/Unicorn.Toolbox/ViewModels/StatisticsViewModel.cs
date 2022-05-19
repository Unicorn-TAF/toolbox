using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicorn.Toolbox.Analysis;
using Unicorn.Toolbox.Commands;

namespace Unicorn.Toolbox.ViewModels
{
    public class StatisticsViewModel : ViewModelBase
    {
        private readonly Analyzer _analyzer;

        public StatisticsViewModel(Analyzer analyzer)
        {
            _analyzer = analyzer;
            OpenSuiteDetailsCommand = new OpenSuiteDetailsCommand(this, _analyzer);
        }

        public ICommand OpenSuiteDetailsCommand { get; }
    }
}
