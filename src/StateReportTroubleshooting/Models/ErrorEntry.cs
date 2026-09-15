using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class ErrorEntry
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("example")]
    public string? Example { get; set; }

    [JsonPropertyName("elements_affected")]
    public List<string> ElementsAffected { get; set; } = [];

    [JsonPropertyName("collection_windows")]
    public List<string> CollectionWindows { get; set; } = [];

    [JsonPropertyName("related_fields")]
    public List<string> RelatedFields { get; set; } = [];

    [JsonPropertyName("object")]
    public string? ObjectName { get; set; }
}
