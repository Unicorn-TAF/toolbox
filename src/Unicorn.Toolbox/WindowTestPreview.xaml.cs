using System.Collections.Generic;
using System.Windows;
using Unicorn.Toolbox.Analysis;

namespace Unicorn.Toolbox
{
    /// <summary>
    /// Interaction logic for WindowTestPreview
    /// </summary>
    public partial class WindowTestPreview : Window
    {
        public WindowTestPreview()
        {
            InitializeComponent();
        }

        public void SetDataSource(List<TestInfo> data)
        {
            TestsPreview.gridResults.ItemsSource = data;
        }
    }
}
