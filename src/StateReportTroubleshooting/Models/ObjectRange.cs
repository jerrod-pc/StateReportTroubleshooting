using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class ObjectRange
{
    [JsonPropertyName("object")]
    public string ObjectName { get; set; } = "";

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }
}
