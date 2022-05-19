using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public void SetTestsData(Dictionary<string, IEnumerable<TestResult>> data)
        {
            FailedTests = data;
        }
    }
}
