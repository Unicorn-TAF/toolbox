namespace Unicorn.Toolbox.ViewModels;

public class FilterItemViewModel : ViewModelBase
{
    private bool selected;

    public FilterItemViewModel(string name)
    {
        Name = name;
        selected = true;
    }

    public string Name { get; }

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
