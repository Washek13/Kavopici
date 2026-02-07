using System.Windows;
using System.Windows.Controls;
using Kavopici.ViewModels;

namespace Kavopici.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
