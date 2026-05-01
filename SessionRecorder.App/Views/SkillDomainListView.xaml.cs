using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class SkillDomainListView : UserControl
{
    public SkillDomainListView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SkillDomainListViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
