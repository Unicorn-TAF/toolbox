using System.Windows;

namespace Unicorn.Toolbox
{
    /// <summary>
    /// Interaction logic for DialogHost.xaml
    /// </summary>
    public partial class DialogHost : Window
    {
        public DialogHost(string title)
        {
            InitializeComponent();
            Title = title;
        }
    }
}
