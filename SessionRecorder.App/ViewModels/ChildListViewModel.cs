using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Converters;
using SessionRecorder.App.Services;
using SessionRecorder.Core.Entities;
using SessionRecorder.Core.Enums;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

// チェックボックス付きリストアイテム用ラッパー
public partial class SelectableChild : ObservableObject
{
    public Child Child { get; }
    [ObservableProperty] private bool _isChecked;
    public SelectableChild(Child child) => Child = child;
}

public partial class ChildListViewModel : ObservableObject
{
    private readonly IChildRepository _childRepo;
    private readonly ISessionRecordRepository _sessionRepo;
    private readonly INaturalObservationRepository _obsRepo;
    private readonly ExcelExportService _excelExport;

    public ObservableCollection<SelectableChild> SelectableChildren { get; } = [];
    public ObservableCollection<SessionRecord> ChildSessions { get; } = [];
    public ObservableCollection<NaturalObservation> ChildObservations { get; } = [];

    public List<EnumItem<OtaStage>> OtaStages { get; } = EnumHelper.GetItems<OtaStage>();

    public event Action<Child>? ChildSelected;

    [ObservableProperty]
    private SelectableChild? _selectedItem;

    [ObservableProperty]
    private Child? _selectedChild;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _showDetailPanel;

    [ObservableProperty]
    private int _selectedDetailTab;

    [ObservableProperty]
    private bool _hasCheckedChildren;

    // Edit fields
    [ObservableProperty] private string _editChildCode = "";
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string? _editGender;
    [ObservableProperty] private string? _editBirthDate;
    [ObservableProperty] private string? _editDiagnosis;
    [ObservableProperty] private OtaStage? _editOtaStage;
    [ObservableProperty] private DateTime? _editStartDate;
    [ObservableProperty] private string? _editTargetSkills;
    [ObservableProperty] private string? _editNotes;
    [ObservableProperty] private bool _isNewChild;

    public ChildListViewModel(
        IChildRepository childRepo,
        ISessionRecordRepository sessionRepo,
        INaturalObservationRepository obsRepo,
        ExcelExportService excelExport)
    {
        _childRepo = childRepo;
        _sessionRepo = sessionRepo;
        _obsRepo = obsRepo;
        _excelExport = excelExport;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        SelectableChildren.Clear();
        var children = await _childRepo.GetAllAsync();
        foreach (var c in children)
        {
            var item = new SelectableChild(c);
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SelectableChild.IsChecked))
                    HasCheckedChildren = SelectableChildren.Any(x => x.IsChecked);
            };
            SelectableChildren.Add(item);
        }
        HasCheckedChildren = false;
    }

    partial void OnSelectedItemChanged(SelectableChild? value)
    {
        SelectedChild = value?.Child;
    }

    async partial void OnSelectedChildChanged(Child? value)
    {
        if (value == null)
        {
            ShowDetailPanel = false;
            return;
        }

        ShowDetailPanel = true;
        IsEditing = false;
        ChildSelected?.Invoke(value);

        ChildSessions.Clear();
        ChildObservations.Clear();

        var sessions = await _sessionRepo.GetByChildIdAsync(value.Id);
        foreach (var s in sessions) ChildSessions.Add(s);

        var obs = await _obsRepo.GetByChildIdAsync(value.Id);
        foreach (var o in obs) ChildObservations.Add(o);
    }

    [RelayCommand]
    private void ToggleAllCheck()
    {
        bool allChecked = SelectableChildren.All(x => x.IsChecked);
        foreach (var item in SelectableChildren)
            item.IsChecked = !allChecked;
    }

    [RelayCommand]
    private async Task DeleteCheckedAsync()
    {
        var targets = SelectableChildren.Where(x => x.IsChecked).Select(x => x.Child).ToList();
        if (targets.Count == 0) return;

        var names = string.Join("\n", targets.Select(c => $"  {c.ChildCode}  {c.Name}"));
        var result = MessageBox.Show(
            $"以下の児童 {targets.Count}名 を削除します。\n\n{names}\n\nよろしいですか？",
            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        foreach (var child in targets)
            await _childRepo.DeleteAsync(child.Id);

        if (SelectedChild != null && targets.Any(c => c.Id == SelectedChild.Id))
        {
            SelectedChild = null;
            ShowDetailPanel = false;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private async Task NewChildAsync()
    {
        IsNewChild = true;
        IsEditing = true;
        ShowDetailPanel = true;
        EditChildCode = await _childRepo.GetNextCodeAsync();
        EditName = "";
        EditGender = null;
        EditBirthDate = null;
        EditDiagnosis = null;
        EditOtaStage = null;
        EditStartDate = DateTime.Today;
        EditTargetSkills = null;
        EditNotes = null;
    }

    [RelayCommand]
    private void EditChild()
    {
        if (SelectedChild == null) return;
        IsNewChild = false;
        IsEditing = true;
        EditChildCode = SelectedChild.ChildCode;
        EditName = SelectedChild.Name;
        EditGender = SelectedChild.Gender;
        EditBirthDate = SelectedChild.BirthDate;
        EditDiagnosis = SelectedChild.PrimaryDiagnosis;
        EditOtaStage = SelectedChild.OtaStage;
        EditStartDate = SelectedChild.StartDate;
        EditTargetSkills = SelectedChild.CurrentTargetSkills;
        EditNotes = SelectedChild.Notes;
    }

    [RelayCommand]
    private async Task SaveChildAsync()
    {
        if (IsNewChild)
        {
            var child = new Child
            {
                ChildCode = EditChildCode,
                Name = EditName,
                Gender = EditGender,
                BirthDate = EditBirthDate,
                PrimaryDiagnosis = EditDiagnosis,
                OtaStage = EditOtaStage,
                StartDate = EditStartDate,
                CurrentTargetSkills = EditTargetSkills,
                Notes = EditNotes
            };
            await _childRepo.AddAsync(child);
        }
        else if (SelectedChild != null)
        {
            SelectedChild.ChildCode = EditChildCode;
            SelectedChild.Name = EditName;
            SelectedChild.Gender = EditGender;
            SelectedChild.BirthDate = EditBirthDate;
            SelectedChild.PrimaryDiagnosis = EditDiagnosis;
            SelectedChild.OtaStage = EditOtaStage;
            SelectedChild.StartDate = EditStartDate;
            SelectedChild.CurrentTargetSkills = EditTargetSkills;
            SelectedChild.Notes = EditNotes;
            await _childRepo.UpdateAsync(SelectedChild);
        }

        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        if (IsNewChild) ShowDetailPanel = SelectedChild != null;
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (SelectedChild == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"{SelectedChild.ChildCode}_{SelectedChild.Name}_記録.xlsx",
            Filter = "Excel Files|*.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            _excelExport.ExportChildSessions(
                SelectedChild,
                ChildSessions.ToList(),
                ChildObservations.ToList(),
                dialog.FileName);
        }
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "児童マスタCSVを選択",
            Filter = "CSV Files|*.csv|All Files|*.*",
            DefaultExt = ".csv"
        };
        if (dialog.ShowDialog() != true) return;

        int imported = 0, skipped = 0;
        var errors = new List<string>();

        var otaMap = new Dictionary<string, OtaStage>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ⅰ"] = OtaStage.Stage1, ["1"] = OtaStage.Stage1,
            ["Ⅱ"] = OtaStage.Stage2, ["2"] = OtaStage.Stage2,
            ["Ⅲ-1"] = OtaStage.Stage3_1, ["3-1"] = OtaStage.Stage3_1,
            ["Ⅲ-2"] = OtaStage.Stage3_2, ["3-2"] = OtaStage.Stage3_2,
            ["Ⅳ-前"] = OtaStage.Stage4_Pre, ["4-前"] = OtaStage.Stage4_Pre,
            ["Ⅳ-後"] = OtaStage.Stage4_Post, ["4-後"] = OtaStage.Stage4_Post,
            ["未測定"] = OtaStage.NotAssessed, [""] = OtaStage.NotAssessed,
        };

        try
        {
            var lines = await File.ReadAllLinesAsync(dialog.FileName, System.Text.Encoding.UTF8);
            if (lines.Length == 0) return;

            int startLine = 0;
            var firstCell = lines[0].Split(',')[0].Trim().TrimStart('﻿');
            if (firstCell.Equals("ChildCode", StringComparison.OrdinalIgnoreCase)
                || firstCell == "児童コード") startLine = 1;

            var existingCodes = SelectableChildren
                .Select(x => x.Child.ChildCode)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (int i = startLine; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var cols = ParseCsvLine(line);
                if (cols.Length < 2) { errors.Add($"行{i + 1}: 列数不足"); continue; }

                var code = cols[0].Trim();
                var name = cols.Length > 1 ? cols[1].Trim() : "";
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                { errors.Add($"行{i + 1}: コードまたは氏名が空です"); continue; }

                if (existingCodes.Contains(code)) { skipped++; continue; }

                var child = new Child
                {
                    ChildCode = code,
                    Name = name,
                    Gender = cols.Length > 2 ? NullIfEmpty(cols[2]) : null,
                    BirthDate = cols.Length > 3 ? NullIfEmpty(cols[3]) : null,
                    PrimaryDiagnosis = cols.Length > 4 ? NullIfEmpty(cols[4]) : null,
                    OtaStage = cols.Length > 5 && otaMap.TryGetValue(cols[5].Trim(), out var stage) ? stage : null,
                    StartDate = cols.Length > 6 && DateTime.TryParse(cols[6].Trim(), out var sd) ? sd : null,
                    CurrentTargetSkills = cols.Length > 7 ? NullIfEmpty(cols[7]) : null,
                    Notes = cols.Length > 8 ? NullIfEmpty(cols[8]) : null,
                };
                await _childRepo.AddAsync(child);
                existingCodes.Add(code);
                imported++;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"読み込みエラー: {ex.Message}", "CSVインポート",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await LoadAsync();

        var msg = $"インポート完了: {imported}件追加、{skipped}件スキップ（コード重複）";
        if (errors.Count > 0) msg += $"\n\n警告:\n" + string.Join("\n", errors.Take(10));
        MessageBox.Show(msg, "CSVインポート", MessageBoxButton.OK,
            errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
