using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.Core.Entities;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class ProgramListViewModel : ObservableObject
{
    private readonly IProgramRepository _programRepo;
    private readonly ISkillDomainRepository _domainRepo;
    private readonly IProgramTypeRepository _typeRepo;

    public ObservableCollection<InterventionProgram> Programs { get; } = [];
    public ObservableCollection<SkillDomain> Domains { get; } = [];
    public ObservableCollection<ProgramTypeMaster> ProgramTypes { get; } = [];

    [ObservableProperty] private InterventionProgram? _selectedProgram;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNewProgram;

    // Edit fields
    [ObservableProperty] private string _editProgramCode = "";
    [ObservableProperty] private string _editProgramName = "";
    [ObservableProperty] private SkillDomain? _editDomain;
    [ObservableProperty] private ProgramTypeMaster? _editProgramType;
    [ObservableProperty] private string? _editMasteryCriteria;
    [ObservableProperty] private string? _editNotes;

    public ProgramListViewModel(
        IProgramRepository programRepo,
        ISkillDomainRepository domainRepo,
        IProgramTypeRepository typeRepo)
    {
        _programRepo = programRepo;
        _domainRepo = domainRepo;
        _typeRepo = typeRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Programs.Clear();
        Domains.Clear();
        ProgramTypes.Clear();

        var programs = await _programRepo.GetAllAsync();
        foreach (var p in programs) Programs.Add(p);

        var domains = await _domainRepo.GetAllAsync();
        foreach (var d in domains) Domains.Add(d);

        var types = await _typeRepo.GetAllAsync();
        foreach (var t in types) ProgramTypes.Add(t);
    }

    [RelayCommand]
    private async Task NewProgramAsync()
    {
        IsNewProgram = true;
        IsEditing = true;
        EditProgramCode = await _programRepo.GetNextCodeAsync();
        EditProgramName = "";
        EditDomain = Domains.FirstOrDefault();
        EditProgramType = ProgramTypes.FirstOrDefault();
        EditMasteryCriteria = null;
        EditNotes = null;
    }

    [RelayCommand]
    private void EditProgram(InterventionProgram program)
    {
        SelectedProgram = program;
        IsNewProgram = false;
        IsEditing = true;
        EditProgramCode = program.ProgramCode;
        EditProgramName = program.ProgramName;
        EditDomain = Domains.FirstOrDefault(d => d.Id == program.DomainId);
        EditProgramType = ProgramTypes.FirstOrDefault(t => t.Id == program.ProgramTypeId);
        EditMasteryCriteria = program.MasteryCriteria;
        EditNotes = program.Notes;
    }

    [RelayCommand]
    private async Task DeleteProgramAsync(InterventionProgram program)
    {
        var inUse = await _programRepo.IsInUseAsync(program.Id);
        if (inUse)
        {
            MessageBox.Show(
                $"「{program.ProgramName}」はセッション記録で使用されているため削除できません。",
                "削除できません", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"「{program.ProgramName}」を削除しますか？\nこの操作は元に戻せません。",
            "削除の確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning,
            MessageBoxResult.Cancel);

        if (result != MessageBoxResult.OK) return;

        await _programRepo.DeleteAsync(program.Id);
        Programs.Remove(program);
        if (SelectedProgram == program) { SelectedProgram = null; IsEditing = false; }
    }

    [RelayCommand]
    private async Task SaveProgramAsync()
    {
        if (EditDomain == null || EditProgramType == null) return;

        if (IsNewProgram)
        {
            var program = new InterventionProgram
            {
                ProgramCode = EditProgramCode,
                ProgramName = EditProgramName,
                DomainId = EditDomain.Id,
                ProgramTypeId = EditProgramType.Id,
                MasteryCriteria = EditMasteryCriteria,
                Notes = EditNotes
            };
            await _programRepo.AddAsync(program);
        }
        else if (SelectedProgram != null)
        {
            SelectedProgram.ProgramCode = EditProgramCode;
            SelectedProgram.ProgramName = EditProgramName;
            SelectedProgram.DomainId = EditDomain.Id;
            SelectedProgram.ProgramTypeId = EditProgramType.Id;
            SelectedProgram.MasteryCriteria = EditMasteryCriteria;
            SelectedProgram.Notes = EditNotes;
            await _programRepo.UpdateAsync(SelectedProgram);
        }

        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;
}
