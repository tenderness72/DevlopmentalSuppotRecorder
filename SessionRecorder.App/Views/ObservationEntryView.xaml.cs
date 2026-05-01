using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ObservationEntryView : UserControl
{
    public ObservationEntryView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ObservationEntryViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
