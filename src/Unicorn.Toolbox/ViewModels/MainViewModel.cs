using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicorn.Toolbox.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            StatisticsViewModel = new StatisticsViewModel();
        }

        public ViewModelBase StatisticsViewModel { get; }
    }
}
