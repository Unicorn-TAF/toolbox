namespace Unicorn.Toolbox.ViewModels
{
    public class DialogHostViewModel : ViewModelBase
    {
        public DialogHostViewModel(ViewModelBase viewModel)
        {
            CurrentViewModel = viewModel;
            OnCurrentViewModelChanged();
        }

        public ViewModelBase CurrentViewModel { get; set; }

        private void OnCurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }
    }
}
