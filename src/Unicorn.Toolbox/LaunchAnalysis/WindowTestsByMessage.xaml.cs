using System.Collections.Generic;
using System.Windows;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    /// <summary>
    /// Interaction logic for WindowTestPreview
    /// </summary>
    public partial class WindowTestsByMessage : Window
    {
        public WindowTestsByMessage()
        {
            InitializeComponent();
        }

        public void SetDataSource(Dictionary<string, IEnumerable<TestResult>> data)
        {
            TestsByMessage.gridResults.ItemsSource = data;
        }
    }
}
