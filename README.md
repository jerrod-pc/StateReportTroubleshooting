# SRTroubleshooting.HELP!

A Blazor WebAssembly reference tool for troubleshooting Massachusetts DESE
state-reporting errors across four collections: **SCS**, **EPIMS**, **SIMS**,
and **SSDR**. Look up an error code and see its description, example, every
related field's full spec (type, length, acceptable values, SIF mapping),
and any linked code-table appendix, all on one screen — instead of hunting
through four separate PDFs.

Live at **[srtroubleshooting.help](https://srtroubleshooting.help)**.

---

## Stack

- **Blazor WebAssembly (standalone), .NET 9** — no backend; the entire app
  ships as static files and runs client-side.
- **Bootstrap 5** (vendored under `wwwroot/lib/bootstrap`) for layout/styling,
  plus a small custom `wwwroot/css/app.css` for the brand theme and
  light/dark mode.
- **Data**: static JSON files under `wwwroot/data/`, fetched via `HttpClient`
  on startup. No database, no API — everything the app knows is baked into
  those JSON files at commit time.
- **Hosting**: GitHub Pages, deployed via GitHub Actions
  (`.github/workflows/deploy-pages.yml`) on every push to `master`. Custom
  domain via `wwwroot/CNAME` (`srtroubleshooting.help`).

## Local development

```bash
cd src/StateReportTroubleshooting
dotnet run              # dev server, hot reload
dotnet build             # compile check
dotnet publish -c Release -o release   # what CI actually ships
```

There's nothing to configure — no connection strings, no secrets, no `.env`.

## Project structure

```
source-docs/               Original DESE PDFs/xlsx this app's data was built from
                            (also copied into wwwroot/resources/ for the Resources
                            page's public downloads)
source-data/                Hand-edited JSON, source of truth for wwwroot/data/
  README.md                 Notes from the original PDF→JSON conversion pass
                             (schema details + known positional-inference caveats)
  scs.json, epims.json,
  sims.json, ssdr.json       One file per collection: object_ranges + errors[] + fields[]
  epims-appendices.json,
  sims-appendices.json,
  ssdr-appendices.json       Code lookup tables (race/ethnicity, course codes,
                             offense codes, etc.) that a field's "see appendix"
                             acceptable_values points to
  index.json                 Flat error+field code list; NOT shipped to wwwroot,
                             kept only as a possible future search-index source

src/StateReportTroubleshooting/
  Program.cs                 DI setup: one scoped HttpClient, one scoped
                             TroubleshootingDataService
  Models/                     Plain POCOs mirroring the JSON schema exactly
                             (see source-data/README.md for field-by-field docs)
  Services/
    TroubleshootingDataService.cs   Loads all 4 collections + EPIMS/SSDR
                                     appendices eagerly on startup; SIMS
                                     appendices (~1.4 MB, mostly the 5,000-row
                                     degree-institution table) load lazily on
                                     first access. Builds in-memory indexes for
                                     error↔field cross-references and
                                     field↔appendix links.
    TextFormat.cs               Turns flattened, whitespace-mangled source text
                                 back into readable lines/paragraphs for display
                                 (see "Dataset quirks" below) — never rewrites
                                 the underlying text, only where line breaks go.
  Layout/                     MainLayout (nav bar, dark-mode toggle), NavMenu
  Pages/
    Home.razor                 /  — quick jump + collection cards + stats
    CollectionBrowse.razor     /collection/{Collection}  — Errors / Fields /
                               Appendices tabs with client-side filtering
    ErrorDetail.razor          /collection/{Collection}/errors/{Code}
    FieldDetail.razor          /collection/{Collection}/fields/{FieldId}
    AppendixDetail.razor       /collection/{Collection}/appendices/{SheetSlug}
    Search.razor                /search?q=  — global search across all 4 collections
    Resources.razor            /resources — quick links + original-document downloads
  Shared/                     FormattedText, AppendixTable, FieldSummaryCard,
                             QuickJumpBox, WindowBadge, ThemeToggle, LoadingSplash
  wwwroot/
    data/                     Deployed copy of source-data/*.json (kept in sync
                             by hand — there's no build step that copies these)
    resources/                Deployed copy of source-docs/* for the public
                             Resources → Downloads section
    CNAME, 404.html            GitHub Pages custom-domain plumbing
```

**Important**: `source-data/*.json` and `src/.../wwwroot/data/*.json` are two
copies of the same files, kept in sync manually. Any data fix must be applied
to (or copied into) both, or the deployed site won't reflect it. Same for
`source-docs/*` → `wwwroot/resources/*`.

## Data model

Each of `scs.json` / `epims.json` / `sims.json` / `ssdr.json`:

```json
{
  "collection": "SCS",
  "error_list_version": "10.3",
  "handbook_version": "9.4",
  "object_ranges": [{ "object": "...", "start": 0, "end": 0 }],
  "errors": [ { "code", "collection", "title", "description", "example",
                "elements_affected", "collection_windows", "related_fields",
                "object" } ],
  "fields": [ { "field_id", "collection", "name", "description", "type",
                "min_length", "max_length", "acceptable_values",
                "related_elements", "notes", "sif_object", "sif_element" } ]
}
```

`epims-appendices.json` / `sims-appendices.json` / `ssdr-appendices.json`:

```json
{ "collection": "EPIMS",
  "appendices": [ { "sheet_name", "collection", "linked_fields": [],
                     "columns": [], "rows": [[...]] } ] }
```

Current size, as of the last audit pass:

| Collection | Errors | Fields | Error list v | Handbook v |
|---|---|---|---|---|
| SIMS  | 334 | 60 | 21.2 | 31.4 |
| EPIMS | 207 | 62 | 9.9  | 10.4 |
| SCS   | 120 | 14 | 10.3 | 9.4  |
| SSDR  | 89  | 50 | 10.0 | 23.2 |

## Dataset quirks

The source data was extracted from DESE's Word/PDF handbooks and error lists
(`pdftotext -layout`, piped through `tr -d '\f'` to drop form-feed page
breaks). That extraction process leaves fingerprints that both the data and
the app's rendering code have to account for:

- **Flattened paragraph breaks.** A run of **2+ spaces** in `notes` /
  `acceptable_values` / `description` marks where an original paragraph or
  line break got collapsed during extraction. `TextFormat.SplitIntoItems`
  splits on this to recover readable paragraphs; a single space is normal
  sentence spacing and is left alone.
- **Literal `\n` enumerated lists.** Around 50 fields (e.g. `DOE032`, `SCS08`,
  `SR18`, `DAT`) store their `code = description` value lists as real
  newline-separated strings instead of the flattened form above. A lone `\n`
  isn't 2+ whitespace, so it needed its own, higher-priority split path —
  added after `DOE032` was reported as rendering like a wall of text.
- **Numbered/coded lists with no whitespace signal at all** (e.g.
  `"00 Not Title I 14 Targeted/science ..."`) are detected by a regex for
  `\d{1,3}[.)]\s+` or a bare 2-digit code immediately followed by a capital
  letter — deliberately narrow so it doesn't misfire on ordinary prose like
  "grade 12 (SP)".
- **Stray pandoc escape backslashes** — `\'`, `\"`, `\<`, `\>`, `\.`, `\^` —
  leaked into plain text and rendered as literal backslashes (e.g. `it\'s a
  typo`, `DOE042\<\>500`). Stripped dataset-wide; see the audit log below.
- **`�` replacement character** substituting for an en-dash in some
  documents' separator characters — broke naive dash-splitting regexes during
  parsing.
- **OCR-style single-letter splits**, e.g. `"T he"` → `"The"`, `"S IF"` →
  `"SIF"` — a recurring artifact in the raw PDF text, cleaned up wherever
  found during parsing.
- **Duplicate TOC-vs-body headers.** Several source PDFs list every
  field/error twice — once in an abbreviated front-matter list, once in the
  real detailed body — and both can match a heading regex, corrupting chunk
  boundaries if not deduped.
- **Multi-column tables with desynchronized row heights**, where a cell's
  wrapped text bleeds onto the *next* row in a naive linear extraction (e.g.
  SSDR's offense-code table originally showed code `0030`'s row with the
  description that actually belonged to `0040`).
- **SIF Object/Element truncation.** `sif_element` uses `/` to separate an
  element from its sub-element (e.g. `Name/LastName`), but the dotted display
  form (`StudentPersonal.Name.LastName`) is what's shown — see `FieldEntry.SifMapping`.
  A pre-audit bug displayed only `StudentPersonal.Name`, silently dropping
  the sub-element, which read as "write to `Name`" instead of `Name.LastName`.
- **Whole extra sections accidentally glued onto one entry.** The most
  serious quirk: several error/field entries absorbed pages of unrelated
  handbook content because the original extraction script didn't recognize
  where the next real entry began. Found and fixed in five places so far
  (SIMS9990, EPIMS9910, SSDR9920, SCS9740, EPIMS `ID07`) — see the audit log.
- **Positional field-code inference** for SIMS `DOE0##` and EPIMS `SR##`/`WA##`
  — those sections' headings in the source docs don't include the code
  itself, so codes were assigned by list position and cross-checked against
  incidental mentions elsewhere in the text. Reasonably solid but not
  verified field-by-field; see `source-data/README.md` for the full caveat
  list (SSDR field-code parsing, `related_fields` regex coverage gaps, and a
  handful of non-numeric-code error entries that aren't captured at all).

## Data-integrity audit — what's been found and fixed

This app went through a dedicated data-integrity pass after the initial
build. In rough chronological order:

1. **SIF mapping display bug.** `StudentPersonal.Name.LastName` was rendering
   as just `StudentPersonal.Name` (the sub-element after `/` was silently
   dropped). Fixed with `FieldEntry.SifMapping`, then used to audit and
   correct `sif_object`/`sif_element` on **140 fields** across all 4
   collections against the real handbooks — including several genuinely
   dual/conditional mappings (e.g. EPIMS `WA06`, `WA09`, `WA17`, `SR09`) that
   were preserved rather than collapsed to one value.
2. **"SIF Validations" appendix bleed.** `SIMS9990`, `EPIMS9910`, `SSDR9920`,
   and `SCS9740` each had an entire separate section of the handbook — a list
   of ~15–105 additional SIF validation rules — accidentally concatenated
   into their `description`. Split out into **165 new, properly cross-linked
   `SIF1xxx` error entries** (SIMS 105, EPIMS 17, SCS 29, SSDR 14), and each
   original entry's title/description restored to its real, short content.
3. **SSDR Appendix A (offense codes) infrastructure.** Discovered as a
   second, distinct problem inside `SSDR9920`: a 92-row offense-code
   reference table, also folded into the same field, additionally scrambled
   by the row-desync issue above. Full appendix-tab/lazy-load infrastructure
   was built to match EPIMS/SIMS (shipped first with an explanatory
   placeholder, since the only available source was too scrambled to trust),
   then populated with all 92 rows once a clean transcription was provided.
4. **Formatted-text readability.** `Acceptable Values`, `Notes`, and
   `Related Elements` were rendering as single dense paragraphs even where
   the source data had clear internal structure. Added the double-space and
   literal-`\n` splitting described above, plus made `Related Elements`
   render through the same formatter (previously plain unformatted text).
   A follow-up fix separated the *visual* styling of a real list (dashed
   rule between entries) from a plain recovered paragraph break (plain
   spacing) — the same rule was firing after every paragraph and reading as
   a stray horizontal line.
5. **Stray pandoc escape backslashes**, dataset-wide (26 error entries + 9
   field `notes`) — see "Dataset quirks" above.
6. **Three SSDR fields with bled-in content**: `DISC IND` and `PST` had a
   fragment of a *different* field's Y/N or coded-value table appended to
   their own `acceptable_values`; `OFF DESC` had the same fragment appended
   after its real (and otherwise correct) value. Found by comparing against
   sibling fields (`OFF DESC2`/`OFF DESC3`) that didn't have the bleed.
7. **EPIMS `ID07` notes bleed** — the largest single instance of the "whole
   section glued on" bug: `ID07`'s `notes` had grown to 14,562 characters,
   having absorbed the *entire* "Staff Roster Data Elements" and "Work
   Assignment Data Elements" sections of the EPIMS handbook (SR01–SR38,
   WA01–WA17) after its own one-sentence note. Verified against the real
   handbook PDF that every fragment already had a complete, correct home on
   its real field — pure duplicate overflow, not missing content — and
   truncated `ID07`'s notes back to just its own text.

## Known remaining limitations

- **EPIMS `WA10` notes is `null`** in the current data, but the handbook has
  real notes content for it (the Appendix G1/G2/G3 course-code format
  explanation) that was never captured. Found during the `ID07` audit but
  out of scope for that fix — not yet backfilled.
- The positional-inference caveats in `source-data/README.md` for SIMS
  `DOE0##` and EPIMS `SR##`/`WA##` codes still apply — they're reasonably
  solid but not verified field-by-field.
- `related_fields` on errors is extracted via regex for known code patterns
  and can miss references phrased unusually, or SSDR's mnemonic-style codes
  (`OFF ID`, etc.).
- A handful of file-level error entries with no numeric code (e.g. "Student
  file: Invalid file - No records available") aren't captured in `errors[]`
  at all.
- `sif-mapping-review.txt` at the repo root is a plain-text dump of every
  field's SIF mapping, generated on request for manual spot-checking. It's
  deliberately left untracked (never `git add`ed) rather than gitignored —
  regenerate or delete as needed; it's not part of the app.

## Deployment

Every push to `master` triggers `.github/workflows/deploy-pages.yml`, which
runs `dotnet publish -c Release`, disables Jekyll processing (`.nojekyll`),
and deploys `release/wwwroot` to GitHub Pages. The custom domain
(`srtroubleshooting.help`) is configured via `wwwroot/CNAME`, which GitHub
Pages requires on every deploy.

## Contact

Data corrections, new DESE versions, or dead links: see the mailto on the
Resources page (`jcampion@rediker.com`).
