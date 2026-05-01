using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SessionRecorder.Core.Entities;
using SessionRecorder.Data.Repositories;

namespace SessionRecorder.App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISessionRecordRepository _sessionRepo;
    private readonly INaturalObservationRepository _obsRepo;

    public ObservableCollection<SessionRecord> SessionResults { get; } = [];
    public ObservableCollection<NaturalObservation> ObservationResults { get; } = [];

    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private string _resultSummary = "";

    public SearchViewModel(
        ISessionRecordRepository sessionRepo,
        INaturalObservationRepository obsRepo)
    {
        _sessionRepo = sessionRepo;
        _obsRepo = obsRepo;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            ResultSummary = "検索キーワードを入力してください";
            return;
        }

        SessionResults.Clear();
        ObservationResults.Clear();

        var sessions = await _sessionRepo.SearchAsync(SearchQuery);
        foreach (var s in sessions) SessionResults.Add(s);

        var obs = await _obsRepo.SearchAsync(SearchQuery);
        foreach (var o in obs) ObservationResults.Add(o);

        ResultSummary = $"セッション記録: {sessions.Count}件 / 自然場面記録: {obs.Count}件";
    }
}
