using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

public enum OtaStage
{
    [Display(Name = "Ⅰ")] Stage1,
    [Display(Name = "Ⅱ")] Stage2,
    [Display(Name = "Ⅲ-1")] Stage3_1,
    [Display(Name = "Ⅲ-2")] Stage3_2,
    [Display(Name = "Ⅳ-前")] Stage4_Pre,
    [Display(Name = "Ⅳ-後")] Stage4_Post,
    [Display(Name = "未測定")] NotAssessed
}
