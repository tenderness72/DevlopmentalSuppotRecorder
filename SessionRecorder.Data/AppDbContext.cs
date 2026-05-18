using Microsoft.EntityFrameworkCore;
using SessionRecorder.Core.Entities;

namespace SessionRecorder.Data;

public class AppDbContext : DbContext
{
    public DbSet<Child> Children => Set<Child>();
    public DbSet<SkillDomain> SkillDomains => Set<SkillDomain>();
    public DbSet<ProgramTypeMaster> ProgramTypes => Set<ProgramTypeMaster>();
    public DbSet<InterventionProgram> Programs => Set<InterventionProgram>();
    public DbSet<SessionRecord> SessionRecords => Set<SessionRecord>();
    public DbSet<NaturalObservation> NaturalObservations => Set<NaturalObservation>();

    private readonly string _dbPath;

    public AppDbContext()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SessionRecorder");
        Directory.CreateDirectory(folder);
        _dbPath = Path.Combine(folder, "session_records.db");
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        _dbPath = "";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
            options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Child
        mb.Entity<Child>(e =>
        {
            e.HasIndex(c => c.ChildCode).IsUnique();
            e.Property(c => c.OtaStage).HasConversion<string>();
        });

        // SkillDomain
        mb.Entity<SkillDomain>(e =>
        {
            e.HasIndex(d => d.DomainCode).IsUnique();
        });

        // ProgramTypeMaster
        mb.Entity<ProgramTypeMaster>(e =>
        {
            e.HasIndex(t => t.TypeCode).IsUnique();
        });

        // InterventionProgram
        mb.Entity<InterventionProgram>(e =>
        {
            e.HasIndex(p => p.ProgramCode).IsUnique();
            e.HasOne(p => p.Domain)
                .WithMany(d => d.Programs)
                .HasForeignKey(p => p.DomainId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.ProgramType)
                .WithMany(t => t.Programs)
                .HasForeignKey(p => p.ProgramTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SessionRecord
        mb.Entity<SessionRecord>(e =>
        {
            e.HasOne(s => s.Child)
                .WithMany(c => c.SessionRecords)
                .HasForeignKey(s => s.ChildId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Program)
                .WithMany(p => p.SessionRecords)
                .HasForeignKey(s => s.ProgramId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(s => s.MasteryLevel).HasConversion<string>();
            e.Property(s => s.PromptLevel).HasConversion<string>();
            e.Ignore(s => s.CorrectRate);
        });

        // NaturalObservation
        mb.Entity<NaturalObservation>(e =>
        {
            e.HasOne(o => o.Child)
                .WithMany(c => c.NaturalObservations)
                .HasForeignKey(o => o.ChildId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(o => o.ObservationType).HasConversion<string>();
            e.Property(o => o.Result).HasConversion<string>();
        });

        // Seed: 初期領域データ
        mb.Entity<SkillDomain>().HasData(
            new SkillDomain { Id = 1,  DomainCode = "D001", DomainName = "マンド",               IsActive = true },
            new SkillDomain { Id = 2,  DomainCode = "D002", DomainName = "社会的スキル",          IsActive = true },
            new SkillDomain { Id = 3,  DomainCode = "D003", DomainName = "アサーション",          IsActive = true },
            new SkillDomain { Id = 4,  DomainCode = "D004", DomainName = "ToM",                   IsActive = true },
            new SkillDomain { Id = 5,  DomainCode = "D005", DomainName = "感情弁別",              IsActive = true },
            new SkillDomain { Id = 6,  DomainCode = "D006", DomainName = "表情弁別",              IsActive = true },
            new SkillDomain { Id = 7,  DomainCode = "D007", DomainName = "シフティング",          IsActive = true },
            new SkillDomain { Id = 8,  DomainCode = "D008", DomainName = "選択的注意",            IsActive = true },
            new SkillDomain { Id = 9,  DomainCode = "D009", DomainName = "持続的注意",            IsActive = true },
            new SkillDomain { Id = 10, DomainCode = "D010", DomainName = "配分的注意",            IsActive = true },
            new SkillDomain { Id = 11, DomainCode = "D011", DomainName = "視空間スケッチパッド",  IsActive = true },
            new SkillDomain { Id = 12, DomainCode = "D012", DomainName = "音韻ループ",            IsActive = true },
            new SkillDomain { Id = 13, DomainCode = "D013", DomainName = "中央実行系",            IsActive = true },
            new SkillDomain { Id = 14, DomainCode = "D014", DomainName = "エピソード・バッファ",  IsActive = true },
            new SkillDomain { Id = 99, DomainCode = "D999", DomainName = "その他",                IsActive = true }
        );

        // Seed: 初期プログラム型
        mb.Entity<ProgramTypeMaster>().HasData(
            new { Id = 1, TypeCode = "StructuredWorksheet", TypeName = "構造化WS",       IsActive = true },
            new { Id = 2, TypeCode = "RolePlay",            TypeName = "ロールプレイ",   IsActive = true },
            new { Id = 3, TypeCode = "DirectInstruction",   TypeName = "直接教示",       IsActive = true }
        );

        // Seed: "その他" program
        mb.Entity<InterventionProgram>().HasData(new
        {
            Id = 1,
            ProgramCode = "P999",
            ProgramName = "その他（単発課題）",
            DomainId = 99,
            ProgramTypeId = 3,  // 直接教示
            MasteryCriteria = "—",
            Notes = "1回限りの課題や分類不能な課題用。詳細はセッション記録の臨床メモに記述。",
            IsActive = true
        });
    }
}
