using System.ComponentModel.DataAnnotations;

namespace SessionRecorder.Core.Enums;

// このenumはSkillDomainエンティティ（テーブル）に移行済み。
// ProgramTypeとの整合性のために残しておく。
// 新しい領域追加はアプリ画面から行う。
[Obsolete("Use SkillDomain entity instead.")]
public enum SkillDomainEnum
{
    [Display(Name = "その他")] Other
}
