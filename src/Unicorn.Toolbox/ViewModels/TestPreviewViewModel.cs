using System.Collections.Generic;
using Unicorn.Toolbox.Analysis;

namespace Unicorn.Toolbox.ViewModels
{
    public class TestPreviewViewModel : ViewModelBase
    {
        public List<TestInfo> TestInfos { get; private set; }

        public TestPreviewViewModel(List<TestInfo> data)
        {
            TestInfos = data;
        }

        public void SetTestsData(List<TestInfo> data)
        {
            TestInfos = data;
        }
    }
}
