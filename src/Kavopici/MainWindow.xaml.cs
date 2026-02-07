using System.Windows;
using Kavopici.ViewModels;

namespace Kavopici;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
