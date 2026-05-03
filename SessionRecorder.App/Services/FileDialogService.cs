using Microsoft.Win32;

namespace SessionRecorder.App.Services;

public interface IFileDialogService
{
    string? ShowSaveCsvDialog(string defaultFileName = "sessions");
}

public class FileDialogService : IFileDialogService
{
    public string? ShowSaveCsvDialog(string defaultFileName = "sessions")
    {
        var dlg = new SaveFileDialog
        {
            FileName = defaultFileName,
            DefaultExt = ".csv",
            Filter = "CSV ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
