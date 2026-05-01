using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.App.Converters;
using SessionRecorder.Core.Entities;
using SessionRecorder.Core.Enums;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class ProgramListViewModel : ObservableObject
{
    private readonly IProgramRepository _programRepo;
    private readonly ISkillDomainRepository _domainRepo;

    public ObservableCollection<InterventionProgram> Programs { get; } = [];
    public ObservableCollection<SkillDomain> Domains { get; } = [];
    public List<EnumItem<ProgramType>> ProgramTypes { get; } = EnumHelper.GetItems<ProgramType>();

    [ObservableProperty] private InterventionProgram? _selectedProgram;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNewProgram;

    // Edit fields
    [ObservableProperty] private string _editProgramCode = "";
    [ObservableProperty] private string _editProgramName = "";
    [ObservableProperty] private SkillDomain? _editDomain;
    [ObservableProperty] private ProgramType _editProgramType;
    [ObservableProperty] private string? _editMasteryCriteria;
    [ObservableProperty] private string? _editNotes;

    public ProgramListViewModel(IProgramRepository programRepo, ISkillDomainRepository domainRepo)
    {
        _programRepo = programRepo;
        _domainRepo = domainRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Programs.Clear();
        Domains.Clear();

        var programs = await _programRepo.GetAllAsync();
        foreach (var p in programs) Programs.Add(p);

        var domains = await _domainRepo.GetAllAsync();
        foreach (var d in domains) Domains.Add(d);
    }

    [RelayCommand]
    private async Task NewProgramAsync()
    {
        IsNewProgram = true;
        IsEditing = true;
        EditProgramCode = await _programRepo.GetNextCodeAsync();
        EditProgramName = "";
        EditDomain = Domains.FirstOrDefault();
        EditProgramType = ProgramType.StructuredWorksheet;
        EditMasteryCriteria = null;
        EditNotes = null;
    }

    [RelayCommand]
    private void EditProgram()
    {
        if (SelectedProgram == null) return;
        IsNewProgram = false;
        IsEditing = true;
        EditProgramCode = SelectedProgram.ProgramCode;
        EditProgramName = SelectedProgram.ProgramName;
        EditDomain = Domains.FirstOrDefault(d => d.Id == SelectedProgram.DomainId);
        EditProgramType = SelectedProgram.ProgramType;
        EditMasteryCriteria = SelectedProgram.MasteryCriteria;
        EditNotes = SelectedProgram.Notes;
    }

    [RelayCommand]
    private async Task SaveProgramAsync()
    {
        if (EditDomain == null) return;

        if (IsNewProgram)
        {
            var program = new InterventionProgram
            {
                ProgramCode = EditProgramCode,
                ProgramName = EditProgramName,
                DomainId = EditDomain.Id,
                ProgramType = EditProgramType,
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
            SelectedProgram.ProgramType = EditProgramType;
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
