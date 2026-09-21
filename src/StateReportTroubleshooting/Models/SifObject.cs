using System.Text.Json.Serialization;

namespace StateReportTroubleshooting.Models;

public class SifObjectFile
{
    [JsonPropertyName("objects")]
    public List<SifObject> Objects { get; set; } = [];

    [JsonPropertyName("general_notes")]
    public List<SifNote> GeneralNotes { get; set; } = [];
}

public class SifObject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; } = "";

    [JsonPropertyName("used_in")]
    public List<string> UsedIn { get; set; } = [];

    [JsonPropertyName("notes")]
    public List<SifNote> Notes { get; set; } = [];
}

public class SifNote
{
    [JsonPropertyName("heading")]
    public string? Heading { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";
}
