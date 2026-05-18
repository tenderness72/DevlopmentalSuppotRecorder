using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ProgramTypeListView : UserControl
{
    public ProgramTypeListView()
    {
        InitializeComponent();
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && DataContext is ProgramTypeListViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
