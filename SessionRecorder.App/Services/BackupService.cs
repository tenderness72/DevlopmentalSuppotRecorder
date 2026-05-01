using System.IO;

namespace SessionRecorder.App.Services;

public class BackupService
{
    private readonly string _dbFolder;

    public BackupService()
    {
        _dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SessionRecorder");
    }

    public void CreateBackupOnStartup()
    {
        var dbPath = Path.Combine(_dbFolder, "session_records.db");
        if (!File.Exists(dbPath)) return;

        var backupFolder = Path.Combine(_dbFolder, "backups");
        Directory.CreateDirectory(backupFolder);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupFolder, $"session_records_{timestamp}.db");

        // 1日1回だけバックアップ
        var today = DateTime.Now.ToString("yyyyMMdd");
        var existing = Directory.GetFiles(backupFolder, $"session_records_{today}*.db");
        if (existing.Length > 0) return;

        File.Copy(dbPath, backupPath);

        // 30日超のバックアップを削除
        var cutoff = DateTime.Now.AddDays(-30);
        foreach (var file in Directory.GetFiles(backupFolder, "*.db"))
        {
            if (File.GetCreationTime(file) < cutoff)
                File.Delete(file);
        }
    }

    public string ExportTo(string destinationPath)
    {
        var dbPath = Path.Combine(_dbFolder, "session_records.db");
        File.Copy(dbPath, destinationPath, overwrite: true);
        return destinationPath;
    }
}
