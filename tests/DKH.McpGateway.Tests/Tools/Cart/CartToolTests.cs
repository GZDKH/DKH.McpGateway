using DKH.CartService.Contracts.Cart.Api.CartClaim.v1;
using DKH.CartService.Contracts.Cart.Api.CartCrud.v1;
using DKH.McpGateway.Application.Tools.Cart;
using DKH.Platform.Grpc.Common.Types;

namespace DKH.McpGateway.Tests.Tools.Cart;

public class CartToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly CartCrudService.CartCrudServiceClient _crud =
        Substitute.For<CartCrudService.CartCrudServiceClient>();

    private readonly CartClaimService.CartClaimServiceClient _claim =
        Substitute.For<CartClaimService.CartClaimServiceClient>();

    private static readonly string CartId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();
    private static readonly string StoreId = Guid.NewGuid().ToString();
    private static readonly string CashierUserId = Guid.NewGuid().ToString();

    // ---- ListCarts ----

    [Fact]
    public async Task ListCarts_HappyPath_ReturnsMetadataAsync()
    {
        SetupListCarts(new ListCartsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
        });

        var result = await ListCartsTool.ExecuteAsync(_auth, _crud);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListCarts_DeletedFilter_MapsToInactiveOnlyAsync()
    {
        SetupListCarts(new ListCartsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
        });

        await ListCartsTool.ExecuteAsync(_auth, _crud, storefrontId: StorefrontId, hasItems: true,
            softDeleteFilter: "deleted", page: 2, pageSize: 10);

        _ = _crud.Received(1).ListCartsAsync(
            Arg.Is<ListCartsRequest>(r =>
                r.StorefrontId.Value == StorefrontId &&
                r.HasItems &&
                r.SoftDeleteFilter == SoftDeleteFilter.InactiveOnly &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCarts_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _crud.ListCartsAsync(
                Arg.Any<ListCartsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListCartsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListCartsTool.ExecuteAsync(_auth, _crud);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    // ---- GetCart ----

    [Fact]
    public async Task GetCart_MissingCartId_ReturnsErrorAsync()
    {
        var result = await GetCartTool.ExecuteAsync(_auth, _crud, cartId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("cartId is required");
    }

    // ---- IssueClaimCode (write) ----

    [Fact]
    public async Task IssueClaimCode_MissingCartId_ReturnsErrorAsync()
    {
        var result = await IssueClaimCodeTool.ExecuteAsync(_auth, _claim, cartId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("cartId is required");
    }

    [Fact]
    public async Task IssueClaimCode_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => IssueClaimCodeTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _claim, cartId: CartId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ---- ClaimCart (write) ----

    [Theory]
    [InlineData("", "store", "cashier", "code is required")]
    [InlineData("FH4K", "", "cashier", "storeId is required")]
    [InlineData("FH4K", "store", "", "cashierUserId is required")]
    public async Task ClaimCart_MissingField_ReturnsErrorAsync(
        string code, string storeId, string cashierUserId, string expected)
    {
        var result = await ClaimCartTool.ExecuteAsync(
            _auth, _claim, code: code, storeId: storeId, cashierUserId: cashierUserId);

        Parse(result).GetProperty("error").GetString().Should().Contain(expected);
    }

    [Fact]
    public async Task ClaimCart_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => ClaimCartTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _claim, code: "FH4K", storeId: StoreId, cashierUserId: CashierUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private void SetupListCarts(ListCartsResponse response)
        => _crud.ListCartsAsync(
                Arg.Any<ListCartsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
