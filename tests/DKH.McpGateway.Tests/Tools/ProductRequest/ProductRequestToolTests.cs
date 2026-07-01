using System.Globalization;
using DKH.McpGateway.Application.Tools.Payment;
using DKH.McpGateway.Application.Tools.ProductRequest;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductRequestService.Contracts.ProductRequest.Api.ProductRequestCrud.v1;
using DKH.ProductRequestService.Contracts.ProductRequest.Models.v1;

namespace DKH.McpGateway.Tests.Tools.ProductRequest;

public sealed class ProductRequestToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly ProductRequestCrudService.ProductRequestCrudServiceClient _client =
        Substitute.For<ProductRequestCrudService.ProductRequestCrudServiceClient>();

    private static readonly string RequestId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();
    private static readonly string ProductId = Guid.NewGuid().ToString();

    [Fact]
    public void ProductRequestToolSurface_DefinesSpecTools()
    {
        string[] expected =
        [
            "get_product_request",
            "list_product_requests",
            "create_product_request",
            "update_product_request",
            "delete_product_request",
            "restore_product_request",
            "permanently_delete_product_request",
            "start_review_product_request",
            "mark_found_product_request",
            "mark_not_found_product_request",
            "cancel_product_request",
            "set_product_request_translation",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.ProductRequest")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetProductRequest_MissingId_ReturnsErrorAsync()
    {
        var result = await GetProductRequestTool.ExecuteAsync(_auth, _client, id: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("id is required");
    }

    [Fact]
    public async Task ListProductRequests_HappyPath_MapsFiltersAndMetadataAsync()
    {
        var response = new ListProductRequestsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 1, CurrentPage = 2, PageSize = 10 },
        };
        response.Items.Add(Model(status: ProductRequestStatus.InReview, customerId: "customer-1"));

        _client.ListAsync(Arg.Any<ListProductRequestsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await ListProductRequestsTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: StorefrontId,
            status: "in_review",
            customerId: "customer-1",
            softDeleteFilter: "deleted",
            page: 2,
            pageSize: 10));

        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("page").GetInt32().Should().Be(2);
        json.GetProperty("pageSize").GetInt32().Should().Be(10);
        json.GetProperty("items")[0].GetProperty("customerId").GetString().Should().Be("customer-1");
        json.GetProperty("items")[0].GetProperty("status").GetString().Should().Be("InReview");

        _ = _client.Received(1).ListAsync(
            Arg.Is<ListProductRequestsRequest>(r =>
                r.StorefrontId.Value == StorefrontId &&
                r.Status == ProductRequestStatus.InReview &&
                r.CustomerId == "customer-1" &&
                r.SoftDeleteFilter == SoftDeleteFilter.InactiveOnly &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProductRequest_HappyPath_SetsRequestAndRetainsOpaqueCustomerIdAsync()
    {
        _client.CreateAsync(Arg.Any<CreateProductRequestRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(Model(customerId: "customer-1")));

        var json = Parse(await CreateProductRequestTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: StorefrontId,
            customerId: "customer-1",
            languageCode: "en",
            productName: "Jasmine tea",
            description: "Loose leaf",
            category: "tea",
            sourceUrl: "https://example.test/tea",
            desiredPrice: 12.5,
            desiredQuantity: 2,
            photoUrls: "https://cdn.test/1.jpg, https://cdn.test/2.jpg"));

        json.GetProperty("customerId").GetString().Should().Be("customer-1");
        var raw = json.GetRawText().ToLowerInvariant();
        raw.Should().NotContain("email");
        raw.Should().NotContain("phone");

        _ = _client.Received(1).CreateAsync(
            Arg.Is<CreateProductRequestRequest>(r =>
                r.StorefrontId.Value == StorefrontId &&
                r.CustomerId == "customer-1" &&
                r.LanguageCode == "en" &&
                r.ProductName == "Jasmine tea" &&
                r.Category == "tea" &&
                r.DesiredPrice == 12.5 &&
                r.DesiredQuantity == 2 &&
                r.PhotoUrls.Count == 2 &&
                r.PhotoUrls[1] == "https://cdn.test/2.jpg"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PermanentlyDeleteProductRequest_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => PermanentlyDeleteProductRequestTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, id: RequestId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MarkFoundProductRequest_MissingProductId_ReturnsErrorAsync()
    {
        var result = await MarkFoundProductRequestTool.ExecuteAsync(
            _auth,
            _client,
            id: RequestId,
            productId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("productId is required");
    }

    [Fact]
    public async Task MarkFoundProductRequest_HappyPath_SetsProductIdAndNoteAsync()
    {
        _client.MarkFoundAsync(Arg.Any<MarkFoundRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(Model(status: ProductRequestStatus.Found)));

        var json = Parse(await MarkFoundProductRequestTool.ExecuteAsync(
            _auth,
            _client,
            id: RequestId,
            productId: ProductId,
            note: "matched catalog product"));

        json.GetProperty("status").GetString().Should().Be("Found");
        _ = _client.Received(1).MarkFoundAsync(
            Arg.Is<MarkFoundRequest>(r =>
                r.Id.Value == RequestId &&
                r.ProductId.Value == ProductId &&
                r.Note == "matched catalog product"),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    private static ProductRequestModel Model(
        ProductRequestStatus status = ProductRequestStatus.New,
        string customerId = "customer-1")
    {
        var model = new ProductRequestModel
        {
            Id = new GuidValue(RequestId),
            StorefrontId = new GuidValue(StorefrontId),
            CustomerId = customerId,
            Status = status,
            LanguageCode = "en",
            ProductName = "Jasmine tea",
            Description = "Loose leaf",
            Category = "tea",
            SourceUrl = "https://example.test/tea",
            DesiredPrice = 12.5,
            DesiredQuantity = 2,
            AdminNotes = "admin note",
            ResolutionNote = "resolved",
            FoundProductId = new GuidValue(ProductId),
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-01-02T03:04:05Z", CultureInfo.InvariantCulture)),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-01-03T03:04:05Z", CultureInfo.InvariantCulture)),
        };
        model.PhotoUrls.Add("https://cdn.test/1.jpg");
        model.Translations.Add(new ProductRequestTranslationModel
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            LanguageCode = "ru",
            ProductName = "Jasmine tea RU",
            Description = "Loose leaf RU",
            AdminNotes = "admin note RU",
        });

        return model;
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
