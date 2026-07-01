using DKH.McpGateway.Application.Tools.Media;
using DKH.MediaService.Contracts.Api.V1;
using DKH.MediaService.Contracts.Entities.V1;

namespace DKH.McpGateway.Tests.Tools.Media;

public class MediaToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly AssetService.AssetServiceClient _assets =
        Substitute.For<AssetService.AssetServiceClient>();

    private readonly AttachmentService.AttachmentServiceClient _attachments =
        Substitute.For<AttachmentService.AttachmentServiceClient>();

    private readonly ScopeRegistryService.ScopeRegistryServiceClient _scopes =
        Substitute.For<ScopeRegistryService.ScopeRegistryServiceClient>();

    private readonly UploadSessionService.UploadSessionServiceClient _uploads =
        Substitute.For<UploadSessionService.UploadSessionServiceClient>();

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    // ---- guards: required args ----

    [Fact]
    public async Task GetAsset_MissingAssetId_ReturnsErrorAsync()
    {
        var result = await GetAssetTool.ExecuteAsync(_auth, _assets, assetId: "");
        Parse(result).GetProperty("error").GetString().Should().Contain("assetId is required");
    }

    [Fact]
    public async Task GetAssetDownloadLink_MissingAssetId_ReturnsErrorAsync()
    {
        var result = await GetAssetDownloadLinkTool.ExecuteAsync(_auth, _assets, assetId: "");
        Parse(result).GetProperty("error").GetString().Should().Contain("assetId is required");
    }

    [Fact]
    public async Task GetAttachments_MissingScope_ReturnsErrorAsync()
    {
        var result = await GetAttachmentsTool.ExecuteAsync(_auth, _attachments, scope: "", scopeKey: "k");
        Parse(result).GetProperty("error").GetString().Should().Contain("scope is required");
    }

    [Fact]
    public async Task GetAttachments_MissingScopeKey_ReturnsErrorAsync()
    {
        var result = await GetAttachmentsTool.ExecuteAsync(_auth, _attachments, scope: "product", scopeKey: "");
        Parse(result).GetProperty("error").GetString().Should().Contain("scopeKey is required");
    }

    [Fact]
    public async Task Detach_MissingAttachmentId_ReturnsErrorAsync()
    {
        var result = await DetachTool.ExecuteAsync(_auth, _attachments, attachmentId: "");
        Parse(result).GetProperty("error").GetString().Should().Contain("attachmentId is required");
    }

    [Fact]
    public async Task UpdateMetadata_MissingAttachmentId_ReturnsErrorAsync()
    {
        var result = await UpdateAttachmentMetadataTool.ExecuteAsync(_auth, _attachments, attachmentId: "");
        Parse(result).GetProperty("error").GetString().Should().Contain("attachmentId is required");
    }

    [Fact]
    public async Task CreateUploadSession_MissingScope_ReturnsErrorAsync()
    {
        var result = await CreateUploadSessionTool.ExecuteAsync(_auth, _uploads, scope: "", scopeKey: "k");
        Parse(result).GetProperty("error").GetString().Should().Contain("scope is required");
    }

    [Fact]
    public async Task ChangeSortOrder_MissingAttachmentId_ReturnsErrorAsync()
    {
        var result = await ChangeAttachmentSortOrderTool.ExecuteAsync(
            _auth,
            _attachments,
            scope: "product",
            scopeKey: "p-1",
            role: "gallery",
            items: /*lang=json,strict*/ """[{"sortOrder":1}]""");

        Parse(result).GetProperty("error").GetString().Should().Contain("items[].attachmentId is required");
    }

    // ---- happy paths ----

    [Fact]
    public async Task GetAsset_HappyPath_ReturnsStorageRefAsync()
    {
        _assets.GetAssetAsync(
                Arg.Any<GetAssetRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new MediaRefModel
            {
                AssetId = "asset-1",
                Container = "media",
                Key = "a/b.png",
                ContentType = "image/png",
                LengthBytes = 42,
            }));

        var result = await GetAssetTool.ExecuteAsync(_auth, _assets, assetId: "asset-1");

        var json = Parse(result);
        json.GetProperty("assetId").GetString().Should().Be("asset-1");
        json.GetProperty("lengthBytes").GetInt64().Should().Be(42);
    }

    [Fact]
    public async Task ListScopes_HappyPath_ReturnsScopesArrayAsync()
    {
        _scopes.ListScopesAsync(
                Arg.Any<ListScopesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListScopesResponse()));

        var result = await ListScopesTool.ExecuteAsync(_auth, _scopes);

        Parse(result).GetProperty("scopes").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetAttachments_HappyPath_OmitsActorPiiAsync()
    {
        _attachments.GetAttachmentsAsync(
                Arg.Any<GetAttachmentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetAttachmentsResponse
            {
                Attachments = { new AttachmentModel { AttachmentId = "att-1", AssetId = "asset-1" } },
            }));

        var result = await GetAttachmentsTool.ExecuteAsync(
            _auth, _attachments, scope: "product", scopeKey: "p-1");

        var attachment = Parse(result).GetProperty("attachments")[0];
        attachment.GetProperty("attachmentId").GetString().Should().Be("att-1");
        // Actor identifier must never be surfaced (gateway "no PII / no actor id" rule).
        attachment.TryGetProperty("attachedById", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetAttachments_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _attachments.GetAttachmentsAsync(
                Arg.Any<GetAttachmentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetAttachmentsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => GetAttachmentsTool.ExecuteAsync(_auth, _attachments, scope: "product", scopeKey: "p-1");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }
}
