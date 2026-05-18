using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.Core.Entities;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class SkillDomainListViewModel : ObservableObject
{
    private readonly ISkillDomainRepository _domainRepo;

    public ObservableCollection<SkillDomain> Domains { get; } = [];

    [ObservableProperty] private SkillDomain? _selectedDomain;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNewDomain;
    [ObservableProperty] private string _editDomainCode = "";
    [ObservableProperty] private string _editDomainName = "";
    [ObservableProperty] private string? _editNotes;
    [ObservableProperty] private string _statusMessage = "";

    public SkillDomainListViewModel(ISkillDomainRepository domainRepo)
    {
        _domainRepo = domainRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Domains.Clear();
        var domains = await _domainRepo.GetAllAsync(activeOnly: false);
        foreach (var d in domains) Domains.Add(d);
    }

    [RelayCommand]
    private async Task NewDomainAsync()
    {
        IsNewDomain = true;
        IsEditing = true;
        EditDomainCode = await _domainRepo.GetNextCodeAsync();
        EditDomainName = "";
        EditNotes = null;
        StatusMessage = "";
    }

    [RelayCommand]
    private void EditDomain(SkillDomain domain)
    {
        SelectedDomain = domain;
        IsNewDomain = false;
        IsEditing = true;
        EditDomainCode = domain.DomainCode;
        EditDomainName = domain.DomainName;
        EditNotes = domain.Notes;
        StatusMessage = "";
    }

    [RelayCommand]
    private async Task SaveDomainAsync()
    {
        if (string.IsNullOrWhiteSpace(EditDomainName))
        {
            StatusMessage = "領域名を入力してください";
            return;
        }

        if (IsNewDomain)
        {
            var domain = new SkillDomain
            {
                DomainCode = EditDomainCode,
                DomainName = EditDomainName,
                Notes = EditNotes,
                IsActive = true
            };
            await _domainRepo.AddAsync(domain);
            StatusMessage = $"「{EditDomainName}」を追加しました";
        }
        else if (SelectedDomain != null)
        {
            SelectedDomain.DomainCode = EditDomainCode;
            SelectedDomain.DomainName = EditDomainName;
            SelectedDomain.Notes = EditNotes;
            await _domainRepo.UpdateAsync(SelectedDomain);
            StatusMessage = $"「{EditDomainName}」を更新しました";
        }

        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteDomainAsync(SkillDomain domain)
    {
        SelectedDomain = domain;

        // 使用中チェック
        var inUse = await _domainRepo.IsInUseAsync(domain.Id);
        if (inUse)
        {
            StatusMessage = $"「{domain.DomainName}」は既にプログラムで使用されているため削除できません";
            return;
        }

        var result = MessageBox.Show(
            $"「{domain.DomainName}」を削除しますか？",
            "削除の確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (result != MessageBoxResult.OK) return;

        domain.IsActive = false;
        await _domainRepo.UpdateAsync(domain);
        StatusMessage = $"「{domain.DomainName}」を削除しました";
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
