﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Unicorn.Toolbox.Commands;
using Unicorn.Toolbox.Models.Launch;

namespace Unicorn.Toolbox.ViewModels
{
    public enum FailsFilter
    { 
        ErrorMessage,
        Time,
        ErrorMessageRegex
    }

    public class LaunchViewModel : ViewModelBase
    {
        private readonly LaunchResult _launchResult;
        private readonly ObservableCollection<FailedTestsGroup> _topFailsList;
        private ListCollectionView listCollectionView;

        private bool dataLoaded;
        private string filterGridBy;
        private string failMessage;
        private FailsFilter filterFailsBy = FailsFilter.ErrorMessage;

        public LaunchViewModel()
        {
            _launchResult = new LaunchResult();
            _topFailsList = new ObservableCollection<FailedTestsGroup>();

            LoadTrxCommand = new LoadTrxCommand(this, _launchResult);
            SearchInExecutedTestsCommand = new SearchInExecutedTestsCommand(this, _launchResult);
            OpenFailsByMessageCommand = new OpenFailsByMessageCommand();
        }

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

        public ListCollectionView ExecutionsList => listCollectionView;

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

        public IEnumerable<FailedTestsGroup> TopFailsList => _topFailsList;

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

        public ICommand LoadTrxCommand { get; }

        public ICommand SearchInExecutedTestsCommand { get; }

        public ICommand OpenFailsByMessageCommand { get; }

        private void FilterExecutions()
        {
            if (listCollectionView != null)
            {
                listCollectionView.Filter = (item) => ((Execution)item).Name.Contains(FilterGridBy);
            }
        }

        public void UpdateViewModel()
        {
            listCollectionView = new ListCollectionView(_launchResult.Executions);
            OnPropertyChanged(nameof(ExecutionsList));

            var results = ExecutedTestsFilter
                .GetTopErrors(_launchResult.Executions.SelectMany(exec => exec.TestResults));

            _topFailsList.Clear();

            for (int i = 0; i < results.Count(); i++)
            {
                _topFailsList.Add(new FailedTestsGroup(results.ElementAt(i)));
            }

            OnPropertyChanged(nameof(TopFailsList));
        }
    }
}
