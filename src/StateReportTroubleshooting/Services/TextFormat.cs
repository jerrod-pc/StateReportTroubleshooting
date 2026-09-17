using System.Text.RegularExpressions;

namespace StateReportTroubleshooting.Services;

/// <summary>
/// Some source-data fields (mostly "notes" and "acceptable_values") are flattened,
/// pandoc-derived dumps of numbered lists or two-column code tables with no line breaks
/// (e.g. "1. First note. 2. Second note." or "00 Not Title I 14 Targeted/science ...").
/// This splits such text into one chunk per line, numbered/coded item, or (failing those)
/// at the runs of 2+ spaces that mark a flattened paragraph/line break, so it can render
/// as separate lines instead of one dense paragraph. It never rewrites the text itself -
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

    // Source docs are pandoc/Word extractions where an original paragraph or line break
    // was flattened into a run of 2+ spaces (single spaces are just normal sentence
    // spacing and are left alone). Splitting on this recovers the original paragraphing.
    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ParagraphBreakRegex();

    private const int MinMarkersToTreatAsList = 4;

    // IsList distinguishes genuinely distinct, enumerated entries (numbered notes, coded
    // values) from plain paragraph breaks, so the two can be styled differently - a rule
    // between list entries reads as a separator; the same rule between paragraphs reads
    // as a stray horizontal line.
    public readonly record struct FormattedItems(IReadOnlyList<string> Items, bool IsList);

    public static FormattedItems SplitIntoItems(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new FormattedItems([], false);
        }

        // A literal newline in the source JSON is an explicit, unambiguous line break
        // (unlike the flattened-whitespace cases below) and is used throughout the source
        // data exclusively for enumerated "code = description" value lists, e.g.
        // "00 = Not Applicable\n01 = ...". Honor it directly, ahead of the fuzzier heuristics.
        if (text.Contains('\n'))
        {
            var lines = text.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            if (lines.Count > 1)
            {
                return new FormattedItems(lines, true);
            }
        }

        var matches = ListMarkerRegex().Matches(text);
        if (matches.Count >= MinMarkersToTreatAsList)
        {
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

            return new FormattedItems(items, true);
        }

        var paragraphs = ParagraphBreakRegex().Split(text)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();

        return paragraphs.Count > 1
            ? new FormattedItems(paragraphs, false)
            : new FormattedItems([text.Trim()], false);
    }
}
