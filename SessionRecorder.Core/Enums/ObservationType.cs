using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

public enum ObservationType
{
    [Display(Name = "自然場面")] Natural,
    [Display(Name = "対話型学習")] Interactive,
    [Display(Name = "観察")] Observation
}
