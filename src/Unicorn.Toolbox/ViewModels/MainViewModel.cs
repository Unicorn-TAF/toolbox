using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Stats;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly StatsCollector _statsCollector;

    public MainViewModel()
    {
        _statsCollector = new StatsCollector();
        StatsViewModel = new StatsViewModel(_statsCollector);
        CoverageViewModel = new CoverageViewModel(_statsCollector);
        LaunchViewModel = new LaunchViewModel();
        RoiConfigurationViewModel = new RoiConfigurationViewModel();

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

    [Notify]
    public FunctionalityViewModelBase CurrentViewModel { get; set; }

    [Notify]
    [CallAlso(nameof(TabChanged))]
    public int SelectedTab { get; set; }

    [Notify]
    public bool FullscreenVisualization { get; set; }

    [Notify]
    public IEnumerable<IPalette> VisualizationPalettes { get; }

    [Notify]
    public IPalette CurrentVisualizationPalette { get; set; }

    public ICommand VisualizeCommand { get; }

    private void TabChanged()
    {
        CurrentViewModel = SelectedTab switch
        {
            0 => StatsViewModel,
            1 => CoverageViewModel,
            2 => LaunchViewModel,
            3 => RoiConfigurationViewModel,
            _ => throw new NotImplementedException($"Unknown tab index {SelectedTab}"),
        };
    }

    private void OnDataLoadedPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FunctionalityViewModelBase.DataLoaded))
        {
            TabChanged();
        }
    }
}
