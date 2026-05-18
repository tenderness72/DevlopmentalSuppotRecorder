using System.IO;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SessionRecorder.App.Services;
using SessionRecorder.App.ViewModels;
using SessionRecorder.App.Views;
using SessionRecorder.Data;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // LiveCharts2 初期化 + 日本語フォント設定
        var jpTypeface =
            SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Normal) ??
            SKTypeface.FromFamilyName("Meiryo UI",    SKFontStyle.Normal) ??
            SKTypeface.FromFamilyName("MS UI Gothic", SKFontStyle.Normal) ??
            SKTypeface.Default;

        LiveCharts.Configure(config => config
            .AddSkiaSharp()
            .AddDefaultMappers()
            .HasGlobalSKTypeface(jpTypeface));

        var services = new ServiceCollection();

        // Database
        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SessionRecorder");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "session_records.db");
        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        services.AddScoped<IChildRepository,              ChildRepository>();
        services.AddScoped<IProgramRepository,            ProgramRepository>();
        services.AddScoped<ISessionRecordRepository,      SessionRecordRepository>();
        services.AddScoped<INaturalObservationRepository, NaturalObservationRepository>();
        services.AddScoped<ISkillDomainRepository,        SkillDomainRepository>();
        services.AddScoped<IProgramTypeRepository,        ProgramTypeRepository>();

        // Services
        services.AddSingleton<BackupService>();
        services.AddSingleton<ExcelExportService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ChildListViewModel>();
        services.AddTransient<ProgramListViewModel>();
        services.AddTransient<SessionEntryViewModel>();
        services.AddTransient<ObservationEntryViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<SkillDomainListViewModel>();
        services.AddTransient<SessionListViewModel>();
        services.AddTransient<ObservationListViewModel>();
        services.AddTransient<ProgressGraphViewModel>();
        services.AddTransient<BackupViewModel>();
        services.AddTransient<ProgramTypeListViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Initialize DB
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            MigrateProgramTypeIfNeeded(db);
        }

        // Backup
        var backup = _serviceProvider.GetRequiredService<BackupService>();
        backup.CreateBackupOnStartup();

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// 旧来 enum ベースの ProgramType（TEXT 列）から、マスタテーブル + ProgramTypeId(int) 構成への
    /// 一度きりのマイグレーション。新規 DB では何もしない。
    /// </summary>
    private static void MigrateProgramTypeIfNeeded(AppDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();

        // 1. Programs テーブルの列構成を確認
        bool oldColumnExists = false;
        bool newColumnExists = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(Programs)";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader.GetString(1);
                if (string.Equals(name, "ProgramType",   StringComparison.OrdinalIgnoreCase)) oldColumnExists = true;
                if (string.Equals(name, "ProgramTypeId", StringComparison.OrdinalIgnoreCase)) newColumnExists = true;
            }
        }

        // 2. ProgramTypes テーブルが無ければ作成（既存DBのケース）
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ProgramTypes (
                    Id INTEGER NOT NULL CONSTRAINT PK_ProgramTypes PRIMARY KEY AUTOINCREMENT,
                    TypeCode TEXT NOT NULL,
                    TypeName TEXT NOT NULL,
                    Notes TEXT NULL,
                    IsActive INTEGER NOT NULL
                );
                CREATE UNIQUE INDEX IF NOT EXISTS IX_ProgramTypes_TypeCode ON ProgramTypes(TypeCode);
            ";
            cmd.ExecuteNonQuery();
        }

        // 3. 初期シード（既存DBで空ならデフォルト3件を挿入）
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM ProgramTypes";
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO ProgramTypes (Id, TypeCode, TypeName, IsActive) VALUES
                    (1, 'StructuredWorksheet', '構造化WS',     1),
                    (2, 'RolePlay',            'ロールプレイ', 1),
                    (3, 'DirectInstruction',   '直接教示',     1);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        // 4. Programs に ProgramTypeId 列が無ければ追加
        if (!newColumnExists)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Programs ADD COLUMN ProgramTypeId INTEGER NOT NULL DEFAULT 3";
            cmd.ExecuteNonQuery();
        }

        // 5. 旧 ProgramType（TEXT enum 名）が残っていれば、新FKに移行
        if (oldColumnExists)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE Programs
                    SET ProgramTypeId = COALESCE(
                        (SELECT Id FROM ProgramTypes WHERE TypeCode = Programs.ProgramType),
                        3)";
                cmd.ExecuteNonQuery();
            }

            // 旧列は EF が参照しないので残しても害は無いが、可能なら掃除
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Programs DROP COLUMN ProgramType";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // SQLite が古くて DROP COLUMN 非対応の場合は無視（EF は新FKしか見ない）
            }
        }
    }
}
