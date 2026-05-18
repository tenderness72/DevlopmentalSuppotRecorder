using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

/// <summary>
/// プロンプトレベル（侵襲性の低い順 / Least-to-Most）
/// ※既存レコード互換のため、enum 名（Independent / Verbal / Gestural / Modeling / Physical）は据え置き。
/// </summary>
public enum PromptLevel
{
    [Display(Name = "独立")]       Independent,
    [Display(Name = "視覚")]       Visual,
    [Display(Name = "言語")]       Verbal,
    [Display(Name = "身振り")]     Gestural,
    [Display(Name = "モデリング")] Modeling,
    [Display(Name = "身体")]       Physical
}
