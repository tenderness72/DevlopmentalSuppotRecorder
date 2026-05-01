using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ChildListView : UserControl
{
    public ChildListView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChildListViewModel vm)
        {
            await vm.LoadCommand.ExecuteAsync(null);
        }
    }
}
