using System.Linq;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class OpenSuiteDetailsCommand : CommandBase
    {
        private readonly StatsCollector _analyzer;

        public OpenSuiteDetailsCommand(StatsCollector analyzer)
        {
            _analyzer = analyzer;
        }

        public override void Execute(object parameter)
        {
            string testSuiteName = parameter.ToString();

            ViewModelBase suiteDetailsVm = new SuiteDetailsViewModel
            {
                TestInfos = _analyzer.Data.FilteredInfo.First(s => s.Name.Equals(testSuiteName)).TestsInfos
            };

            DialogHost window = new DialogHost("Suite preview: " + testSuiteName)
            {
                DataContext = new DialogHostViewModel(suiteDetailsVm),
                ShowActivated = false
            };

            window.ShowDialog();
        }
    }
}
