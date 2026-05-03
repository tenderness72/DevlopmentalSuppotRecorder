using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Converters;
using SessionRecorder.App.Services;
using SessionRecorder.Core.Entities;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class ObservationListViewModel : ObservableObject
{
    private readonly INaturalObservationRepository _obsRepo;
    private readonly IChildRepository              _childRepo;
    private readonly IFileDialogService            _fileDialog;

    private List<NaturalObservation> _allRecords = [];

    /// <summary>一覧から編集画面への遷移要求（MainViewModel が購読）</summary>
    public event Action<NaturalObservation>? EditObservationRequested;

    public ObservableCollection<Child?>             Children       { get; } = [];
    public ObservableCollection<NaturalObservation> DisplayRecords { get; } = [];

    [ObservableProperty] private Child?    _filterChild;
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;
    [ObservableProperty] private string    _statusText = "";
    [ObservableProperty] private bool      _isLoading;

    public ObservationListViewModel(
        INaturalObservationRepository obsRepo,
        IChildRepository childRepo,
        IFileDialogService fileDialog)
    {
        _obsRepo    = obsRepo;
        _childRepo  = childRepo;
        _fileDialog = fileDialog;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allRecords = await _obsRepo.GetAllAsync();

            Children.Clear();
            Children.Add(null); // 「すべて」
            var children = await _childRepo.GetAllAsync(activeOnly: false);
            foreach (var c in children.OrderBy(c => c.ChildCode))
                Children.Add(c);

            ApplyFilter();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var filtered = _allRecords.AsEnumerable();

        if (FilterChild != null)
            filtered = filtered.Where(r => r.ChildId == FilterChild.Id);
        if (FilterFrom.HasValue)
            filtered = filtered.Where(r => r.Date.Date >= FilterFrom.Value.Date);
        if (FilterTo.HasValue)
            filtered = filtered.Where(r => r.Date.Date <= FilterTo.Value.Date);

        DisplayRecords.Clear();
        foreach (var r in filtered) DisplayRecords.Add(r);
        StatusText = $"{DisplayRecords.Count} 件";
    }

    [RelayCommand]
    private void ClearFilter()
    {
        FilterChild = null;
        FilterFrom  = null;
        FilterTo    = null;
        ApplyFilter();
    }

    [RelayCommand]
    private void RequestEdit(NaturalObservation obs) =>
        EditObservationRequested?.Invoke(obs);

    [RelayCommand]
    private async Task DeleteAsync(NaturalObservation obs)
    {
        var result = MessageBox.Show(
            $"{obs.Date:yyyy/MM/dd}  {obs.Child?.Name}\n「{Truncate(obs.ObservedBehavior, 30)}」\nこの自然場面記録を削除しますか？",
            "削除の確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        if (result != MessageBoxResult.OK) return;

        await _obsRepo.DeleteAsync(obs.Id);
        _allRecords.Remove(obs);
        DisplayRecords.Remove(obs);
        StatusText = $"削除しました　{DisplayRecords.Count} 件";
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (DisplayRecords.Count == 0)
        {
            StatusText = "出力するデータがありません";
            return;
        }

        var defaultName = FilterChild != null
            ? $"obs_{FilterChild.ChildCode}_{DateTime.Today:yyyyMMdd}"
            : $"obs_all_{DateTime.Today:yyyyMMdd}";

        var path = _fileDialog.ShowSaveCsvDialog(defaultName);
        if (path == null) return;

        try
        {
            var csv = BuildCsv(DisplayRecords);
            File.WriteAllText(path, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            StatusText = $"保存しました: {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            StatusText = $"エラー: {ex.Message}";
        }
    }

    private static string BuildCsv(IEnumerable<NaturalObservation> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("日付,児童コード,児童名,記録タイプ,状況,観察された行動,結果,臨床的解釈,次回検証課題");

        foreach (var o in records)
        {
            sb.AppendLine(string.Join(",",
                Q(o.Date.ToString("yyyy/MM/dd")),
                Q(o.Child?.ChildCode ?? ""),
                Q(o.Child?.Name ?? ""),
                Q(EnumHelper.GetDisplayName(o.ObservationType)),
                Q(o.Situation    ?? ""),
                Q(o.ObservedBehavior ?? ""),
                Q(o.Result.HasValue ? EnumHelper.GetDisplayName(o.Result.Value) : ""),
                Q(o.Interpretation   ?? ""),
                Q(o.NextVerification ?? "")
            ));
        }

        return sb.ToString();
    }

    private static string Q(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string Truncate(string? s, int max) =>
        s == null ? "" : (s.Length <= max ? s : s[..max] + "…");
}
