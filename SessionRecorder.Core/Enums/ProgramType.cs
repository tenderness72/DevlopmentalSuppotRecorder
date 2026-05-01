using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

public enum ProgramType
{
    [Display(Name = "構造化WS")] StructuredWorksheet,
    [Display(Name = "ロールプレイ")] RolePlay,
    [Display(Name = "直接教示")] DirectInstruction
}
