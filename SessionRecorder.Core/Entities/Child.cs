using SessionRecorder.Core.Enums;

namespace SessionRecorder.Core.Entities;

public class Child
{
    public int Id { get; set; }
    public string ChildCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Gender { get; set; }
    public string? BirthDate { get; set; }
    public string? PrimaryDiagnosis { get; set; }
    public OtaStage? OtaStage { get; set; }
    public DateTime? StartDate { get; set; }
    public string? CurrentTargetSkills { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SessionRecord> SessionRecords { get; set; } = [];
    public ICollection<NaturalObservation> NaturalObservations { get; set; } = [];
}
