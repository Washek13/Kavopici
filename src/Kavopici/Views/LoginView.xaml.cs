using System.Windows;
using System.Windows.Controls;
using Kavopici.ViewModels;

namespace Kavopici.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            await vm.LoadUsersCommand.ExecuteAsync(null);
        }
    }
}
