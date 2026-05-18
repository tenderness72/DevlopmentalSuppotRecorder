using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.Core.Entities;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class ProgramTypeListViewModel : ObservableObject
{
    private readonly IProgramTypeRepository _typeRepo;

    public ObservableCollection<ProgramTypeMaster> Types { get; } = [];

    [ObservableProperty] private ProgramTypeMaster? _selectedType;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNewType;
    [ObservableProperty] private string _editTypeCode = "";
    [ObservableProperty] private string _editTypeName = "";
    [ObservableProperty] private string? _editNotes;
    [ObservableProperty] private string _statusMessage = "";

    public ProgramTypeListViewModel(IProgramTypeRepository typeRepo)
    {
        _typeRepo = typeRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Types.Clear();
        var types = await _typeRepo.GetAllAsync(activeOnly: false);
        foreach (var t in types) Types.Add(t);
    }

    [RelayCommand]
    private async Task NewTypeAsync()
    {
        IsNewType = true;
        IsEditing = true;
        EditTypeCode = await _typeRepo.GetNextCodeAsync();
        EditTypeName = "";
        EditNotes = null;
        StatusMessage = "";
    }

    [RelayCommand]
    private void EditType(ProgramTypeMaster type)
    {
        SelectedType = type;
        IsNewType = false;
        IsEditing = true;
        EditTypeCode = type.TypeCode;
        EditTypeName = type.TypeName;
        EditNotes = type.Notes;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SaveTypeAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTypeName))
        {
            StatusMessage = "型名を入力してください";
            return;
        }

        if (IsNewType)
        {
            var type = new ProgramTypeMaster
            {
                TypeCode = EditTypeCode,
                TypeName = EditTypeName,
                Notes = EditNotes,
                IsActive = true
            };
            await _typeRepo.AddAsync(type);
            StatusMessage = $"「{EditTypeName}」を追加しました";
        }
        else if (SelectedType != null)
        {
            SelectedType.TypeCode = EditTypeCode;
            SelectedType.TypeName = EditTypeName;
            SelectedType.Notes = EditNotes;
            await _typeRepo.UpdateAsync(SelectedType);
            StatusMessage = $"「{EditTypeName}」を更新しました";
        }

        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteTypeAsync(ProgramTypeMaster type)
    {
        SelectedType = type;

        var inUse = await _typeRepo.IsInUseAsync(type.Id);
        if (inUse)
        {
            StatusMessage = $"「{type.TypeName}」は既にプログラムで使用されているため削除できません";
            return;
        }

        var result = MessageBox.Show(
            $"「{type.TypeName}」を削除しますか？",
            "削除の確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (result != MessageBoxResult.OK) return;

        type.IsActive = false;
        await _typeRepo.UpdateAsync(type);
        StatusMessage = $"「{type.TypeName}」を削除しました";
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        StatusMessage = "";
    }
}
