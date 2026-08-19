namespace Portfolio.API.DTOs.Admin;

/// <summary>
/// The publish toggle, kept separate from the full update so the admin SPA can flip a project
/// live without resubmitting the whole form.
/// </summary>
public sealed class SetPublishedRequest
{
    public bool IsPublished { get; set; }
}
