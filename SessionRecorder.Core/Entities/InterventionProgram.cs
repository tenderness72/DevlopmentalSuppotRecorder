namespace SessionRecorder.Core.Entities;

public class InterventionProgram
{
    public int Id { get; set; }
    public string ProgramCode { get; set; } = "";
    public string ProgramName { get; set; } = "";

    // 領域はテーブル参照
    public int DomainId { get; set; }
    public SkillDomain Domain { get; set; } = null!;

    // 型もテーブル参照（マスタ）
    public int ProgramTypeId { get; set; }
    public ProgramTypeMaster ProgramType { get; set; } = null!;

    public string? MasteryCriteria { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SessionRecord> SessionRecords { get; set; } = [];
}
