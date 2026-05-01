namespace SessionRecorder.Core.Entities;

public class SkillDomain
{
    public int Id { get; set; }
    public string DomainCode { get; set; } = "";   // D001, D002, ...
    public string DomainName { get; set; } = "";   // マンド、社会的スキル、等
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InterventionProgram> Programs { get; set; } = [];
}
