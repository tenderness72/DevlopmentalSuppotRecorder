using System.Windows;
using System.Windows.Controls;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class ProgramListView : UserControl
{
    public ProgramListView()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue && DataContext is ProgramListViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
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
