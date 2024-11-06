namespace Unicorn.Toolbox.ViewModels;

public class DialogHostViewModel
{
    public DialogHostViewModel(IDialogViewModel viewModel)
    {
        CurrentViewModel = viewModel;
    }

    [Notify]
    public IDialogViewModel CurrentViewModel { get; set; }
}
