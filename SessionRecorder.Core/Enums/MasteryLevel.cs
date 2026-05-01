using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

public enum MasteryLevel
{
    [Display(Name = "マスター")] Mastered,
    [Display(Name = "獲得中")] Acquiring,
    [Display(Name = "般化中")] Generalizing,
    [Display(Name = "未獲得")] NotAcquired
}
