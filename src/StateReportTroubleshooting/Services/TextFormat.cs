using System.Text.RegularExpressions;

namespace StateReportTroubleshooting.Services;

/// <summary>
/// Some source-data fields (mostly "notes" and "acceptable_values") are flattened,
/// pandoc-derived dumps of numbered lists or two-column code tables with no line breaks
/// (e.g. "1. First note. 2. Second note." or "00 Not Title I 14 Targeted/science ...").
/// This splits such text into one chunk per numbered/coded item so it can render as
/// separate lines instead of one dense paragraph. It never rewrites the text itself -
/// only where line breaks go - and leaves ordinary prose untouched.
/// </summary>
public static partial class TextFormat
{
    // Matches either a numbered-list marker ("1. ", "12) ") or a bare two-digit code-table
    // marker immediately followed by a capitalized word ("00 Not Title I", "14 Targeted...").
    // Deliberately does NOT match a bare number in running prose (e.g. "grade 12 (SP)")
    // since that has no trailing punctuation and isn't followed by a capital letter.
    [GeneratedRegex(@"(?<=^|\s)(?:\d{1,3}[.)]\s+|\d{2}\s+(?=[A-Z]))")]
    private static partial Regex ListMarkerRegex();

    private const int MinMarkersToTreatAsList = 4;

    public static List<string> SplitIntoItems(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var matches = ListMarkerRegex().Matches(text);
        if (matches.Count < MinMarkersToTreatAsList)
        {
            return [text];
        }

        var items = new List<string>();

        var firstIndex = matches[0].Index;
        if (firstIndex > 0)
        {
            var preamble = text[..firstIndex].Trim();
            if (preamble.Length > 0)
            {
                items.Add(preamble);
            }
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var item = text[start..end].Trim();
            if (item.Length > 0)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
