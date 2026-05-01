using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ProgramListView : UserControl
{
    public ProgramListView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProgramListViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProgramListViewModel vm)
            vm.EditProgramCommand.Execute(null);
    }

    private void OnMasteryCriteriaLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var text = tb.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // 数値のみ（整数・小数）なら%を付加
        if (double.TryParse(text, out _) && !text.EndsWith('%'))
            tb.Text = text + "%";
    }
}
