using DKH.McpGateway.Application.Tools.Subscription;
using DKH.Platform.Grpc.Common.Types;
using DKH.SubscriptionService.Contracts.Subscription.Api.SubscriptionCrud.v1;

namespace DKH.McpGateway.Tests.Tools.Subscription;

public class SubscriptionToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly SubscriptionCrudService.SubscriptionCrudServiceClient _client =
        Substitute.For<SubscriptionCrudService.SubscriptionCrudServiceClient>();

    private static readonly string UserId = Guid.NewGuid().ToString();

    [Fact]
    public async Task ListPlans_HappyPath_ReturnsItemsAsync()
    {
        _client.ListPlansAsync(
                Arg.Any<ListPlansRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListPlansResponse()));

        var result = await ListPlansTool.ExecuteAsync(_auth, _client);

        Parse(result).GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetUserSubscription_MissingUserId_ReturnsErrorAsync()
    {
        var result = await GetUserSubscriptionTool.ExecuteAsync(_auth, _client, userId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("userId is required");
    }

    [Fact]
    public async Task ListUserSubscriptions_HappyPath_ReturnsMetadataAsync()
    {
        SetupListUserSubscriptions(new ListUserSubscriptionsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
        });

        var result = await ListUserSubscriptionsTool.ExecuteAsync(_auth, _client, userId: UserId);

        Parse(result).GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task ListUserSubscriptions_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListUserSubscriptionsAsync(
                Arg.Any<ListUserSubscriptionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListUserSubscriptionsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListUserSubscriptionsTool.ExecuteAsync(_auth, _client, userId: UserId);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private void SetupListUserSubscriptions(ListUserSubscriptionsResponse response)
        => _client.ListUserSubscriptionsAsync(
                Arg.Any<ListUserSubscriptionsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
