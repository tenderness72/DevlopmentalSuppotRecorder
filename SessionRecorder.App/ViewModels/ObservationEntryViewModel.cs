using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Converters;
using SessionRecorder.Core.Entities;
using SessionRecorder.Core.Enums;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class ObservationEntryViewModel : ObservableObject
{
    private readonly IChildRepository _childRepo;
    private readonly INaturalObservationRepository _obsRepo;

    // 一覧画面から編集要求があったとき、LoadAsync 完了まで保持する
    private NaturalObservation? _pendingEditObs;

    public ObservableCollection<Child> Children { get; } = [];
    public ObservableCollection<NaturalObservation> RecentObservations { get; } = [];

    public List<EnumItem<ObservationType>> ObservationTypes { get; } = EnumHelper.GetItems<ObservationType>();
    public List<EnumItem<ResponseResult>> ResponseResults  { get; } = EnumHelper.GetItems<ResponseResult>();

    [ObservableProperty] private Child?          _selectedChild;
    [ObservableProperty] private DateTime        _date = DateTime.Today;
    [ObservableProperty] private ObservationType _observationType = ObservationType.Natural;
    [ObservableProperty] private string?         _situation;
    [ObservableProperty] private string?         _observedBehavior;
    [ObservableProperty] private ResponseResult? _result;
    [ObservableProperty] private string?         _interpretation;
    [ObservableProperty] private string?         _nextVerification;
    [ObservableProperty] private string          _saveMessage = "";
    [ObservableProperty] private bool            _isEditMode;
    [ObservableProperty] private int             _editingObsId;

    public ObservationEntryViewModel(
        IChildRepository childRepo,
        INaturalObservationRepository obsRepo)
    {
        _childRepo = childRepo;
        _obsRepo   = obsRepo;
    }

    /// <summary>一覧画面から「編集」ボタンが押されたとき呼ばれる（LoadAsync の前に呼ぶ）</summary>
    public void PrepareEdit(NaturalObservation obs) => _pendingEditObs = obs;

    [RelayCommand]
    private async Task LoadAsync()
    {
        Children.Clear();
        var children = await _childRepo.GetAllAsync();
        foreach (var c in children) Children.Add(c);

        // 一覧から編集要求があれば、コレクション準備後に適用
        if (_pendingEditObs != null)
        {
            EditObservation(_pendingEditObs);
            _pendingEditObs = null;
        }
    }

    async partial void OnSelectedChildChanged(Child? value)
    {
        if (value == null) return;
        RecentObservations.Clear();
        var obs = await _obsRepo.GetByChildIdAsync(value.Id);
        foreach (var o in obs.Take(10)) RecentObservations.Add(o);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedChild == null)
        {
            SaveMessage = "児童を選択してください";
            return;
        }

        if (IsEditMode)
        {
            var obs = await _obsRepo.GetByIdAsync(EditingObsId);
            if (obs != null)
            {
                obs.Date             = Date;
                obs.ChildId          = SelectedChild.Id;
                obs.ObservationType  = ObservationType;
                obs.Situation        = Situation;
                obs.ObservedBehavior = ObservedBehavior;
                obs.Result           = Result;
                obs.Interpretation   = Interpretation;
                obs.NextVerification = NextVerification;
                await _obsRepo.UpdateAsync(obs);
                SaveMessage = "記録を更新しました";
            }
            IsEditMode = false;
        }
        else
        {
            var obs = new NaturalObservation
            {
                Date             = Date,
                ChildId          = SelectedChild.Id,
                ObservationType  = ObservationType,
                Situation        = Situation,
                ObservedBehavior = ObservedBehavior,
                Result           = Result,
                Interpretation   = Interpretation,
                NextVerification = NextVerification
            };
            await _obsRepo.AddAsync(obs);
            SaveMessage = $"保存しました（{SelectedChild.ChildCode}）";
        }

        if (SelectedChild != null)
        {
            RecentObservations.Clear();
            var records = await _obsRepo.GetByChildIdAsync(SelectedChild.Id);
            foreach (var o in records.Take(10)) RecentObservations.Add(o);
        }

        ClearForm();
    }

    [RelayCommand]
    private void EditObservation(NaturalObservation obs)
    {
        IsEditMode       = true;
        EditingObsId     = obs.Id;
        SelectedChild    = Children.FirstOrDefault(c => c.Id == obs.ChildId);
        Date             = obs.Date;
        ObservationType  = obs.ObservationType;
        Situation        = obs.Situation;
        ObservedBehavior = obs.ObservedBehavior;
        Result           = obs.Result;
        Interpretation   = obs.Interpretation;
        NextVerification = obs.NextVerification;
    }

    [RelayCommand]
    private async Task DeleteObservationAsync(NaturalObservation obs)
    {
        await _obsRepo.DeleteAsync(obs.Id);
        RecentObservations.Remove(obs);
        SaveMessage = "記録を削除しました";
    }

    [RelayCommand]
    private void ClearForm()
    {
        Situation        = null;
        ObservedBehavior = null;
        Result           = null;
        Interpretation   = null;
        NextVerification = null;
        IsEditMode       = false;
    }
}
