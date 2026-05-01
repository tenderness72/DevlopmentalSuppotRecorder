using ClosedXML.Excel;
using SessionRecorder.App.Converters;
using SessionRecorder.Core.Entities;

namespace SessionRecorder.App.Services;

public class ExcelExportService
{
    public void ExportChildSessions(Child child, List<SessionRecord> sessions,
        List<NaturalObservation> observations, string filePath)
    {
        using var wb = new XLWorkbook();

        // セッション記録シート
        var ws1 = wb.AddWorksheet("セッション記録");
        ws1.Cell(1, 1).Value = $"{child.ChildCode} {child.Name} — セッション記録";
        ws1.Cell(1, 1).Style.Font.Bold = true;
        ws1.Cell(1, 1).Style.Font.FontSize = 14;

        var headers1 = new[] { "日付", "課題名", "領域", "試行数", "正反応数",
            "正反応率", "クリア判定", "プロンプト", "臨床メモ", "仮説/解釈", "次回申し送り" };
        for (int i = 0; i < headers1.Length; i++)
        {
            ws1.Cell(3, i + 1).Value = headers1[i];
            ws1.Cell(3, i + 1).Style.Font.Bold = true;
            ws1.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int r = 0; r < sessions.Count; r++)
        {
            var s = sessions[r];
            var row = r + 4;
            ws1.Cell(row, 1).Value = s.Date.ToString("yyyy/MM/dd");
            ws1.Cell(row, 2).Value = s.Program?.ProgramName ?? "";
            ws1.Cell(row, 3).Value = s.Program?.Domain?.DomainName ?? "";
            ws1.Cell(row, 4).Value = s.TrialCount ?? 0;
            ws1.Cell(row, 5).Value = s.CorrectCount ?? 0;
            ws1.Cell(row, 6).Value = s.CorrectRate.HasValue ? $"{s.CorrectRate.Value:P0}" : "—";
            ws1.Cell(row, 7).Value = s.MasteryLevel.HasValue ? EnumHelper.GetDisplayName(s.MasteryLevel.Value) : "";
            ws1.Cell(row, 8).Value = s.PromptLevel.HasValue ? EnumHelper.GetDisplayName(s.PromptLevel.Value) : "";
            ws1.Cell(row, 9).Value = s.ClinicalNote ?? "";
            ws1.Cell(row, 10).Value = s.Hypothesis ?? "";
            ws1.Cell(row, 11).Value = s.NextAction ?? "";
        }
        ws1.Columns().AdjustToContents(3, 50);

        // 自然場面記録シート
        var ws2 = wb.AddWorksheet("自然場面記録");
        ws2.Cell(1, 1).Value = $"{child.ChildCode} {child.Name} — 自然場面記録";
        ws2.Cell(1, 1).Style.Font.Bold = true;
        ws2.Cell(1, 1).Style.Font.FontSize = 14;

        var headers2 = new[] { "日付", "記録タイプ", "状況", "観察された行動",
            "結果", "臨床的解釈", "次回検証課題" };
        for (int i = 0; i < headers2.Length; i++)
        {
            ws2.Cell(3, i + 1).Value = headers2[i];
            ws2.Cell(3, i + 1).Style.Font.Bold = true;
            ws2.Cell(3, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int r = 0; r < observations.Count; r++)
        {
            var o = observations[r];
            var row = r + 4;
            ws2.Cell(row, 1).Value = o.Date.ToString("yyyy/MM/dd");
            ws2.Cell(row, 2).Value = EnumHelper.GetDisplayName(o.ObservationType);
            ws2.Cell(row, 3).Value = o.Situation ?? "";
            ws2.Cell(row, 4).Value = o.ObservedBehavior ?? "";
            ws2.Cell(row, 5).Value = o.Result.HasValue ? EnumHelper.GetDisplayName(o.Result.Value) : "";
            ws2.Cell(row, 6).Value = o.Interpretation ?? "";
            ws2.Cell(row, 7).Value = o.NextVerification ?? "";
        }
        ws2.Columns().AdjustToContents(3, 50);

        wb.SaveAs(filePath);
    }
}
