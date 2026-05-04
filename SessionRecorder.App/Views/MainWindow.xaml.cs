using System.ComponentModel;
using System.Windows;
using SessionRecorder.App.ViewModels;

namespace SessionRecorder.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // ▼ 未保存の変更がある場合、終了前に確認（デフォルト：キャンセル）
        if (DataContext is MainViewModel vm &&
            vm.CurrentView is IUnsavedChangesGuard guard &&
            guard.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "入力中のデータが保存されていません。\nアプリを終了すると変更が失われます。\n\n終了しますか？",
                "未保存の変更があります",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Cancel);

            if (result != MessageBoxResult.OK)
            {
                e.Cancel = true;    // ウィンドウを閉じない
                return;
            }
        }

        base.OnClosing(e);
    }
}
