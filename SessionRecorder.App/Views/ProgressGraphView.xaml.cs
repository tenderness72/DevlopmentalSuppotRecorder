using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ProgressGraphView : UserControl
{
    public ProgressGraphView()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is ProgressGraphViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
