using System.Collections.Generic;
using Unicorn.Toolbox.Stats;

namespace Unicorn.Toolbox.ViewModels;

public class SuiteDetailsViewModel : IDialogViewModel
{
    public List<TestInfo> TestInfos { get; set; }
}
