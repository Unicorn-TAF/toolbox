using System.Windows.Controls;

namespace Unicorn.Toolbox.Views
{
    /// <summary>
    /// Interaction logic for CoverageView.xaml
    /// </summary>
    public partial class CoverageView : UserControl
    {
        public CoverageView()
        {
            InitializeComponent();
        }

        public string Status { get; set; } = string.Empty;
    }
}
