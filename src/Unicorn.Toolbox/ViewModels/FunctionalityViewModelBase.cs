namespace Unicorn.Toolbox.ViewModels
{
    public class FunctionalityViewModelBase : ViewModelBase
    {
        private bool dataLoaded;

        public bool DataLoaded
        {
            get => dataLoaded;

            set
            {
                dataLoaded = value;
                OnPropertyChanged(nameof(DataLoaded));
            }
        }

        public string Status { get; set; } = string.Empty;

        public virtual bool CanCustomizeVisualization { get; }
    }
}
