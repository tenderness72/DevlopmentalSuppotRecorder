using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class SessionEntryView : UserControl
{
    public SessionEntryView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SessionEntryViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
