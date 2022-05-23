using System.Collections.Generic;
using Unicorn.Toolbox.LaunchAnalysis;

namespace Unicorn.Toolbox.ViewModels
{
    public class FailedTestsViewModel : ViewModelBase
    {
        public Dictionary<string, IEnumerable<TestResult>> FailedTests { get; private set; }

        public FailedTestsViewModel(Dictionary<string, IEnumerable<TestResult>> data)
        {
            FailedTests = data;
        }
    }
}
