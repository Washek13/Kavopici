using System.Windows;
using System.Windows.Controls;
using Kavopici.ViewModels;

namespace Kavopici.Views;

public partial class ComparisonView : UserControl
{
    public ComparisonView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ComparisonViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
