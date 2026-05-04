using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Converters;
using SessionRecorder.Core.Entities;
using SessionRecorder.Core.Enums;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class SessionEntryViewModel : ObservableObject, IUnsavedChangesGuard
{
    private readonly IChildRepository          _childRepo;
    private readonly IProgramRepository        _programRepo;
    private readonly ISessionRecordRepository  _sessionRepo;

    // ── dirty 検知 ────────────────────────────────────────────────
    private bool _suppressDirty;

    private static readonly HashSet<string> DirtyProps = new()
    {
        nameof(SelectedChild), nameof(SelectedProgram), nameof(Date),
        nameof(TrialCount), nameof(CorrectCount),
        nameof(MasteryLevel), nameof(PromptLevel),
        nameof(ClinicalNote), nameof(Hypothesis), nameof(NextAction),
    };

    [ObservableProperty] private bool _hasUnsavedChanges;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (!_suppressDirty && DirtyProps.Contains(e.PropertyName ?? ""))
            HasUnsavedChanges = true;
    }

    public void DiscardChanges() => HasUnsavedChanges = false;

    // ── コレクション・enum ──────────────────────────────────────────
    public ObservableCollection<Child>               Children      { get; } = [];
    public ObservableCollection<InterventionProgram> Programs      { get; } = [];
    public ObservableCollection<SessionRecord>       RecentRecords { get; } = [];

    public List<EnumItem<MasteryLevel>> MasteryLevels { get; } = EnumHelper.GetItems<MasteryLevel>();
    public List<EnumItem<PromptLevel>>  PromptLevels  { get; } = EnumHelper.GetItems<PromptLevel>();

    // ── フォームフィールド ──────────────────────────────────────────
    [ObservableProperty] private Child?               _selectedChild;
    [ObservableProperty] private InterventionProgram? _selectedProgram;
    [ObservableProperty] private DateTime             _date = DateTime.Today;
    [ObservableProperty] private int?                 _trialCount;
    [ObservableProperty] private int?                 _correctCount;
    [ObservableProperty] private MasteryLevel?        _masteryLevel;
    [ObservableProperty] private PromptLevel?         _promptLevel;
    [ObservableProperty] private string?              _clinicalNote;
    [ObservableProperty] private string?              _hypothesis;
    [ObservableProperty] private string?              _nextAction;
    [ObservableProperty] private string               _saveMessage = "";
    [ObservableProperty] private bool                 _isEditMode;
    [ObservableProperty] private int                  _editingRecordId;

    // ── 試行数・正答数の整合性チェック ──────────────────────────────
    /// <summary>正反応数が試行数を超えている場合にエラーメッセージを返す</summary>
    public string CountError =>
        TrialCount.HasValue && CorrectCount.HasValue && CorrectCount.Value > TrialCount.Value
            ? "⚠ 正反応数が試行数を超えています"
            : "";

    public bool IsCountValid =>
        !(TrialCount.HasValue && CorrectCount.HasValue && CorrectCount.Value > TrialCount.Value);

    public string CorrectRateDisplay =>
        (TrialCount.HasValue && TrialCount > 0 && CorrectCount.HasValue && IsCountValid)
            ? $"{(double)CorrectCount.Value / TrialCount.Value:P0}"
            : "—";

    partial void OnTrialCountChanged(int? value)
    {
        OnPropertyChanged(nameof(CorrectRateDisplay));
        OnPropertyChanged(nameof(CountError));
        OnPropertyChanged(nameof(IsCountValid));
    }

    partial void OnCorrectCountChanged(int? value)
    {
        OnPropertyChanged(nameof(CorrectRateDisplay));
        OnPropertyChanged(nameof(CountError));
        OnPropertyChanged(nameof(IsCountValid));
    }

    // セッション一覧から遷移時、Load後に適用する編集レコード
    private SessionRecord? _pendingEditRecord;

    public SessionEntryViewModel(
        IChildRepository childRepo,
        IProgramRepository programRepo,
        ISessionRecordRepository sessionRepo)
    {
        _childRepo   = childRepo;
        _programRepo = programRepo;
        _sessionRepo = sessionRepo;
    }

    public void PrepareEdit(SessionRecord record) => _pendingEditRecord = record;

    [RelayCommand]
    private async Task LoadAsync()
    {
        _suppressDirty = true;
        try
        {
            Children.Clear();
            Programs.Clear();

            var children = await _childRepo.GetAllAsync();
            foreach (var c in children) Children.Add(c);

            var programs = await _programRepo.GetAllAsync();
            foreach (var p in programs) Programs.Add(p);

            if (_pendingEditRecord != null)
            {
                EditRecordCommand.Execute(_pendingEditRecord);
                _pendingEditRecord = null;
            }
        }
        finally { _suppressDirty = false; }
    }

    async partial void OnSelectedChildChanged(Child? value)
    {
        if (value == null) return;
        RecentRecords.Clear();
        var records = await _sessionRepo.GetByChildIdAsync(value.Id);
        foreach (var r in records.Take(10)) RecentRecords.Add(r);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChild == null || SelectedProgram == null)
        {
            SaveMessage = "児童とプログラムを選択してください";
            return;
        }

        // ▼ 正答数 > 試行数 は保存ブロック
        if (!IsCountValid)
        {
            SaveMessage = "正反応数が試行数を超えています。入力値を確認してください";
            return;
        }

        if (IsEditMode)
        {
            var record = await _sessionRepo.GetByIdAsync(EditingRecordId);
            if (record != null)
            {
                record.Date         = Date;
                record.ChildId      = SelectedChild.Id;
                record.ProgramId    = SelectedProgram.Id;
                record.TrialCount   = TrialCount;
                record.CorrectCount = CorrectCount;
                record.MasteryLevel = MasteryLevel;
                record.PromptLevel  = PromptLevel;
                record.ClinicalNote = ClinicalNote;
                record.Hypothesis   = Hypothesis;
                record.NextAction   = NextAction;
                await _sessionRepo.UpdateAsync(record);
                SaveMessage = $"記録を更新しました（ID: {record.Id}）";
            }
            IsEditMode = false;
        }
        else
        {
            var record = new SessionRecord
            {
                Date         = Date,
                ChildId      = SelectedChild.Id,
                ProgramId    = SelectedProgram.Id,
                TrialCount   = TrialCount,
                CorrectCount = CorrectCount,
                MasteryLevel = MasteryLevel,
                PromptLevel  = PromptLevel,
                ClinicalNote = ClinicalNote,
                Hypothesis   = Hypothesis,
                NextAction   = NextAction
            };
            await _sessionRepo.AddAsync(record);
            SaveMessage = $"保存しました（{SelectedChild.ChildCode} / {SelectedProgram.ProgramName}）";
        }

        if (SelectedChild != null)
        {
            RecentRecords.Clear();
            var records = await _sessionRepo.GetByChildIdAsync(SelectedChild.Id);
            foreach (var r in records.Take(10)) RecentRecords.Add(r);
        }

        ClearForm();
    }

    [RelayCommand]
    private void EditRecord(SessionRecord record)
    {
        // ▼ 入力中フォームを上書きする前に確認（直近一覧の編集ボタン対策）
        if (HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "入力中のデータが保存されていません。\n別のセッションを読み込むと現在の内容が失われます。\n\n続けますか？",
                "未保存の変更があります",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Cancel);
            if (result != MessageBoxResult.OK) return;
        }

        _suppressDirty = true;
        try
        {
            IsEditMode      = true;
            EditingRecordId = record.Id;
            SelectedChild   = Children.FirstOrDefault(c => c.Id == record.ChildId);
            SelectedProgram = Programs.FirstOrDefault(p => p.Id == record.ProgramId);
            Date            = record.Date;
            TrialCount      = record.TrialCount;
            CorrectCount    = record.CorrectCount;
            MasteryLevel    = record.MasteryLevel;
            PromptLevel     = record.PromptLevel;
            ClinicalNote    = record.ClinicalNote;
            Hypothesis      = record.Hypothesis;
            NextAction      = record.NextAction;
        }
        finally { _suppressDirty = false; }

        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(SessionRecord record)
    {
        // ▼ 削除確認（デフォルト：キャンセル）
        var result = MessageBox.Show(
            $"{record.Date:yyyy/MM/dd}  {record.Program?.ProgramName}\nこのセッション記録を削除しますか？\nこの操作は元に戻せません。",
            "削除の確認",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel);
        if (result != MessageBoxResult.OK) return;

        await _sessionRepo.DeleteAsync(record.Id);
        RecentRecords.Remove(record);
        SaveMessage = "記録を削除しました";
    }

    [RelayCommand]
    private void ClearForm()
    {
        _suppressDirty = true;
        try
        {
            TrialCount   = null;
            CorrectCount = null;
            MasteryLevel = null;
            PromptLevel  = null;
            ClinicalNote = null;
            Hypothesis   = null;
            NextAction   = null;
            IsEditMode   = false;
        }
        finally
        {
            _suppressDirty    = false;
            HasUnsavedChanges = false;
        }
    }
}
