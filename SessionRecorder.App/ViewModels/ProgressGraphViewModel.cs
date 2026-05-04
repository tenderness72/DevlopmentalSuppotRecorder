using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SessionRecorder.Core.Entities;
using SessionRecorder.Data.Repositories;
using SkiaSharp;

namespace SessionRecorder.App.ViewModels;

public partial class ProgressGraphViewModel : ObservableObject
{
    private readonly ISessionRecordRepository _sessionRepo;
    private readonly IChildRepository _childRepo;

    private List<SessionRecord> _childSessions = [];

    public ObservableCollection<Child> Children { get; } = [];
    public ObservableCollection<ProgramWithLastDate> ProgramsForChild { get; } = [];

    [ObservableProperty] private Child? _selectedChild;
    [ObservableProperty] private ProgramWithLastDate? _selectedProgramItem;
    [ObservableProperty] private string _lastProgramLabel = "";

    // LiveCharts バインディング用（配列置き換えで更新通知）
    private ISeries[] _series = [];
    public ISeries[] Series
    {
        get => _series;
        set { _series = value; OnPropertyChanged(); }
    }

    private Axis[] _xAxes = DefaultXAxes();
    public Axis[] XAxes
    {
        get => _xAxes;
        set { _xAxes = value; OnPropertyChanged(); }
    }

    private Axis[] _yAxes = DefaultYAxes();
    public Axis[] YAxes
    {
        get => _yAxes;
        set { _yAxes = value; OnPropertyChanged(); }
    }

    // 凡例・ツールチップの日本語フォント（XAML からバインド）
    public SolidColorPaint LegendPaint  { get; } = JpPaint("#1A1A2E");
    public SolidColorPaint TooltipPaint { get; } = JpPaint("#1A1A2E");

    // サマリ
    [ObservableProperty] private int _sessionCount;
    [ObservableProperty] private string _avgRate = "—";
    [ObservableProperty] private string _latestRate = "—";
    [ObservableProperty] private string _trendText = "";
    [ObservableProperty] private bool _hasData;

    public ProgressGraphViewModel(
        ISessionRecordRepository sessionRepo,
        IChildRepository childRepo)
    {
        _sessionRepo = sessionRepo;
        _childRepo = childRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Children.Clear();
        var children = await _childRepo.GetAllAsync();
        foreach (var c in children) Children.Add(c);
    }

    partial void OnSelectedChildChanged(Child? value)
    {
        if (value == null) { ProgramsForChild.Clear(); ClearGraph(); return; }
        _ = LoadChildDataAsync(value);
    }

    partial void OnSelectedProgramItemChanged(ProgramWithLastDate? value)
    {
        if (value == null) { ClearGraph(); return; }
        BuildGraph(value.Program);
    }

    private async Task LoadChildDataAsync(Child child)
    {
        _childSessions = await _sessionRepo.GetByChildIdAsync(child.Id);

        var programItems = _childSessions
            .GroupBy(s => s.ProgramId)
            .Select(g => new ProgramWithLastDate(g.First().Program, g.Max(s => s.Date)))
            .OrderByDescending(p => p.LastDate)
            .ToList();

        ProgramsForChild.Clear();
        foreach (var item in programItems) ProgramsForChild.Add(item);

        if (programItems.Count > 0)
        {
            var last = programItems[0];
            LastProgramLabel = $"前回：{last.Program.ProgramName}  ({last.LastDate:MM/dd})";
            SelectedProgramItem = last;
        }
        else
        {
            LastProgramLabel = "";
            SelectedProgramItem = null;
            ClearGraph();
        }
    }

    private void BuildGraph(InterventionProgram program)
    {
        var sessions = _childSessions
            .Where(s => s.ProgramId == program.Id && s.CorrectRate.HasValue)
            .OrderBy(s => s.Date)
            .ToList();

        SessionCount = _childSessions.Count(s => s.ProgramId == program.Id);
        HasData = sessions.Count > 0;

        if (!HasData) { ClearGraph(); return; }

        // X 軸は OADate（double）で扱う → DateTime との相互変換が安定
        var points = sessions
            .Select(s => new ObservablePoint(
                s.Date.ToOADate(),
                Math.Round(s.CorrectRate!.Value * 100, 1)))
            .ToList();

        var allSeries = new List<ISeries>
        {
            new LineSeries<ObservablePoint>
            {
                Values = points,
                Name = "正答率",
                Stroke            = new SolidColorPaint(SKColor.Parse("#2563EB"), 2.5f),
                Fill              = new SolidColorPaint(SKColor.Parse("#2563EB").WithAlpha(25)),
                GeometryFill      = new SolidColorPaint(SKColor.Parse("#2563EB")),
                GeometryStroke    = new SolidColorPaint(SKColors.White, 2f),
                GeometrySize      = 10,
                LineSmoothness    = 0
            }
        };

        // 達成基準ライン（%が読み取れた場合）
        var criterion = ParseCriterionPercent(program.MasteryCriteria);
        if (criterion.HasValue && sessions.Count >= 1)
        {
            var minX = points.Min(p => p.X!.Value);
            var maxX = points.Max(p => p.X!.Value);
            allSeries.Add(new LineSeries<ObservablePoint>
            {
                Values = new[]
                {
                    new ObservablePoint(minX, criterion.Value),
                    new ObservablePoint(maxX, criterion.Value)
                },
                Name           = $"達成基準 {criterion.Value}%",
                Stroke         = new SolidColorPaint(SKColor.Parse("#DC2626"), 1.5f)
                                 { PathEffect = new DashEffect([6f, 4f]) },
                Fill           = null,
                GeometrySize   = 0,
                LineSmoothness = 0
            });
        }

        Series = allSeries.ToArray();

        XAxes =
        [
            new Axis
            {
                Labeler        = v => DateTime.FromOADate(v).ToString("M/d"),
                MinStep        = 1.0,
                Name           = "日付",
                LabelsRotation = -30,
                NamePaint      = JpPaint("#6B7280"),
                LabelsPaint    = JpPaint("#374151"),
            }
        ];

        YAxes =
        [
            new Axis
            {
                Name        = "正答率 (%)",
                MinLimit    = 0,
                MaxLimit    = 100,
                Labeler     = v => $"{v:F0}%",
                NamePaint   = JpPaint("#6B7280"),
                LabelsPaint = JpPaint("#374151"),
            }
        ];

        // サマリ
        var rates = sessions.Select(s => s.CorrectRate!.Value * 100).ToList();
        AvgRate    = $"{rates.Average():F1}%";
        LatestRate = $"{rates.Last():F1}%";
        TrendText  = rates.Count >= 2
            ? (rates.Last() - rates.First() >= 0
                ? $"▲ {rates.Last() - rates.First():F1}pt 向上"
                : $"▼ {rates.First() - rates.Last():F1}pt 低下")
            : "";
    }

    private void ClearGraph()
    {
        Series     = [];
        XAxes      = DefaultXAxes();
        YAxes      = DefaultYAxes();
        SessionCount = 0;
        AvgRate    = "—";
        LatestRate = "—";
        TrendText  = "";
        HasData    = false;
    }

    private static Axis[] DefaultXAxes() =>
    [
        new Axis { Name = "日付", NamePaint = JpPaint("#6B7280"), LabelsPaint = JpPaint("#374151") }
    ];

    private static Axis[] DefaultYAxes() =>
    [
        new Axis
        {
            Name        = "正答率 (%)",
            MinLimit    = 0,
            MaxLimit    = 100,
            Labeler     = v => $"{v:F0}%",
            NamePaint   = JpPaint("#6B7280"),
            LabelsPaint = JpPaint("#374151"),
        }
    ];

    // 日本語対応フォント（Yu Gothic UI → Meiryo → フォールバック）
    private static readonly SKTypeface JpFont =
        SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Normal) ??
        SKTypeface.FromFamilyName("Meiryo UI",    SKFontStyle.Normal) ??
        SKTypeface.FromFamilyName("MS UI Gothic", SKFontStyle.Normal) ??
        SKTypeface.Default;

    private static SolidColorPaint JpPaint(string hex) =>
        new(SKColor.Parse(hex)) { SKTypeface = JpFont };

    private static double? ParseCriterionPercent(string? criteria)
    {
        if (string.IsNullOrEmpty(criteria)) return null;
        var m = Regex.Match(criteria, @"(\d+(?:\.\d+)?)%");
        if (m.Success && double.TryParse(m.Groups[1].Value, out var v)) return v;
        return null;
    }
}

public class ProgramWithLastDate(InterventionProgram program, DateTime lastDate)
{
    public InterventionProgram Program { get; } = program;
    public DateTime LastDate { get; } = lastDate;
    public string DisplayName => Program.ProgramName;
    public string LastDateDisplay => $"{LastDate:MM/dd}";
}
