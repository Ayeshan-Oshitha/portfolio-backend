namespace FrostWoodTech.API.DTOs.Admin;

public sealed class RejectUserRequest
{
    /// <summary>Required — the rejected user is told why, so it cannot be left blank.</summary>
    public string? Reason { get; set; }
}
