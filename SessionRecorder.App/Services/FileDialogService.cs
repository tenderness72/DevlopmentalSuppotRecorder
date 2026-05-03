using Microsoft.Win32;

namespace SessionRecorder.App.Services;

public interface IFileDialogService
{
    string? ShowSaveCsvDialog(string defaultFileName = "export");
    string? ShowSaveExcelDialog(string defaultFileName = "export");
    string? ShowSaveImageDialog(string defaultFileName = "graph");
    string? ShowSaveDbDialog(string defaultFileName = "session_records_backup");
}

public class FileDialogService : IFileDialogService
{
    public string? ShowSaveCsvDialog(string defaultFileName = "export")
    {
        var dlg = new SaveFileDialog
        {
            FileName   = defaultFileName,
            DefaultExt = ".csv",
            Filter     = "CSV ファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveExcelDialog(string defaultFileName = "export")
    {
        var dlg = new SaveFileDialog
        {
            FileName   = defaultFileName,
            DefaultExt = ".xlsx",
            Filter     = "Excel ファイル (*.xlsx)|*.xlsx|すべてのファイル (*.*)|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveImageDialog(string defaultFileName = "graph")
    {
        var dlg = new SaveFileDialog
        {
            FileName   = defaultFileName,
            DefaultExt = ".png",
            Filter     = "PNG 画像 (*.png)|*.png|すべてのファイル (*.*)|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? ShowSaveDbDialog(string defaultFileName = "session_records_backup")
    {
        var dlg = new SaveFileDialog
        {
            FileName   = defaultFileName,
            DefaultExt = ".db",
            Filter     = "SQLite データベース (*.db)|*.db|すべてのファイル (*.*)|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
