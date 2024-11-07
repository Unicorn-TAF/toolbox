using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Roi;
using Unicorn.Toolbox.Stats;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly RoiInputs _roiInputs;
    private int selectedTab;

    public MainViewModel(StatsCollector statsCollector)
    {
        _roiInputs = new RoiInputs();
        StatsViewModel = new StatsViewModel(statsCollector);
        CoverageViewModel = new CoverageViewModel(statsCollector);
        LaunchViewModel = new LaunchViewModel();
        RoiConfigurationViewModel = new RoiConfigurationViewModel(_roiInputs);

        VisualizationPalettes = Palettes.AvailablePalettes;
        CurrentVisualizationPalette = VisualizationPalettes.First();
        VisualizeCommand = new VisualizeCommand(this);
        CurrentViewModel = StatsViewModel;

        StatsViewModel.PropertyChanged += OnDataLoadedPropertyChanged;
        CoverageViewModel.PropertyChanged += OnDataLoadedPropertyChanged;
        LaunchViewModel.PropertyChanged += OnDataLoadedPropertyChanged;
    }

    public FunctionalityViewModelBase StatsViewModel { get; }

    public FunctionalityViewModelBase CoverageViewModel { get; }

    public FunctionalityViewModelBase LaunchViewModel { get; }

    public FunctionalityViewModelBase RoiConfigurationViewModel { get; set; }

    public FunctionalityViewModelBase CurrentViewModel { get; set; }

    public int SelectedTab
    {
        get => selectedTab;

        set
        {
            selectedTab = value;
            OnPropertyChanged(nameof(SelectedTab));
            TabChanged(selectedTab);
        }
    }

    [Notify]
    public bool FullscreenVisualization { get; set; }

    [Notify]
    public IEnumerable<IPalette> VisualizationPalettes { get; }

    [Notify]
    public IPalette CurrentVisualizationPalette { get; set; }

    public ICommand VisualizeCommand { get; }

    private void TabChanged(int selectedTab)
    {
        CurrentViewModel = selectedTab switch
        {
            0 => StatsViewModel,
            1 => CoverageViewModel,
            2 => LaunchViewModel,
            3 => RoiConfigurationViewModel,
            _ => throw new NotImplementedException($"Unknown tab index {selectedTab}"),
        };
        UpdateProperties();
    }

    public void UpdateProperties() =>
        OnPropertyChanged(nameof(CurrentViewModel));

    private void OnDataLoadedPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FunctionalityViewModelBase.DataLoaded))
        {
            UpdateProperties();
        }
    }
}
