using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SessionRecorder.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string  _statusMessage = "準備完了";

    public ChildListViewModel        ChildListVm        { get; }
    public ProgramListViewModel      ProgramListVm      { get; }
    public SessionEntryViewModel     SessionEntryVm     { get; }
    public ObservationEntryViewModel ObservationEntryVm { get; }
    public SearchViewModel           SearchVm           { get; }
    public SkillDomainListViewModel  SkillDomainListVm  { get; }
    public SessionListViewModel      SessionListVm      { get; }
    public ObservationListViewModel  ObservationListVm  { get; }
    public ProgressGraphViewModel    ProgressGraphVm    { get; }
    public BackupViewModel           BackupVm           { get; }

    public MainViewModel(
        ChildListViewModel        childListVm,
        ProgramListViewModel      programListVm,
        SessionEntryViewModel     sessionEntryVm,
        ObservationEntryViewModel observationEntryVm,
        SearchViewModel           searchVm,
        SkillDomainListViewModel  skillDomainListVm,
        SessionListViewModel      sessionListVm,
        ObservationListViewModel  observationListVm,
        ProgressGraphViewModel    progressGraphVm,
        BackupViewModel           backupVm)
    {
        ChildListVm        = childListVm;
        ProgramListVm      = programListVm;
        SessionEntryVm     = sessionEntryVm;
        ObservationEntryVm = observationEntryVm;
        SearchVm           = searchVm;
        SkillDomainListVm  = skillDomainListVm;
        SessionListVm      = sessionListVm;
        ObservationListVm  = observationListVm;
        ProgressGraphVm    = progressGraphVm;
        BackupVm           = backupVm;

        ChildListVm.ChildSelected += child =>
            StatusMessage = $"{child.ChildCode} {child.Name} を選択中";

        // セッション一覧からの編集遷移（一覧は dirty にならないので警告不要）
        SessionListVm.EditSessionRequested += record =>
        {
            SessionEntryVm.PrepareEdit(record);
            CurrentView = SessionEntryVm;
        };

        // 自然場面一覧からの編集遷移
        ObservationListVm.EditObservationRequested += obs =>
        {
            ObservationEntryVm.PrepareEdit(obs);
            CurrentView = ObservationEntryVm;
        };

        CurrentView = ChildListVm;
    }

    // ─────────────────────────────────────────────────────────────────
    // 未保存チェック付き画面遷移
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 現在の画面に未保存の変更がある場合はダイアログを表示し、
    /// ユーザーが「移動する」を選んだ場合のみ遷移する。
    /// </summary>
    private bool TryNavigateTo(object targetVm)
    {
        // 同じ画面への遷移は無視
        if (ReferenceEquals(CurrentView, targetVm)) return true;

        if (CurrentView is IUnsavedChangesGuard guard && guard.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "入力中のデータが保存されていません。\n画面を移動すると変更が失われます。\n\n移動しますか？",
                "未保存の変更があります",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Cancel);   // デフォルトは「キャンセル」（誤操作防止）

            if (result != MessageBoxResult.OK) return false;

            // 破棄を確定：ダーティフラグをリセット
            guard.DiscardChanges();
        }

        CurrentView = targetVm;
        return true;
    }

    // ─────────────────────────────────────────────────────────────────
    // ナビゲーションコマンド（すべて TryNavigateTo を経由する）
    // ─────────────────────────────────────────────────────────────────

    [RelayCommand] private void NavigateToChildren()         => TryNavigateTo(ChildListVm);
    [RelayCommand] private void NavigateToPrograms()         => TryNavigateTo(ProgramListVm);
    [RelayCommand] private void NavigateToSessionEntry()     => TryNavigateTo(SessionEntryVm);
    [RelayCommand] private void NavigateToObservationEntry() => TryNavigateTo(ObservationEntryVm);
    [RelayCommand] private void NavigateToSearch()           => TryNavigateTo(SearchVm);
    [RelayCommand] private void NavigateToSkillDomains()     => TryNavigateTo(SkillDomainListVm);
    [RelayCommand] private void NavigateToSessionList()      => TryNavigateTo(SessionListVm);
    [RelayCommand] private void NavigateToObservationList()  => TryNavigateTo(ObservationListVm);
    [RelayCommand] private void NavigateToProgressGraph()    => TryNavigateTo(ProgressGraphVm);
    [RelayCommand] private void NavigateToBackup()           => TryNavigateTo(BackupVm);
}
