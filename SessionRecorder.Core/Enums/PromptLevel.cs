using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

public enum PromptLevel
{
    [Display(Name = "独立")] Independent,
    [Display(Name = "言語")] Verbal,
    [Display(Name = "ジェスチャー")] Gestural,
    [Display(Name = "モデリング")] Modeling,
    [Display(Name = "身体ガイダンス")] Physical
}
