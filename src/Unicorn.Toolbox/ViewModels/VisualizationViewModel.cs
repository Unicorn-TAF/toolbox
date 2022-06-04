using System.Windows.Input;
using Unicorn.Toolbox.Commands;

namespace Unicorn.Toolbox.ViewModels
{
    public class VisualizationViewModel : ViewModelBase
    {
        private bool exportable;

        public VisualizationViewModel()
        {
            ExportVisualizationCommand = new ExportVisualizationCommand();
        }

        public bool Exportable
        {
            get => exportable;

            set
            {
                exportable = value;
                OnPropertyChanged(nameof(Exportable));
            }
        }

        public ICommand ExportVisualizationCommand { get; }
    }
}
