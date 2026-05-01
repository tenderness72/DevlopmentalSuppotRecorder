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
}
