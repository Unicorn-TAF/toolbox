using System.Collections.Generic;
using Unicorn.Toolbox.Models.Launch;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class OpenFailsByMessageCommand : CommandBase
    {
        public override void Execute(object parameter)
        {
            var window = new DialogHost("Failed tests by error message filter")
            {
                DataContext = new DialogHostViewModel(parameter as Dictionary<string, IEnumerable<TestResult>>)
            };

            //window.SetDataSource(FailedResults);
            window.ShowDialog();
        }
    }
}
