using System.Collections.Generic;
using Unicorn.Toolbox.Models.Launch;

namespace Unicorn.Toolbox.ViewModels
{
    public class FailedTestsViewModel : ViewModelBase
    {
        public Dictionary<string, IEnumerable<TestResult>> FailedTests { get; set; }
    }
}
