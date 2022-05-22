using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Unicorn.Toolbox.LaunchAnalysis;

namespace Unicorn.Toolbox.Views
{
    /// <summary>
    /// Interaction logic for LaunchResultView.xaml
    /// </summary>
    public partial class LaunchResultsView : UserControl
    {
        internal bool groupBoxVisualizationStateTemp = false;
        internal bool trxLoaded = false;
        

        public LaunchResultsView()
        {
            InitializeComponent();
        }

        public string Status { get; set; } = string.Empty;
    }
}
