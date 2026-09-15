using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class CollectionFile
{
    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";

    [JsonPropertyName("error_list_version")]
    public string? ErrorListVersion { get; set; }

    [JsonPropertyName("handbook_version")]
    public string? HandbookVersion { get; set; }

    [JsonPropertyName("object_ranges")]
    public List<ObjectRange> ObjectRanges { get; set; } = [];

    [JsonPropertyName("errors")]
    public List<ErrorEntry> Errors { get; set; } = [];

    [JsonPropertyName("fields")]
    public List<FieldEntry> Fields { get; set; } = [];
}
