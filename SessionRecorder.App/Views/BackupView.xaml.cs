using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && DataContext is BackupViewModel vm)
            vm.LoadCommand.Execute(null);
    }
}
