using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class AppendixFile
{
    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";

    [JsonPropertyName("appendices")]
    public List<AppendixSheet> Appendices { get; set; } = [];
}
