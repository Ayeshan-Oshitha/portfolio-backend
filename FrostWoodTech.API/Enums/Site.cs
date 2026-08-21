namespace FrostWoodTech.API.Enums;

/// <summary>
/// The two public frontends. Not a Postgres enum — visibility is stored as a pair of flag
/// columns per row, this only ever travels on the wire as <c>?site=</c>.
/// </summary>
public enum Site
{
    Agency,
    Personal
}
