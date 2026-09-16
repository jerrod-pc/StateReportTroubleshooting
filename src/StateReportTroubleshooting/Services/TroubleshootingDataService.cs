using System.Net.Http.Json;
using StateReportTroubleshooting.Models;

namespace StateReportTroubleshooting.Services;

public class TroubleshootingDataService(HttpClient http)
{
    public static readonly string[] CollectionNames = ["SIMS", "EPIMS", "SCS", "SSDR"];

    public bool IsInitialized { get; private set; }
    public bool SimsAppendicesLoaded { get; private set; }

    public Dictionary<string, CollectionFile> Collections { get; } = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Dictionary<string, ErrorEntry>> _errorsByCode =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Dictionary<string, FieldEntry>> _fieldsById =
        new(StringComparer.OrdinalIgnoreCase);

    // key: "{Collection}|{FieldId}" -> errors referencing that field (via related_fields or elements_affected)
    private readonly Dictionary<string, List<ErrorEntry>> _errorsReferencingField =
        new(StringComparer.OrdinalIgnoreCase);

    // key: "{Collection}|{FieldId}" -> appendix sheets linked to that field
    private readonly Dictionary<string, List<AppendixSheet>> _appendicesByField =
        new(StringComparer.OrdinalIgnoreCase);

    private AppendixFile? _epimsAppendices;
    private AppendixFile? _simsAppendices;

    private static string Key(string collection, string fieldId) => $"{collection}|{fieldId}";

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        var scsTask = http.GetFromJsonAsync<CollectionFile>("data/scs.json");
        var epimsTask = http.GetFromJsonAsync<CollectionFile>("data/epims.json");
        var simsTask = http.GetFromJsonAsync<CollectionFile>("data/sims.json");
        var ssdrTask = http.GetFromJsonAsync<CollectionFile>("data/ssdr.json");
        var epimsAppendicesTask = http.GetFromJsonAsync<AppendixFile>("data/epims-appendices.json");

        await Task.WhenAll(scsTask, epimsTask, simsTask, ssdrTask, epimsAppendicesTask);

        RegisterCollection(scsTask.Result);
        RegisterCollection(epimsTask.Result);
        RegisterCollection(simsTask.Result);
        RegisterCollection(ssdrTask.Result);

        _epimsAppendices = epimsAppendicesTask.Result;
        if (_epimsAppendices is not null)
        {
            IndexAppendices(_epimsAppendices);
        }

        IsInitialized = true;
    }

    public async Task EnsureSimsAppendicesLoadedAsync()
    {
        if (SimsAppendicesLoaded) return;

        _simsAppendices = await http.GetFromJsonAsync<AppendixFile>("data/sims-appendices.json");
        if (_simsAppendices is not null)
        {
            IndexAppendices(_simsAppendices);
        }

        SimsAppendicesLoaded = true;
    }

    private void RegisterCollection(CollectionFile? file)
    {
        if (file is null) return;

        var name = file.Collection;
        Collections[name] = file;

        var errorDict = new Dictionary<string, ErrorEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var error in file.Errors)
        {
            errorDict[error.Code] = error;
        }
        _errorsByCode[name] = errorDict;

        var fieldDict = new Dictionary<string, FieldEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in file.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.FieldId)) continue;
            // First-wins: a couple of legitimate duplicate field_ids exist in source data
            // (e.g. SSDR "OFF ID" appears once for the offense record, once for the discipline record).
            fieldDict.TryAdd(field.FieldId, field);
        }
        _fieldsById[name] = fieldDict;

        foreach (var error in file.Errors)
        {
            var referenced = error.RelatedFields.Concat(error.ElementsAffected).Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var fieldId in referenced)
            {
                var key = Key(name, fieldId);
                if (!_errorsReferencingField.TryGetValue(key, out var list))
                {
                    list = [];
                    _errorsReferencingField[key] = list;
                }
                list.Add(error);
            }
        }
    }

    private void IndexAppendices(AppendixFile appendixFile)
    {
        foreach (var sheet in appendixFile.Appendices)
        {
            foreach (var fieldId in sheet.LinkedFields)
            {
                var key = Key(appendixFile.Collection, fieldId);
                if (!_appendicesByField.TryGetValue(key, out var list))
                {
                    list = [];
                    _appendicesByField[key] = list;
                }
                list.Add(sheet);
            }
        }
    }

    public ErrorEntry? FindError(string collection, string code)
    {
        return _errorsByCode.TryGetValue(collection, out var dict) && dict.TryGetValue(code, out var error)
            ? error
            : null;
    }

    public FieldEntry? FindField(string collection, string fieldId)
    {
        return _fieldsById.TryGetValue(collection, out var dict) && dict.TryGetValue(fieldId, out var field)
            ? field
            : null;
    }

    /// <summary>
    /// Resolves a field code that may belong to a different collection than the error referencing it
    /// (e.g. an EPIMS error's related_fields entry of "DOE014" refers to SIMS). Tries a prefix-based
    /// guess first, then falls back to scanning every collection. Some source-data codes are typo'd
    /// (missing a leading zero, etc.) and will never resolve — callers should treat a null result as
    /// "show the raw code as an unresolved chip", not as an error.
    /// </summary>
    public (string Collection, FieldEntry Field)? FindFieldAnyCollection(string code, string? preferredCollection = null)
    {
        var candidates = new List<string>();
        if (preferredCollection is not null) candidates.Add(preferredCollection);

        if (code.StartsWith("DOE", StringComparison.OrdinalIgnoreCase)) candidates.Add("SIMS");
        else if (code.StartsWith("SR", StringComparison.OrdinalIgnoreCase)
                 || code.StartsWith("WA", StringComparison.OrdinalIgnoreCase)
                 || code.StartsWith("ID", StringComparison.OrdinalIgnoreCase)) candidates.Add("EPIMS");
        else if (code.StartsWith("SCS", StringComparison.OrdinalIgnoreCase)) candidates.Add("SCS");

        candidates.AddRange(CollectionNames);

        foreach (var collection in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var field = FindField(collection, code);
            if (field is not null) return (collection, field);
        }

        return null;
    }

    public List<ErrorEntry> ErrorsReferencing(string collection, string fieldId)
    {
        return _errorsReferencingField.TryGetValue(Key(collection, fieldId), out var list)
            ? list
            : [];
    }

    public List<AppendixSheet> AppendicesFor(string collection, string fieldId)
    {
        return _appendicesByField.TryGetValue(Key(collection, fieldId), out var list)
            ? list
            : [];
    }

    public List<AppendixSheet> AppendicesForCollection(string collection)
    {
        if (collection.Equals("EPIMS", StringComparison.OrdinalIgnoreCase))
        {
            return _epimsAppendices?.Appendices ?? [];
        }
        if (collection.Equals("SIMS", StringComparison.OrdinalIgnoreCase))
        {
            return _simsAppendices?.Appendices ?? [];
        }
        return [];
    }

    public AppendixSheet? FindAppendixSheet(string collection, string sheetSlug)
    {
        return AppendicesForCollection(collection)
            .FirstOrDefault(s => Slugify(s.SheetName) == sheetSlug);
    }

    public static string Slugify(string value)
    {
        var chars = value.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug.Trim('-');
    }

    public List<ErrorEntry> SearchErrors(string? collection, string query)
    {
        var collections = collection is null ? CollectionNames : [collection];
        var q = query.Trim();
        if (q.Length == 0) return [];

        return collections
            .Where(c => Collections.ContainsKey(c))
            .SelectMany(c => Collections[c].Errors)
            .Where(e => e.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || e.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || (e.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public List<FieldEntry> SearchFields(string? collection, string query)
    {
        var collections = collection is null ? CollectionNames : [collection];
        var q = query.Trim();
        if (q.Length == 0) return [];

        return collections
            .Where(c => Collections.ContainsKey(c))
            .SelectMany(c => Collections[c].Fields)
            .Where(f => (f.FieldId?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                        || f.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public List<SearchHit> GlobalSearch(string query, int limit = 50)
    {
        var q = query.Trim();
        if (q.Length == 0) return [];

        // Direct error-code jump pattern, e.g. "EPIMS6420"
        var directHits = new List<SearchHit>();
        foreach (var collection in CollectionNames)
        {
            var error = FindError(collection, q);
            if (error is not null)
            {
                directHits.Add(new SearchHit { Collection = collection, Kind = SearchHitKind.Error, Code = error.Code, Title = error.Title });
            }
        }

        var errorHits = CollectionNames
            .Where(c => Collections.ContainsKey(c))
            .SelectMany(c => SearchErrors(c, q).Select(e => new SearchHit { Collection = c, Kind = SearchHitKind.Error, Code = e.Code, Title = e.Title }));

        var fieldHits = CollectionNames
            .Where(c => Collections.ContainsKey(c))
            .SelectMany(c => SearchFields(c, q).Select(f => new SearchHit { Collection = c, Kind = SearchHitKind.Field, Code = f.FieldId ?? "", Title = f.Name }));

        return directHits
            .Concat(errorHits)
            .Concat(fieldHits)
            .DistinctBy(h => (h.Collection, h.Kind, h.Code))
            .Take(limit)
            .ToList();
    }
}
