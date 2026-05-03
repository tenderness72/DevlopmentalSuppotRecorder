using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ObservationListView : UserControl
{
    public ObservationListView()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is ObservationListViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
