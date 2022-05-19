using System.Collections.Generic;
using Unicorn.Toolbox.Analysis;

namespace Unicorn.Toolbox.ViewModels
{
    public class SuiteDetailsViewModel : ViewModelBase
    {
        public List<TestInfo> TestInfos { get; private set; }

        public SuiteDetailsViewModel(List<TestInfo> data)
        {
            TestInfos = data;
        }

        public void SetTestsData(List<TestInfo> data)
        {
            TestInfos = data;
        }
    }
}
