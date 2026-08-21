namespace Portfolio.API.Enums;

/// <summary>
/// How the dedicated public reviews page orders its list. Query-string only — nothing persists
/// this, so it is not a Postgres enum.
/// </summary>
public enum ReviewSortOption
{
    Latest,
    Rating,
    Country
}
