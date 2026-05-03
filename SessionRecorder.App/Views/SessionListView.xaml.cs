using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class SessionListView : UserControl
{
    public SessionListView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SessionListViewModel vm)
            _ = vm.LoadCommand.ExecuteAsync(null);
    }
}
