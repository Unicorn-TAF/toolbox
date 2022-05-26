using System.Linq;
using Unicorn.Toolbox.Models.Stats;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class OpenSuiteDetailsCommand : CommandBase
    {
        private readonly StatsViewModel _viewModel;
        private readonly StatsCollector _analyzer;

        public OpenSuiteDetailsCommand(StatsViewModel viewModel, StatsCollector analyzer)
        {
            _viewModel = viewModel;
            _analyzer = analyzer;
        }

        public override void Execute(object parameter)
        {
            string testSuiteName = parameter.ToString();

            var window = new DialogHost("Suite preview: " + testSuiteName)
            {
                DataContext = new DialogHostViewModel(
                    _analyzer.Data.FilteredInfo.First(s => s.Name.Equals(testSuiteName)).TestsInfos),
                ShowActivated = false
            };

            //window.SetDataSource(FailedResults);
            window.Show();
        }
    }
}
