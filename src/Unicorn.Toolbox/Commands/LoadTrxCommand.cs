using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using Unicorn.Toolbox.LaunchAnalysis;
using Unicorn.Toolbox.ViewModels;

namespace Unicorn.Toolbox.Commands
{
    public class LoadTrxCommand : CommandBase
    {
        private readonly LaunchResult _launchResult;
        private readonly LaunchResultsViewModel _viewModel;

        public LoadTrxCommand(LaunchResultsViewModel viewModel, LaunchResult launchResult)
        {
            _launchResult = launchResult;
            _viewModel = viewModel;
        }

        public override void Execute(object parameter)
        {
            // TODO
            //buttonVisualize.IsEnabled = false;
            //checkBoxFullscreen.IsEnabled = false;

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
            //Status = "Loading .trx";
            //statusBarText.Text = StatusLineResults;

            _launchResult.Clear();

            //var taskLoading = Task.Factory.StartNew(() =>
            //{
                foreach (var trxFile in trxFiles)
                {
                    try
                    {
                        _launchResult.AppendResultsFromTrx(trxFile);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error parsing {trxFile} file:" + ex.ToString(), 
                            "Error", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
                    }
                }
            //});

            _viewModel.UpdateModel();
        }
    }
}
