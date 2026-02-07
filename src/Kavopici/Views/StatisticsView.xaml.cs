using System.Windows;
using System.Windows.Controls;
using Kavopici.ViewModels;

namespace Kavopici.Views;

public partial class StatisticsView : UserControl
{
    public StatisticsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StatisticsViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
