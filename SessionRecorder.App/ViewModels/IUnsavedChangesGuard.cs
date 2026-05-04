namespace SessionRecorder.App.ViewModels;

/// <summary>
/// 未保存の変更がある可能性のある画面 ViewModel に実装するインターフェース。
/// MainViewModel が画面遷移前にチェックする。
/// </summary>
public interface IUnsavedChangesGuard
{
    /// <summary>ユーザーが入力・変更したが未保存の内容がある場合 true</summary>
    bool HasUnsavedChanges { get; }

    /// <summary>「移動する」と確定されたとき呼ばれる（ダーティフラグをリセット）</summary>
    void DiscardChanges();
}
