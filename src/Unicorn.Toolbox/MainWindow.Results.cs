using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Unicorn.Toolbox.LaunchAnalysis;
using Unicorn.Toolbox.Visualization;

namespace Unicorn.Toolbox
{
    public partial class MainWindow
    {
        private LaunchResult launchResult;
        private ExecutedTestsFilter failedTestsFilter;
        private ListCollectionView listCollectionView;
        private bool groupBoxVisualizationStateTemp = false;
        private bool trxLoaded = false;
        private string StatusLineResults = string.Empty;


        private void LoadTrx(object sender, RoutedEventArgs e)
        {
            buttonVisualize.IsEnabled = false;
            checkBoxFullscreen.IsEnabled = false;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "Trx files|*.trx",
                Multiselect = true
            };

            if (!(bool)openFileDialog.ShowDialog() || !openFileDialog.FileNames.Any())
            {
                return;
            }

            var trxFiles = openFileDialog.FileNames;
            StatusLineResults = "Loading .trx";
            statusBarText.Text = StatusLineResults;
            var taskLoading = Task.Factory.StartNew(() =>
            {
                launchResult = new LaunchResult();

                foreach (var trxFile in trxFiles)
                {
                    try
                    {
                        launchResult.AppendResultsFromTrx(trxFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error parsing {trxFile} file:" + ex.ToString());
                    }
                }
            });

            taskLoading.ContinueWith((t1) =>
            {
                var obColl = new ObservableCollection<Execution>(launchResult.Executions);
                this.Dispatcher.Invoke(() =>
                {
                    listCollectionView = new ListCollectionView(obColl);
                    gridTestResults.ItemsSource = null;
                    gridTestResults.ItemsSource = listCollectionView;

                    textBoxLaunchSummary.Text = launchResult.ToString();

                    buttonVisualize.IsEnabled = true;
                    checkBoxFullscreen.IsEnabled = true;
                    trxLoaded = true;

                    stackPanelFails.Children.Clear();

                    var results = ExecutedTestsFilter.GetTopErrors(launchResult.Executions.SelectMany(exec => exec.TestResults));

                    for (int i = 0; i < results.Count(); i++)
                    {
                        stackPanelFails.Children.Add(new FailedTestsGroup(results.ElementAt(i)));
                    }
                });
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            StatusLineResults = $"{trxFiles.Count()} .trx files were loaded";
        }

        private void FilterExecutions(object sender, TextChangedEventArgs e)
        {
            if (listCollectionView == null)
            {
                return;
            }

            listCollectionView.Filter = (item) => { return ((Execution)item).Name.Contains(((TextBox)sender).Text); };
        }

        private void VisualizeResults()
        {
            var visualization = GetVisualizationWindow("Launch visualization");
            visualization.Show();

            new LaunchVisualizer(visualization.canvasVisualization, launchResult.Executions).Visualize();
        }

        private void SearchInExecutedTests(object sender, RoutedEventArgs e)
        {
            if (comboFilterExecutedTestsBy.Text.Equals("By fail message"))
            {
                failedTestsFilter = new ExecutedTestsFilter(textBoxFailMessage.Text, checkboxFailMessageRegex.IsChecked.Value);
                failedTestsFilter.FilterTestsByFailMessage(launchResult.Executions.SelectMany(exec => exec.TestResults));
            }
            else
            {
                failedTestsFilter = new ExecutedTestsFilter(textBoxFailMessage.Text);
                failedTestsFilter.FilterTestsByTime(launchResult.Executions.SelectMany(exec => exec.TestResults));
            }

            labelFoundFailedTests.Content = "Found: " + failedTestsFilter.MatchingTestsCount;
        }

        private void OpenFilteredTests(object sender, MouseButtonEventArgs e)
        {
            var window = new WindowTestsByMessage();
            window.gridResults.ItemsSource = failedTestsFilter.FilteredResults;
            window.ShowDialog();
        }

        private void ActivateSearchByMessage(object sender, RoutedEventArgs e) =>
            checkboxFailMessageRegex.Visibility = Visibility.Visible;

        private void ActivateSearchByTime(object sender, RoutedEventArgs e) =>
            checkboxFailMessageRegex.Visibility = Visibility.Hidden;
    }
}
