using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SessionRecorder.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private object? _currentView;
    [ObservableProperty] private string _statusMessage = "準備完了";

    public ChildListViewModel ChildListVm { get; }
    public ProgramListViewModel ProgramListVm { get; }
    public SessionEntryViewModel SessionEntryVm { get; }
    public ObservationEntryViewModel ObservationEntryVm { get; }
    public SearchViewModel SearchVm { get; }
    public SkillDomainListViewModel SkillDomainListVm { get; }

    public MainViewModel(
        ChildListViewModel childListVm,
        ProgramListViewModel programListVm,
        SessionEntryViewModel sessionEntryVm,
        ObservationEntryViewModel observationEntryVm,
        SearchViewModel searchVm,
        SkillDomainListViewModel skillDomainListVm)
    {
        ChildListVm = childListVm;
        ProgramListVm = programListVm;
        SessionEntryVm = sessionEntryVm;
        ObservationEntryVm = observationEntryVm;
        SearchVm = searchVm;
        SkillDomainListVm = skillDomainListVm;

        ChildListVm.ChildSelected += child =>
            StatusMessage = $"{child.ChildCode} {child.Name} を選択中";

        CurrentView = ChildListVm;
    }

    [RelayCommand] private void NavigateToChildren() => CurrentView = ChildListVm;
    [RelayCommand] private void NavigateToPrograms() => CurrentView = ProgramListVm;
    [RelayCommand] private void NavigateToSessionEntry() => CurrentView = SessionEntryVm;
    [RelayCommand] private void NavigateToObservationEntry() => CurrentView = ObservationEntryVm;
    [RelayCommand] private void NavigateToSearch() => CurrentView = SearchVm;
    [RelayCommand] private void NavigateToSkillDomains() => CurrentView = SkillDomainListVm;
}
