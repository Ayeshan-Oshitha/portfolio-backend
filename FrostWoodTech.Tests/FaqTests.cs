using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

[Collection(nameof(PostgresCollection))]
public class FaqTests
{
    private readonly PostgresFixture _fixture;

    public FaqTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_draft_faq_is_admin_only_until_it_is_published()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db);

        var request = NewFaq();
        request.IsPublished = false;

        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var publicBefore = await service.GetPublicFaqsAsync(Site.Agency, null, CancellationToken.None);
        Assert.DoesNotContain(publicBefore, f => f.Id == created.Value!.Id);

        var admin = await service.GetAdminFaqsAsync(null, null, null, null, 1, 50, CancellationToken.None);
        Assert.Contains(admin.Items, f => f.Id == created.Value!.Id);

        var published = await service.UpdateAsync(
            created.Value!.Id,
            new UpdateFaqRequest
            {
                Question = request.Question,
                Answer = request.Answer,
                Category = request.Category,
                IsPublished = true,
                ShowOnAgency = true
            },
            CancellationToken.None);

        Assert.True(published.IsSuccess);

        var publicAfter = await service.GetPublicFaqsAsync(Site.Agency, null, CancellationToken.None);
        Assert.Contains(publicAfter, f => f.Id == created.Value.Id);
    }

    [Fact]
    public async Task Faqs_can_be_filtered_by_category()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db);

        var category = $"Pricing {Guid.NewGuid():N}";

        var request = NewFaq();
        request.Category = category;

        var created = await service.CreateAsync(request, CancellationToken.None);
        Assert.True(created.IsSuccess);

        var matching = await service.GetPublicFaqsAsync(Site.Agency, category, CancellationToken.None);
        var other = await service.GetPublicFaqsAsync(Site.Agency, "Process", CancellationToken.None);

        Assert.Contains(matching, f => f.Id == created.Value!.Id);
        Assert.DoesNotContain(other, f => f.Id == created.Value!.Id);
    }

    [Fact]
    public async Task Featuring_on_a_site_the_faq_is_not_shown_on_is_rejected()
    {
        await using var db = _fixture.CreateContext();
        var service = new FaqService(db);

        var request = NewFaq();
        request.ShowOnPersonal = false;
        request.FeaturedOnPersonal = true;

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    private static CreateFaqRequest NewFaq() => new()
    {
        Question = $"How much does it cost? {Guid.NewGuid():N}",
        Answer = "It depends on the scope.",
        IsPublished = true,
        ShowOnAgency = true
    };
}
