using SessionRecorder.Core.Enums;

namespace SessionRecorder.Core.Entities;

public class SessionRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }

    public int ChildId { get; set; }
    public Child Child { get; set; } = null!;

    public int ProgramId { get; set; }
    public InterventionProgram Program { get; set; } = null!;

    public int? TrialCount { get; set; }
    public int? CorrectCount { get; set; }

    public double? CorrectRate =>
        (TrialCount.HasValue && TrialCount.Value > 0 && CorrectCount.HasValue)
            ? (double)CorrectCount.Value / TrialCount.Value
            : null;

    public MasteryLevel? MasteryLevel { get; set; }
    public PromptLevel? PromptLevel { get; set; }

    public string? ClinicalNote { get; set; }
    public string? Hypothesis { get; set; }
    public string? NextAction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
