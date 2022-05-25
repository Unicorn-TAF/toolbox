using System.Collections.Generic;
using Unicorn.Toolbox.Models.Stats;

namespace Unicorn.Toolbox.ViewModels
{
    public class SuiteDetailsViewModel : ViewModelBase
    {
        public List<TestInfo> TestInfos { get; private set; }

        public SuiteDetailsViewModel(List<TestInfo> data)
        {
            TestInfos = data;
        }
    }
}
