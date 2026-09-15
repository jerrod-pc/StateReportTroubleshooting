# DESE Collection Reference Data — Generated JSON

Converted from the SCS, EPIMS, SIMS, and SSDR error lists and data
handbooks (all versioned October 1, 2025), plus the EPIMS and SIMS
appendices workbooks.

## Files

| File | Contents | Size |
|---|---|---|
| `scs.json`, `epims.json`, `sims.json`, `ssdr.json` | One file per collection: `errors[]`, `fields[]`, and the shared `object_ranges` table | 78–222 KB |
| `index.json` | Flat list of every error code and field code across all four collections (`collection`, `type`, `code`, `title`) — for fast search/autocomplete without loading a full collection file | 117 KB |
| `epims-appendices.json`, `sims-appendices.json` | Code lookup tables (race/ethnicity, job classification, degree institutions, course codes, etc.) referenced by fields whose acceptable values are "see appendix" | 169 KB / 1.4 MB |

SCS and SSDR have no appendices workbook of their own; SSDR's one
appendix (offense codes) lives inside its handbook text, not a
separate file, and isn't broken out yet.

**Note on `epims-appendices.json` size**: the degree-institution list
(SR19/22/25) alone is ~5,000 rows and accounts for almost all of the
1.4 MB. For WASM, lazy-load this file only when a query actually
touches SR19/22/25/etc., rather than on app start.

## Schema

**Error entry:**
```json
{
  "code": "SCS9310",
  "collection": "SCS",
  "title": "Course Credit Earned (SCS11) must be less or equal to Course Credit Available (SCS10)",
  "description": "...",
  "example": "...or null",
  "elements_affected": ["DOE015"],
  "collection_windows": ["OCT", "MAR", "EOY"],
  "related_fields": ["SCS10", "SCS11"],
  "object": "StudentSectionMarks"
}
```
- `elements_affected` is populated mainly from SIMS entries that have an explicit "Elements affected:" line.
- `collection_windows` is populated only where the source doc tagged the rule (SIMS mainly; OCT/MAR/EOY). Empty list ≠ "applies to no window" — it usually means the source doc didn't tag it.
- `object` is derived by matching the numeric part of the code against the shared SIF object-range table, best-effort (not authoritative for non-numeric codes like `SCSF10`).

**Field entry:**
```json
{
  "field_id": "SCS01",
  "collection": "SCS",
  "name": "Locally Assigned Student Identifier (LASID)",
  "description": "...",
  "type": "Alphanumeric",
  "min_length": 1,
  "max_length": 32,
  "acceptable_values": "...or null",
  "related_elements": "...or null",
  "notes": "...or null",
  "sif_object": "StudentPersonal",
  "sif_element": "LocalId"
}
```

**Appendix entry** (in the two `*-appendices.json` files):
```json
{
  "sheet_name": "Appendix A SR08 Race-Ethnicity",
  "collection": "EPIMS",
  "linked_fields": ["SR08"],
  "columns": ["Code", "Ethnicity: Race"],
  "rows": [["01", "Not Hispanic or Latino: White"], ...]
}
```

## Known limitations — read before treating any field as ground truth

1. **SIMS field codes (`DOE0##`) are positionally inferred**, not
   read directly off the field — the SIMS handbook's field headings
   don't include the DOE code, so codes were matched by aligning body
   heading order against the table-of-contents order. Counts were
   60 body headings vs. 59 ToC entries, so there is a mismatch
   *somewhere* in that list — likely near the end. Worth a spot check
   before relying on SIMS `field_id` values in the UI.

2. **EPIMS Staff Roster (`SR##`) and Work Assignment (`WA##`) field
   codes are positionally inferred**, not stated in the document —
   those two sections are plain numbered lists with no code in the
   heading at all. Codes were assigned by list position (item 1 =
   SR01/WA01, item 2 = SR02/WA02, etc.), cross-checked against a few
   codes mentioned elsewhere in the doc (e.g. "license number ...
   (SR03)" lines up with item 3). This is reasonably solid but not
   verified field-by-field — a field DESE has since discontinued and
   silently removed from the list (rather than marking "discontinued"
   in place) would shift every code after it. EPIMS `ID01`–`ID07`
   *are* stated directly in headings and are reliable.

3. **SSDR field codes** (`OFF ID`, `SCH NAME`, `SOT5`, etc.) are
   parsed from the heading text itself by splitting off leading
   ALL-CAPS tokens — reliable for the great majority, but any field
   with an unusual heading format could have parsed oddly. `SCS`,
   `EPIMS` field IDs come straight from explicit headings and are
   the most reliable of the four.

4. **`related_fields` on errors** is extracted via regex for
   patterns like `SCS0#`, `DOE0##`, `WA##`, `SR##`, `ID##` mentioned
   in the error's own title/description — it will miss a reference
   phrased in a way the regex doesn't cover, and won't catch SSDR's
   mnemonic-style codes (`OFF ID`) at all yet.

5. **File-level entries with no numeric code** (a handful in SIMS,
   e.g. "Student file: Invalid file - No records available") aren't
   captured in `errors[]` — the parser only picks up entries matching
   `PREFIX####`. Worth adding as a small hand-curated list if the UI
   needs to surface them.

6. Text is pulled from Word-via-pandoc conversion; formatting like
   tables embedded inside a field's Notes (e.g. SCS05's course-code
   coding-format table) was not reconstructed and may be thin or
   missing in `notes`/`description` for a handful of entries that
   relied on a table for their content.

None of this blocks using the data — the error lists (the highest
value for troubleshooting) are solid across all four collections.
The caveats above are about field-code reliability in the handbook
data specifically, so you know where to spot check before the UI
leans on them.
