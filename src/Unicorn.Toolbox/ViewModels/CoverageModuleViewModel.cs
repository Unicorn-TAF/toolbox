using System.Collections.Generic;
using System.Linq;
using Unicorn.Toolbox.Coverage;

namespace Unicorn.Toolbox.ViewModels
{
    public class CoverageModuleViewModel : ViewModelBase
    {
        private readonly CoverageModule _module;
        private bool selected;

        public CoverageModuleViewModel(CoverageModule module)
        {
            _module = module;
            selected = false;
        }

        public string Name => _module.Name;

        public bool Covered => _module.Suites.Any();

        public IEnumerable<string> Features => _module.Features;

        public bool Selected
        {
            get => selected;

            set
            {
                selected = value;
                OnPropertyChanged(nameof(Selected));
            }
        }
    }
}
