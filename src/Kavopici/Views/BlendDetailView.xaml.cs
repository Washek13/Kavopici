using System.Windows;
using System.Windows.Controls;
using Kavopici.ViewModels;

namespace Kavopici.Views;

public partial class BlendDetailView : UserControl
{
    public BlendDetailView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BlendDetailViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
