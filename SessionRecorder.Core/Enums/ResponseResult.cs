using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

public enum ResponseResult
{
    [Display(Name = "正反応")] Correct,
    [Display(Name = "誤反応")] Incorrect,
    [Display(Name = "判定不能")] Indeterminate
}
