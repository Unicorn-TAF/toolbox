using System.Collections.Generic;
using Unicorn.Toolbox.Models.Launch;

namespace Unicorn.Toolbox.ViewModels;

public class FailedTestsViewModel : IDialogViewModel
{
    public Dictionary<string, IEnumerable<TestResult>> FailedTests { get; set; }
}
