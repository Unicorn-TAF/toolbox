using System.Collections.Generic;
using Unicorn.Toolbox.Models.Launch;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands;

public class OpenFailsByMessageCommand : CommandBase
{
    public override void Execute(object parameter)
    {
        IDialogViewModel failedTestsVm = new FailedTestsViewModel
        {
            FailedTests = parameter as Dictionary<string, IEnumerable<TestResult>>
        };

        DialogHost window = new DialogHost("Failed tests by error message filter")
        {
            DataContext = new DialogHostViewModel(failedTestsVm)
        };

        window.ShowDialog();
    }
}
