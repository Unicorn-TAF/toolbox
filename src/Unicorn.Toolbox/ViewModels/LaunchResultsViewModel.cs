using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicorn.Toolbox.LaunchAnalysis;

namespace Unicorn.Toolbox.ViewModels
{
    public class LaunchResultsViewModel : ViewModelBase
    {
        private readonly LaunchResult _launchResult;

        public LaunchResultsViewModel(LaunchResult launchResult)
        {
            _launchResult = launchResult;
        }
    }
}
