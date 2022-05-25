using System.Collections.Generic;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Visualization;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.ViewModels
{
    public class VisualizationViewModel : ViewModelBase
    {
        private IPalette currentPalette;
        private bool fullscreen;
        private bool circles;
        private bool available;
        private bool canCustomize;

        public VisualizationViewModel()
        {
            CurrentPalette = new LightGreen();
            Palettes = new List<IPalette>() { CurrentPalette, new Orange(), new DeepPurple() };
            VisualizeCommand = new VisualizeCommand(this);
        }

        public bool Available
        {
            get => available;
            set
            {
                available = value;
                OnPropertyChanged(nameof(Available));
            }
        }

        public bool CanCustomize
        {
            get => canCustomize;
            set
            {
                canCustomize = value;
                OnPropertyChanged(nameof(CanCustomize));
            }
        }

        public bool Fullscreen
        {
            get => fullscreen;
            set
            {
                fullscreen = value;
                OnPropertyChanged(nameof(Fullscreen));
            }
        }

        public bool Circles
        {
            get => circles;
            set
            {
                circles = value;
                OnPropertyChanged(nameof(Circles));
            }
        }

        public IEnumerable<IPalette> Palettes { get; }

        public IPalette CurrentPalette
        {
            get => currentPalette;
            set
            {
                currentPalette = value;
                OnPropertyChanged(nameof(CurrentPalette));
            }
        }

        public ViewModelBase CurrentViewModel { get; set; }

        public ICommand VisualizeCommand { get; }
    }
}
