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

public partial class SessionListViewModel : ObservableObject
{
    private readonly ISessionRecordRepository _sessionRepo;
    private readonly IChildRepository _childRepo;
    private readonly IFileDialogService _fileDialog;

    private List<SessionRecord> _allRecords = [];

    /// <summary>セッション一覧から編集画面への遷移要求（MainViewModel が購読）</summary>
    public event Action<SessionRecord>? EditSessionRequested;

    public ObservableCollection<SessionRecord> DisplayRecords { get; } = [];
    public ObservableCollection<Child?> Children { get; } = [];

    [ObservableProperty] private Child? _selectedChild;
    [ObservableProperty] private DateTime? _dateFrom;
    [ObservableProperty] private DateTime? _dateTo;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isLoading;

    public SessionListViewModel(
        ISessionRecordRepository sessionRepo,
        IChildRepository childRepo,
        IFileDialogService fileDialog)
    {
        _sessionRepo = sessionRepo;
        _childRepo = childRepo;
        _fileDialog = fileDialog;
    }

    [RelayCommand]
    private void RequestEdit(SessionRecord record) =>
        EditSessionRequested?.Invoke(record);

    [RelayCommand]
    private async Task DeleteSessionAsync(SessionRecord record)
    {
        var result = MessageBox.Show(
            $"{record.Date:yyyy/MM/dd}  {record.Child?.Name} / {record.Program?.ProgramName}\nこのセッション記録を削除しますか？",
            "削除の確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        if (result != MessageBoxResult.OK) return;

        await _sessionRepo.DeleteAsync(record.Id);
        _allRecords.Remove(record);
        DisplayRecords.Remove(record);
        StatusText = $"削除しました　{DisplayRecords.Count} 件";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allRecords = await _sessionRepo.GetAllAsync();

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

        if (SelectedChild != null)
            filtered = filtered.Where(s => s.ChildId == SelectedChild.Id);

        if (DateFrom.HasValue)
            filtered = filtered.Where(s => s.Date.Date >= DateFrom.Value.Date);

        if (DateTo.HasValue)
            filtered = filtered.Where(s => s.Date.Date <= DateTo.Value.Date);

        DisplayRecords.Clear();
        foreach (var r in filtered)
            DisplayRecords.Add(r);

        StatusText = $"{DisplayRecords.Count} 件";
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedChild = null;
        DateFrom = null;
        DateTo = null;
        ApplyFilter();
    }

    [RelayCommand]
    private void ExportCsv()
    {
        if (DisplayRecords.Count == 0)
        {
            StatusText = "エクスポートするデータがありません";
            return;
        }

        var defaultName = $"sessions_{DateTime.Now:yyyyMMdd}";
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

    private static string BuildCsv(IEnumerable<SessionRecord> records)
    {
        var sb = new StringBuilder();
        // Header
        sb.AppendLine("日付,児童コード,児童名,プログラムコード,課題名,領域,試行数,正答数,正答率,習得レベル,プロンプトレベル,臨床メモ,仮説,次回");

        foreach (var r in records)
        {
            var rate = r.CorrectRate.HasValue ? $"{r.CorrectRate.Value:P0}" : "";
            var mastery = r.MasteryLevel.HasValue ? EnumHelper.GetDisplayName(r.MasteryLevel.Value) : "";
            var prompt = r.PromptLevel.HasValue ? EnumHelper.GetDisplayName(r.PromptLevel.Value) : "";

            sb.AppendLine(string.Join(",",
                Q(r.Date.ToString("yyyy/MM/dd")),
                Q(r.Child?.ChildCode ?? ""),
                Q(r.Child?.Name ?? ""),
                Q(r.Program?.ProgramCode ?? ""),
                Q(r.Program?.ProgramName ?? ""),
                Q(r.Program?.Domain?.DomainName ?? ""),
                r.TrialCount?.ToString() ?? "",
                r.CorrectCount?.ToString() ?? "",
                Q(rate),
                Q(mastery),
                Q(prompt),
                Q(r.ClinicalNote ?? ""),
                Q(r.Hypothesis ?? ""),
                Q(r.NextAction ?? "")
            ));
        }

        return sb.ToString();
    }

    // CSV フィールドのクォート処理
    private static string Q(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
