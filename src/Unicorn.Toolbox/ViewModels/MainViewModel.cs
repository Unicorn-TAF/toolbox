using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Roi;
using Unicorn.Toolbox.Stats;
using Unicorn.Toolbox.Visualization;
using Unicorn.Toolbox.Visualization.Palettes;

namespace Unicorn.Toolbox.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly RoiInputs _roiInputs;
    private int selectedTab;
        private IPalette currentPalette;
        private bool fullscreen;
        private bool circles;

    public MainViewModel(StatsCollector statsCollector)
    {
        _roiInputs = new RoiInputs();
        StatsViewModel = new StatsViewModel(statsCollector);
        CoverageViewModel = new CoverageViewModel(statsCollector);
        LaunchViewModel = new LaunchViewModel();
        RoiConfigurationViewModel = new RoiConfigurationViewModel(_roiInputs);

        CurrentVisualizationPalette = new LightGreen();
        VisualizationPalettes = new List<IPalette>() { CurrentVisualizationPalette, new Orange(), new DeepPurple() };
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

        public bool FullscreenVisualization
        {
            get => fullscreen;
            set
            {
                fullscreen = value;
                OnPropertyChanged(nameof(FullscreenVisualization));
            }
        }

        public bool CirclesVisualization
        {
            get => circles;
            set
            {
                circles = value;
                OnPropertyChanged(nameof(CirclesVisualization));
            }
        }

    public IEnumerable<IPalette> VisualizationPalettes { get; }

        public IPalette CurrentVisualizationPalette
        {
            get => currentPalette;
            set
            {
                currentPalette = value;
                OnPropertyChanged(nameof(CurrentVisualizationPalette));
            }
        }

    public ICommand VisualizeCommand { get; }

    private void TabChanged(int selectedTab)
    {
        switch (selectedTab)
        {
            case 0:
                CurrentViewModel = StatsViewModel;
                break;
            case 1:
                CurrentViewModel = CoverageViewModel;
                break;
            case 2:
                CurrentViewModel = LaunchViewModel;
                break;
            case 3:
                CurrentViewModel = RoiConfigurationViewModel;
                break;
            default:
                throw new NotImplementedException($"Unknown tab index {selectedTab}");
        }

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
