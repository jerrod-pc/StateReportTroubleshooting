namespace StateReportTroubleshooting.Models;

public enum SearchHitKind
{
    Error,
    Field
}

public class SearchHit
{
    public required string Collection { get; init; }
    public required SearchHitKind Kind { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
}
