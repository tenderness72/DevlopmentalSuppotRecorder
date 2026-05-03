using System.Diagnostics;
using System.IO;

namespace SessionRecorder.App.Services;

public class BackupFileInfo
{
    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public long FileSizeKb { get; init; }
    public string DisplayName => $"{CreatedAt:yyyy/MM/dd HH:mm}  （{FileSizeKb} KB）";
}

public class BackupService
{
    private readonly string _dbFolder;

    public string BackupFolderPath { get; }
    public string DbPath { get; }

    public BackupService()
    {
        _dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SessionRecorder");
        BackupFolderPath = Path.Combine(_dbFolder, "backups");
        DbPath = Path.Combine(_dbFolder, "session_records.db");
    }

    /// <summary>起動時の自動バックアップ（1日1回）</summary>
    public void CreateBackupOnStartup()
    {
        if (!File.Exists(DbPath)) return;
        Directory.CreateDirectory(BackupFolderPath);

        var today = DateTime.Now.ToString("yyyyMMdd");
        var existing = Directory.GetFiles(BackupFolderPath, $"session_records_{today}*.db");
        if (existing.Length > 0) return;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(BackupFolderPath, $"session_records_{timestamp}.db");
        File.Copy(DbPath, backupPath);

        PurgeOldBackups();
    }

    /// <summary>手動バックアップ（即時実行・ファイル名を返す）</summary>
    public string CreateManualBackup()
    {
        if (!File.Exists(DbPath))
            throw new FileNotFoundException("データベースファイルが見つかりません。", DbPath);

        Directory.CreateDirectory(BackupFolderPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(BackupFolderPath, $"session_records_{timestamp}_manual.db");
        File.Copy(DbPath, backupPath);
        PurgeOldBackups();
        return backupPath;
    }

    /// <summary>バックアップ一覧を取得（新しい順）</summary>
    public List<BackupFileInfo> GetBackupFiles()
    {
        if (!Directory.Exists(BackupFolderPath)) return [];

        return Directory.GetFiles(BackupFolderPath, "*.db")
            .Select(f => new BackupFileInfo
            {
                FilePath = f,
                FileName = Path.GetFileName(f),
                CreatedAt = File.GetLastWriteTime(f),
                FileSizeKb = new FileInfo(f).Length / 1024
            })
            .OrderByDescending(f => f.CreatedAt)
            .ToList();
    }

    /// <summary>エクスプローラーでバックアップフォルダを開く</summary>
    public void OpenBackupFolder()
    {
        Directory.CreateDirectory(BackupFolderPath);
        Process.Start("explorer.exe", BackupFolderPath);
    }

    /// <summary>DBを任意パスにコピーしてエクスポート</summary>
    public void ExportTo(string destinationPath)
    {
        File.Copy(DbPath, destinationPath, overwrite: true);
    }

    private void PurgeOldBackups()
    {
        var cutoff = DateTime.Now.AddDays(-30);
        foreach (var file in Directory.GetFiles(BackupFolderPath, "*.db"))
        {
            if (File.GetLastWriteTime(file) < cutoff)
                File.Delete(file);
        }
    }
}
