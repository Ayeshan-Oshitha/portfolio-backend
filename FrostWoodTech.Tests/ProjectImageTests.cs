using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

/// <summary>
/// "Exactly one primary image per project" is enforced by a partial unique index in Postgres and
/// by the service clearing the old primary in the same save. Both halves have to hold: the index
/// alone would throw, the service alone would drift.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class ProjectImageTests
{
    private readonly PostgresFixture _fixture;

    public ProjectImageTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Setting_a_new_primary_image_clears_the_previous_one()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProjectService(db, new FakeMediaService());

        var project = await service.CreateAsync(NewProject(), CancellationToken.None);
        Assert.True(project.IsSuccess);

        var first = await service.AddImageAsync(
            project.Value!.Id, NewImage("first", isPrimary: true), CancellationToken.None);

        var second = await service.AddImageAsync(
            project.Value.Id, NewImage("second", isPrimary: true), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var primaries = await db.ProjectImages
            .AsNoTracking()
            .Where(i => i.ProjectId == project.Value.Id && i.IsPrimary)
            .Select(i => i.Id)
            .ToListAsync(CancellationToken.None);

        Assert.Equal([second.Value!.Id], primaries);
    }

    [Fact]
    public async Task An_image_without_alt_text_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new ProjectService(db, new FakeMediaService());

        var project = await service.CreateAsync(NewProject(), CancellationToken.None);
        Assert.True(project.IsSuccess);

        var request = NewImage("no-alt", isPrimary: false);
        request.AltText = "   ";

        var result = await service.AddImageAsync(project.Value!.Id, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    private static CreateProjectRequest NewProject() => new()
    {
        Title = $"A project {Guid.NewGuid():N}",
        Year = 2026,
        ShortDescription = "Short.",
        Description = "Long.",
        IsPublished = true,
        ShowOnAgency = true
    };

    private static AddProjectImageRequest NewImage(string name, bool isPrimary) => new()
    {
        ObjectKey = $"frostwoodtech/projects/test/{name}-{Guid.NewGuid():N}",
        Url = "https://res.cloudinary.com/demo/image/upload/sample.jpg",
        AltText = "A screenshot of the project.",
        Width = 1200,
        Height = 800,
        IsPrimary = isPrimary
    };
}
