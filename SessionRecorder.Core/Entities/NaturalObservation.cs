using SessionRecorder.Core.Enums;

namespace SessionRecorder.Core.Entities;

public class NaturalObservation
{
    public int Id { get; set; }
    public DateTime Date { get; set; }

    public int ChildId { get; set; }
    public Child Child { get; set; } = null!;

    public ObservationType ObservationType { get; set; }
    public string? Situation { get; set; }
    public string? ObservedBehavior { get; set; }
    public ResponseResult? Result { get; set; }
    public string? Interpretation { get; set; }
    public string? NextVerification { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
