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

    /// <summary>Short, conversational one/two-sentence explanation of what this object is for.</summary>
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }

    /// <summary>The MA-relevant elements of this object, one line each - the headline content
    /// of the object's page. Elements the MA collections don't use at all are pre-filtered out
    /// of the source data, not hidden here.</summary>
    [JsonPropertyName("elements")]
    public List<SifElement> Elements { get; set; } = [];

    /// <summary>Denser prose pulled from the SIF Technical Guide - secondary, collapsed-by-default
    /// content for whoever wants the deeper implementation detail.</summary>
    [JsonPropertyName("notes")]
    public List<SifNote> Notes { get; set; } = [];
}

public class SifElement
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("used_in")]
    public List<string> UsedIn { get; set; } = [];

    /// <summary>Collection -> M/MR/O/C, only present when known and not already implied by a
    /// resolved field link in Fields.</summary>
    [JsonPropertyName("requirement")]
    public Dictionary<string, string> Requirement { get; set; } = [];

    [JsonPropertyName("fields")]
    public List<SifElementField> Fields { get; set; } = [];
}

public class SifElementField
{
    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";

    [JsonPropertyName("field_id")]
    public string FieldId { get; set; } = "";
}

public class SifNote
{
    [JsonPropertyName("heading")]
    public string? Heading { get; set; }

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";
}
