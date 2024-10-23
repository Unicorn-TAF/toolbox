namespace Unicorn.Toolbox.ViewModels;

public class DialogHostViewModel
{
    public DialogHostViewModel(ViewModelBase viewModel)
    {
        CurrentViewModel = viewModel;
    }

    [Notify]
    public ViewModelBase CurrentViewModel { get; set; }
}
