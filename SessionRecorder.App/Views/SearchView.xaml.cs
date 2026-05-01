using System.Windows.Controls;
using System.Windows.Input;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class SearchView : UserControl
{
    public SearchView()
    {
        InitializeComponent();
    }

    private async void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SearchViewModel vm)
            await vm.SearchCommand.ExecuteAsync(null);
    }
}
