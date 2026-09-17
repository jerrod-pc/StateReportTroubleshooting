using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class FieldEntry
{
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("min_length")]
    public int? MinLength { get; set; }

    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("acceptable_values")]
    public string? AcceptableValues { get; set; }

    [JsonPropertyName("related_elements")]
    public string? RelatedElements { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("sif_object")]
    public string? SifObject { get; set; }

    [JsonPropertyName("sif_element")]
    public string? SifElement { get; set; }

    /// <summary>
    /// The full dotted SIF path, e.g. "StudentPersonal.Name.LastName" for an
    /// object of "StudentPersonal" and an element of "Name/LastName" - source data
    /// uses "/" to separate an element from its sub-element, but the dotted form
    /// reads unambiguously as a path and can't be mistaken for "write to Name".
    /// </summary>
    public string? SifMapping =>
        string.IsNullOrWhiteSpace(SifObject) || SifObject == "None"
            ? null
            : $"{SifObject}.{SifElement?.Replace('/', '.')}";
}
