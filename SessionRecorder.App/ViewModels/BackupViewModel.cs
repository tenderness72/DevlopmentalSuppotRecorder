using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Services;

namespace SessionRecorder.App.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly BackupService     _backupService;
    private readonly IFileDialogService _fileDialog;

    public ObservableCollection<BackupFileInfo> BackupFiles { get; } = [];

    [ObservableProperty] private string _backupFolderPath = "";
    [ObservableProperty] private string _statusMessage    = "";
    [ObservableProperty] private bool   _isBusy;

    public BackupViewModel(BackupService backupService, IFileDialogService fileDialog)
    {
        _backupService = backupService;
        _fileDialog    = fileDialog;
    }

    [RelayCommand]
    private void Load()
    {
        BackupFolderPath = _backupService.BackupFolderPath;
        RefreshList();
    }

    [RelayCommand]
    private void CreateManualBackup()
    {
        try
        {
            IsBusy = true;
            var path = _backupService.CreateManualBackup();
            StatusMessage = $"バックアップを作成しました：{Path.GetFileName(path)}";
            RefreshList();
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー：{ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        try
        {
            _backupService.OpenBackupFolder();
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー：{ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportDb()
    {
        var defaultName = $"session_records_{DateTime.Today:yyyyMMdd}";
        var path = _fileDialog.ShowSaveDbDialog(defaultName);
        if (path == null) return;

        try
        {
            IsBusy = true;
            _backupService.ExportTo(path);
            StatusMessage = $"エクスポート完了：{Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー：{ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private void RefreshList()
    {
        BackupFiles.Clear();
        foreach (var f in _backupService.GetBackupFiles())
            BackupFiles.Add(f);
        StatusMessage = BackupFiles.Count == 0
            ? "バックアップファイルはありません"
            : $"{BackupFiles.Count} 件のバックアップ";
    }
}
