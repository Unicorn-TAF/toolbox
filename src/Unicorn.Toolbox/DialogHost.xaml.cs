using System.Collections.Generic;
using System.Windows;
using Unicorn.Toolbox.LaunchAnalysis;
using Unicorn.Toolbox.ViewModels;

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
