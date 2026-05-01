using System.IO;
using System.Windows;
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

        var services = new ServiceCollection();

        // Database
        var dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SessionRecorder");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "session_records.db");
        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        services.AddScoped<IChildRepository, ChildRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<ISessionRecordRepository, SessionRecordRepository>();
        services.AddScoped<INaturalObservationRepository, NaturalObservationRepository>();
        services.AddScoped<ISkillDomainRepository, SkillDomainRepository>();

        // Services
        services.AddSingleton<BackupService>();
        services.AddSingleton<ExcelExportService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ChildListViewModel>();
        services.AddTransient<ProgramListViewModel>();
        services.AddTransient<SessionEntryViewModel>();
        services.AddTransient<ObservationEntryViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<SkillDomainListViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Initialize DB
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
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
}
