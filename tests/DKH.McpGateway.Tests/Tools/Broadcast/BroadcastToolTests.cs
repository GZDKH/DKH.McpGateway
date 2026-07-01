using System.Globalization;
using DKH.BroadcastService.Contracts.Broadcast.Api.v1;
using DKH.BroadcastService.Contracts.Broadcast.Models.v1;
using DKH.McpGateway.Application.Tools.Broadcast;
using DKH.McpGateway.Application.Tools.Payment;
using DKH.Platform.Grpc.Common.Types;
using BroadcastGrpc = DKH.BroadcastService.Contracts.Broadcast.Api.v1.BroadcastService;

namespace DKH.McpGateway.Tests.Tools.Broadcast;

public sealed class BroadcastToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly BroadcastGrpc.BroadcastServiceClient _client =
        Substitute.For<BroadcastGrpc.BroadcastServiceClient>();

    private static readonly string BroadcastId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    [Fact]
    public void BroadcastToolSurface_DefinesSpecToolsOnly()
    {
        string[] expected =
        [
            "get_broadcast",
            "list_broadcasts",
            "create_broadcast",
            "update_broadcast",
            "delete_broadcast",
            "retry_broadcast",
            "cancel_broadcast",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Broadcast")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(expected);
        actual.Should().NotContain("report_delivery_result");
    }

    [Fact]
    public async Task GetBroadcast_MissingId_ReturnsErrorAsync()
    {
        var result = await GetBroadcastTool.ExecuteAsync(_auth, _client, broadcastId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("broadcastId is required");
    }

    [Fact]
    public async Task ListBroadcasts_HappyPath_MapsFiltersAndMetadataAsync()
    {
        var response = new ListBroadcastsResponse
        {
            Metadata = new PaginationMetadata { TotalCount = 1, CurrentPage = 2, PageSize = 10 },
        };
        response.Items.Add(Model(status: BroadcastStatus.Pending));

        _client.ListBroadcastsAsync(Arg.Any<ListBroadcastsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await ListBroadcastsTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: StorefrontId,
            targetType: "telegram",
            status: "Pending",
            page: 2,
            pageSize: 10));

        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("page").GetInt32().Should().Be(2);
        json.GetProperty("pageSize").GetInt32().Should().Be(10);
        json.GetProperty("items")[0].GetProperty("targetConfig").GetString()
            .Should().Be(/*lang=json,strict*/ """{"channel":"news"}""");

        _ = _client.Received(1).ListBroadcastsAsync(
            Arg.Is<ListBroadcastsRequest>(r =>
                r.StorefrontId.Value == StorefrontId &&
                r.TargetType == "telegram" &&
                r.Status == BroadcastStatus.Pending &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBroadcast_HappyPath_SetsRequestAndMapsResponseAsync()
    {
        _client.CreateBroadcastAsync(Arg.Any<CreateBroadcastRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(Model(status: BroadcastStatus.Pending, recurrence: RecurrenceType.Daily)));

        var json = Parse(await CreateBroadcastTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: StorefrontId,
            targetType: "telegram",
            targetConfig: /*lang=json,strict*/ """{"channel":"news"}""",
            text: "Maintenance at 10:00",
            parseMode: "HTML",
            scheduledAt: "2026-01-02T03:04:05Z",
            timeZoneId: "Europe/London",
            recurrence: "Daily",
            cronExpression: "0 10 * * *",
            recurrenceEndAt: "2026-02-02T03:04:05Z"));

        json.GetProperty("status").GetString().Should().Be("Pending");
        json.GetProperty("recurrence").GetString().Should().Be("Daily");
        json.GetRawText().ToLowerInvariant().Should().NotContain("phone");
        json.GetRawText().ToLowerInvariant().Should().NotContain("email");

        _ = _client.Received(1).CreateBroadcastAsync(
            Arg.Is<CreateBroadcastRequest>(r =>
                r.StorefrontId.Value == StorefrontId &&
                r.TargetType == "telegram" &&
                r.TargetConfig == /*lang=json,strict*/ """{"channel":"news"}""" &&
                r.Text == "Maintenance at 10:00" &&
                r.ParseMode == "HTML" &&
                r.ScheduledAt.ToDateTimeOffset() == DateTimeOffset.Parse("2026-01-02T03:04:05Z", CultureInfo.InvariantCulture) &&
                r.TimeZoneId == "Europe/London" &&
                r.Recurrence == RecurrenceType.Daily &&
                r.CronExpression == "0 10 * * *" &&
                r.RecurrenceEndAt.ToDateTimeOffset() == DateTimeOffset.Parse("2026-02-02T03:04:05Z", CultureInfo.InvariantCulture)),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBroadcast_InvalidScheduledAt_ReturnsErrorAsync()
    {
        var result = await CreateBroadcastTool.ExecuteAsync(
            _auth,
            _client,
            storefrontId: StorefrontId,
            targetType: "telegram",
            targetConfig: "{}",
            text: "hello",
            scheduledAt: "not-a-date");

        Parse(result).GetProperty("error").GetString().Should().Contain("scheduledAt must be ISO 8601");
    }

    [Fact]
    public async Task CancelBroadcast_ReadOnly_ThrowsUnauthorizedAsync()
    {
        var act = () => CancelBroadcastTool.ExecuteAsync(
            ApiKeyContextMocks.ReadOnly(), _client, broadcastId: BroadcastId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static BroadcastModel Model(
        BroadcastStatus status = BroadcastStatus.Pending,
        RecurrenceType recurrence = RecurrenceType.None)
    {
        return new BroadcastModel
        {
            Id = new GuidValue(BroadcastId),
            StorefrontId = new GuidValue(StorefrontId),
            TargetType = "telegram",
            TargetConfig = /*lang=json,strict*/ """{"channel":"news"}""",
            Text = "Maintenance at 10:00",
            ParseMode = "HTML",
            ScheduledAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-01-02T03:04:05Z", CultureInfo.InvariantCulture)),
            TimeZoneId = "Europe/London",
            Status = status,
            ExecutedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-01-02T03:05:05Z", CultureInfo.InvariantCulture)),
            ErrorMessage = "failed once",
            ExternalMessageId = "tg-1",
            Recurrence = recurrence,
            CronExpression = "0 10 * * *",
            RecurrenceEndAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-02-02T03:04:05Z", CultureInfo.InvariantCulture)),
            ParentBroadcastId = new GuidValue(Guid.NewGuid().ToString()),
            RetryCount = 1,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-01-01T03:04:05Z", CultureInfo.InvariantCulture)),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                DateTimeOffset.Parse("2026-01-01T03:05:05Z", CultureInfo.InvariantCulture)),
        };
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
