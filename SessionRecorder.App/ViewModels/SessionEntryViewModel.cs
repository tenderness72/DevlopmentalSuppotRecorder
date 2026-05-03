using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Converters;
using SessionRecorder.Core.Entities;
using SessionRecorder.Core.Enums;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class SessionEntryViewModel : ObservableObject
{
    private readonly IChildRepository _childRepo;
    private readonly IProgramRepository _programRepo;
    private readonly ISessionRecordRepository _sessionRepo;

    public ObservableCollection<Child> Children { get; } = [];
    public ObservableCollection<InterventionProgram> Programs { get; } = [];
    public ObservableCollection<SessionRecord> RecentRecords { get; } = [];

    public List<EnumItem<MasteryLevel>> MasteryLevels { get; } = EnumHelper.GetItems<MasteryLevel>();
    public List<EnumItem<PromptLevel>> PromptLevels { get; } = EnumHelper.GetItems<PromptLevel>();

    [ObservableProperty] private Child? _selectedChild;
    [ObservableProperty] private InterventionProgram? _selectedProgram;
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private int? _trialCount;
    [ObservableProperty] private int? _correctCount;
    [ObservableProperty] private MasteryLevel? _masteryLevel;
    [ObservableProperty] private PromptLevel? _promptLevel;
    [ObservableProperty] private string? _clinicalNote;
    [ObservableProperty] private string? _hypothesis;
    [ObservableProperty] private string? _nextAction;
    [ObservableProperty] private string _saveMessage = "";
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private int _editingRecordId;

    // セッション一覧から遷移時、Load後に適用する編集レコード
    private SessionRecord? _pendingEditRecord;

    public string CorrectRateDisplay =>
        (TrialCount.HasValue && TrialCount > 0 && CorrectCount.HasValue)
            ? $"{(double)CorrectCount.Value / TrialCount.Value:P0}"
            : "—";

    public SessionEntryViewModel(
        IChildRepository childRepo,
        IProgramRepository programRepo,
        ISessionRecordRepository sessionRepo)
    {
        _childRepo = childRepo;
        _programRepo = programRepo;
        _sessionRepo = sessionRepo;
    }

    partial void OnTrialCountChanged(int? value) => OnPropertyChanged(nameof(CorrectRateDisplay));
    partial void OnCorrectCountChanged(int? value) => OnPropertyChanged(nameof(CorrectRateDisplay));

    /// <summary>セッション一覧からの遷移時に呼ぶ。Load完了後にフォームを自動セットする。</summary>
    public void PrepareEdit(SessionRecord record) => _pendingEditRecord = record;

    [RelayCommand]
    private async Task LoadAsync()
    {
        Children.Clear();
        Programs.Clear();

        var children = await _childRepo.GetAllAsync();
        foreach (var c in children) Children.Add(c);

        var programs = await _programRepo.GetAllAsync();
        foreach (var p in programs) Programs.Add(p);

        // 一覧画面からの編集遷移：コレクション確定後に適用
        if (_pendingEditRecord != null)
        {
            EditRecordCommand.Execute(_pendingEditRecord);
            _pendingEditRecord = null;
        }
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

        if (IsEditMode)
        {
            var record = await _sessionRepo.GetByIdAsync(EditingRecordId);
            if (record != null)
            {
                record.Date = Date;
                record.ChildId = SelectedChild.Id;
                record.ProgramId = SelectedProgram.Id;
                record.TrialCount = TrialCount;
                record.CorrectCount = CorrectCount;
                record.MasteryLevel = MasteryLevel;
                record.PromptLevel = PromptLevel;
                record.ClinicalNote = ClinicalNote;
                record.Hypothesis = Hypothesis;
                record.NextAction = NextAction;
                await _sessionRepo.UpdateAsync(record);
                SaveMessage = $"記録を更新しました（ID: {record.Id}）";
            }
            IsEditMode = false;
        }
        else
        {
            var record = new SessionRecord
            {
                Date = Date,
                ChildId = SelectedChild.Id,
                ProgramId = SelectedProgram.Id,
                TrialCount = TrialCount,
                CorrectCount = CorrectCount,
                MasteryLevel = MasteryLevel,
                PromptLevel = PromptLevel,
                ClinicalNote = ClinicalNote,
                Hypothesis = Hypothesis,
                NextAction = NextAction
            };
            await _sessionRepo.AddAsync(record);
            SaveMessage = $"保存しました（{SelectedChild.ChildCode} / {SelectedProgram.ProgramName}）";
        }

        // Refresh recent records
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
        IsEditMode = true;
        EditingRecordId = record.Id;
        SelectedChild = Children.FirstOrDefault(c => c.Id == record.ChildId);
        SelectedProgram = Programs.FirstOrDefault(p => p.Id == record.ProgramId);
        Date = record.Date;
        TrialCount = record.TrialCount;
        CorrectCount = record.CorrectCount;
        MasteryLevel = record.MasteryLevel;
        PromptLevel = record.PromptLevel;
        ClinicalNote = record.ClinicalNote;
        Hypothesis = record.Hypothesis;
        NextAction = record.NextAction;
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(SessionRecord record)
    {
        await _sessionRepo.DeleteAsync(record.Id);
        RecentRecords.Remove(record);
        SaveMessage = "記録を削除しました";
    }

    [RelayCommand]
    private void ClearForm()
    {
        TrialCount = null;
        CorrectCount = null;
        MasteryLevel = null;
        PromptLevel = null;
        ClinicalNote = null;
        Hypothesis = null;
        NextAction = null;
        IsEditMode = false;
    }
}
