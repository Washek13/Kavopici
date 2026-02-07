using System.Windows;
using System.Windows.Controls;
using Kavopici.ViewModels;

namespace Kavopici.Views;

public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AdminViewModel vm)
        {
            await vm.LoadAllCommand.ExecuteAsync(null);
        }
    }
}
