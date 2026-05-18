namespace SessionRecorder.Core.Entities;

public class ProgramTypeMaster
{
    public int Id { get; set; }
    public string TypeCode { get; set; } = "";   // 例: "StructuredWorksheet" (内部識別)
    public string TypeName { get; set; } = "";   // 例: "構造化WS" (表示名)
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InterventionProgram> Programs { get; set; } = [];
}
