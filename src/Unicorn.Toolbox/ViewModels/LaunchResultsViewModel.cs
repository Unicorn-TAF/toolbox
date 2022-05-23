using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.LaunchAnalysis;
using Unicorn.Toolbox.Views;

namespace Unicorn.Toolbox.ViewModels
{
    public enum FailsFilter
    { 
        ErrorMessage,
        Time,
        ErrorMessageRegex
    }

    public class LaunchResultsViewModel : ViewModelBase
    {
        private readonly LaunchResult _launchResult;
        private readonly MainWindow _window;
        private ListCollectionView listCollectionView;

        //private ExecutedTestsFilter failedTestsFilter;
        public string filterGridBy;
        public string filterTestsBy;
        public string failMessage;
        private bool regex;
        private bool searchByMessage;
        private FailsFilter filterFailsBy = FailsFilter.ErrorMessage;

        public LaunchResultsViewModel()
        {
            _window = App.Current.MainWindow as MainWindow;
            _launchResult = new LaunchResult();
            LoadTrxCommand = new LoadTrxCommand(this, _launchResult);
            SearchInExecutedTestsCommand = new SearchInExecutedTestsCommand(this, _launchResult);
            OpenFilteredTestsCommand = new OpenFilteredTestsCommand(this);
        }

        public ICommand LoadTrxCommand { get; }

        public ICommand SearchInExecutedTestsCommand { get; }

        public ICommand OpenFilteredTestsCommand { get; }

        public string FilterGridBy
        {
            get => filterGridBy;

            set
            {
                filterGridBy = value;
                OnPropertyChanged(nameof(FilterGridBy));
                FilterExecutions();
            }
        }

        public IEnumerable<FailsFilter> FailsFilters { get; } =
            Enum.GetValues(typeof(FailsFilter)).Cast<FailsFilter>();

        public FailsFilter FilterFailsBy
        {
            get => filterFailsBy;
            set
            {
                filterFailsBy = value;
                OnPropertyChanged(nameof(FilterFailsBy));
            }
        }

        public string FailSearchCriteria
        {
            get => failMessage;

            set
            {
                failMessage = value;
                OnPropertyChanged(nameof(FailSearchCriteria));
            }
        }

        public ExecutedTestsFilter Filter { get; set; }

        private void FilterExecutions()
        {
            if (listCollectionView == null)
            {
                return;
            }

            listCollectionView.Filter = (item) => { return ((Execution)item).Name.Contains(FilterGridBy); };
        }

        public void UpdateModel()
        {
            //taskLoading.ContinueWith((t1) =>
            //{
                var obColl = new ObservableCollection<Execution>(_launchResult.Executions);
                _window.Dispatcher.Invoke(() =>
                {
                    listCollectionView = new ListCollectionView(obColl);
                    _window.LaunchResultsView.gridTestResults.ItemsSource = null;
                    _window.LaunchResultsView.gridTestResults.ItemsSource = listCollectionView;

                    _window.LaunchResultsView.textBoxLaunchSummary.Text = _launchResult.ToString();

                    //TODO
                    _window.buttonVisualize.IsEnabled = true;
                    _window.checkBoxFullscreen.IsEnabled = true;
                    //trxLoaded = true;

                    _window.LaunchResultsView.stackPanelFails.Children.Clear();

                    var results = ExecutedTestsFilter.GetTopErrors(_launchResult.Executions.SelectMany(exec => exec.TestResults));

                    for (int i = 0; i < results.Count(); i++)
                    {
                        _window.LaunchResultsView.stackPanelFails.Children.Add(new FailedTestsGroup(results.ElementAt(i)));
                    }
                });
            //}, TaskContinuationOptions.OnlyOnRanToCompletion);

            _window.LaunchResultsView.Status = $"{_launchResult.Executions.Count()} .trx files were loaded";
        }

        public void UpdateFilteredTestsCount()
        {
            _window.LaunchResultsView.labelFoundFailedTests.Content = "Found: " + Filter.MatchingTestsCount;
        }
    }
}
