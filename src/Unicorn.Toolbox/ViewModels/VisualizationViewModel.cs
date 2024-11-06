using System.Windows.Input;
using Unicorn.Toolbox.Commands;

namespace Unicorn.Toolbox.ViewModels;

public class VisualizationViewModel : IDialogViewModel
{
    public VisualizationViewModel()
    {
        ExportVisualizationCommand = new ExportVisualizationCommand();
    }

    [Notify]
    public bool Exportable { get; set; }

    public ICommand ExportVisualizationCommand { get; }
}
