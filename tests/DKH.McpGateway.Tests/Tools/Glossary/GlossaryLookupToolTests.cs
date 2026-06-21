using DKH.McpGateway.Application.Tools.Glossary;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecAttributeManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.TagManagement.v1;

namespace DKH.McpGateway.Tests.Tools.Glossary;

public class GlossaryLookupToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly TagManagementService.TagManagementServiceClient _tags =
        Substitute.For<TagManagementService.TagManagementServiceClient>();
    private readonly SpecAttributeManagementService.SpecAttributeManagementServiceClient _specs =
        Substitute.For<SpecAttributeManagementService.SpecAttributeManagementServiceClient>();
    private readonly CategoryManagementService.CategoryManagementServiceClient _categories =
        Substitute.For<CategoryManagementService.CategoryManagementServiceClient>();

    public GlossaryLookupToolTests()
    {
        SetupTags(new ListTagsResponse());
        SetupSpecs(new ListSpecAttributesResponse());
        SetupCategories(new GetCategoryTreeResponse());
    }

    [Fact]
    public async Task GlossaryLookup_EmptyTerm_ReturnsErrorAsync()
    {
        var result = await ExecuteToolAsync(" ");

        Parse(result).GetProperty("success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task GlossaryLookup_CategoryMatch_ReturnsMatchesAsync()
    {
        var tree = new GetCategoryTreeResponse();
        var root = new CategoryNode { Name = "Oolong Teas", SeoName = "oolong-teas", ProductCount = 5 };
        root.Children.Add(new CategoryNode { Name = "Dark Oolong", SeoName = "dark-oolong", ProductCount = 2 });
        tree.RootCategories.Add(root);
        tree.RootCategories.Add(new CategoryNode { Name = "Green Teas", SeoName = "green-teas" });
        SetupCategories(tree);

        var result = await ExecuteToolAsync("oolong", type: "category");

        var categories = Parse(result).GetProperty("matches").GetProperty("categories");
        categories.GetArrayLength().Should().Be(2);
        categories[0].GetProperty("name").GetString().Should().Be("Oolong Teas");
        categories[0].GetProperty("type").GetString().Should().Be("category");
    }

    [Fact]
    public async Task GlossaryLookup_ExactMatch_BecomesCanonicalAsync()
    {
        var tree = new GetCategoryTreeResponse();
        tree.RootCategories.Add(new CategoryNode { Name = "Oolong Teas", SeoName = "oolong-teas" });
        SetupCategories(tree);

        var result = await ExecuteToolAsync("Oolong Teas", type: "category");

        var canonical = Parse(result).GetProperty("canonical");
        canonical.GetProperty("name").GetString().Should().Be("Oolong Teas");
        canonical.GetProperty("type").GetString().Should().Be("category");
    }

    [Fact]
    public async Task GlossaryLookup_NoMatches_CanonicalIsNullAsync()
    {
        var result = await ExecuteToolAsync("nonexistent", type: "category");

        Parse(result).GetProperty("canonical").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GlossaryLookup_TagType_QueriesTagClientAsync()
    {
        await ExecuteToolAsync("caffeine", type: "tag");

        _ = _tags.Received(1).ListAsync(
            Arg.Is<ListTagsRequest>(r => r.Search == "caffeine" && r.Language == "ru"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        _ = _categories.DidNotReceive().GetCategoryTreeAsync(
            Arg.Any<GetCategoryTreeRequest>(),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GlossaryLookup_AllType_QueriesEverySourceAsync()
    {
        await ExecuteToolAsync("oolong");

        _ = _tags.Received(1).ListAsync(
            Arg.Any<ListTagsRequest>(),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        _ = _specs.Received(1).ListAsync(
            Arg.Any<ListSpecAttributesRequest>(),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        _ = _categories.Received(1).GetCategoryTreeAsync(
            Arg.Any<GetCategoryTreeRequest>(),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    private Task<string> ExecuteToolAsync(
        string term,
        string type = "all",
        string lang = "ru",
        string catalogSeoName = "main-catalog",
        int limit = 10)
        => GlossaryLookupTool.ExecuteAsync(
            _auth,
            _tags,
            _specs,
            _categories,
            term: term,
            type: type,
            lang: lang,
            catalogSeoName: catalogSeoName,
            limit: limit);

    private void SetupTags(ListTagsResponse response)
        => _tags.ListAsync(
                Arg.Any<ListTagsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupSpecs(ListSpecAttributesResponse response)
        => _specs.ListAsync(
                Arg.Any<ListSpecAttributesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupCategories(GetCategoryTreeResponse response)
        => _categories.GetCategoryTreeAsync(
                Arg.Any<GetCategoryTreeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
