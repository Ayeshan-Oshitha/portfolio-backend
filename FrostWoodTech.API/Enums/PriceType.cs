namespace FrostWoodTech.API.Enums;

/// <summary>Maps to the Postgres native enum <c>price_type</c>.</summary>
public enum PriceType
{
    Fixed,
    StartingFrom,
    Hourly,
    Monthly,
    Custom
}
