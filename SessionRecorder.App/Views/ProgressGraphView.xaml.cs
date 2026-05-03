using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
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

    private void OnSaveImageClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as ProgressGraphViewModel;
        var defaultName = vm?.SelectedChild != null && vm.SelectedProgramItem != null
            ? $"graph_{vm.SelectedChild.ChildCode}_{vm.SelectedProgramItem.Program.ProgramCode}_{DateTime.Today:yyyyMMdd}"
            : $"graph_{DateTime.Today:yyyyMMdd}";

        var dlg = new SaveFileDialog
        {
            FileName   = defaultName,
            DefaultExt = ".png",
            Filter     = "PNG 画像 (*.png)|*.png|すべてのファイル (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            // ChartBorder ごと 96dpi でレンダリング
            var element = ChartBorder;
            var dpi     = 96.0;
            var rtb     = new RenderTargetBitmap(
                (int)element.ActualWidth,
                (int)element.ActualHeight,
                dpi, dpi, PixelFormats.Pbgra32);
            rtb.Render(element);

            using var stream  = File.Create(dlg.FileName);
            var       encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(stream);

            MessageBox.Show($"画像を保存しました:\n{dlg.FileName}",
                "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました:\n{ex.Message}",
                "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
