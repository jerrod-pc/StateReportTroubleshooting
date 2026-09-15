using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class AppendixSheet
{
    [JsonPropertyName("sheet_name")]
    public string SheetName { get; set; } = "";

    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";

    [JsonPropertyName("linked_fields")]
    public List<string> LinkedFields { get; set; } = [];

    [JsonPropertyName("columns")]
    public List<string> Columns { get; set; } = [];

    [JsonPropertyName("rows")]
    public List<List<string>> Rows { get; set; } = [];
}
