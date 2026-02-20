using DKH.McpGateway.Application.Tools.Telegram;
using DKH.Platform.Grpc.Common.Types;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BroadcastManagement.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Telegram;

public class ManageTelegramSchedulingToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly BroadcastManagementService.BroadcastManagementServiceClient _client =
        Substitute.For<BroadcastManagementService.BroadcastManagementServiceClient>();

    private static readonly string BotId = Guid.NewGuid().ToString();

    [Fact]
    public async Task Create_HappyPath_ReturnsCreatedBroadcastAsync()
    {
        var broadcastId = Guid.NewGuid().ToString();
        var scheduledAt = DateTimeOffset.UtcNow.AddHours(1);

        _client.CreateScheduledBroadcastAsync(
                Arg.Any<CreateScheduledBroadcastRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CreateScheduledBroadcastReply
            {
                Id = new GuidValue(broadcastId),
                BotId = new GuidValue(BotId),
                ChatId = "@test_channel",
                ScheduledAt = Timestamp.FromDateTimeOffset(scheduledAt),
                TimeZoneId = "UTC",
            }));

        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "create",
            chatId: "@test_channel", text: "Hello!", scheduledAt: scheduledAt.ToString("O"));

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("created");
    }

    [Fact]
    public async Task Create_MissingRequiredFields_ReturnsErrorAsync()
    {
        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "create", text: "Hello!");

        Parse(result).GetProperty("error").GetString().Should().Contain("chatId, text, and scheduledAt are required");
    }

    [Fact]
    public async Task Create_InvalidScheduledAtFormat_ReturnsErrorAsync()
    {
        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "create",
            chatId: "@channel", text: "Hello!", scheduledAt: "not-a-date");

        Parse(result).GetProperty("error").GetString().Should().Contain("Invalid scheduledAt format");
    }

    [Fact]
    public async Task List_HappyPath_ReturnsItemsAsync()
    {
        var broadcastId = Guid.NewGuid().ToString();
        var scheduledAt = DateTimeOffset.UtcNow.AddHours(1);

        _client.ListScheduledBroadcastsAsync(
                Arg.Any<ListScheduledBroadcastsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListScheduledBroadcastsReply
            {
                Items =
                {
                    new ScheduledBroadcastItem
                    {
                        Id = new GuidValue(broadcastId),
                        BotId = new GuidValue(BotId),
                        ChatId = "@test_channel",
                        Text = "Hello!",
                        ScheduledAt = Timestamp.FromDateTimeOffset(scheduledAt),
                        TimeZoneId = "UTC",
                        Status = ScheduledBroadcastStatus.Pending,
                    },
                },
                Pagination = new PaginationMetadata { TotalCount = 1, CurrentPage = 1, PageSize = 20 },
            }));

        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "list");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Update_HappyPath_ReturnsUpdatedBroadcastAsync()
    {
        var broadcastId = Guid.NewGuid().ToString();
        var newTime = DateTimeOffset.UtcNow.AddHours(2);

        _client.UpdateScheduledBroadcastAsync(
                Arg.Any<UpdateScheduledBroadcastRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new UpdateScheduledBroadcastReply
            {
                Id = new GuidValue(broadcastId),
                Text = "Updated text",
                ScheduledAt = Timestamp.FromDateTimeOffset(newTime),
                TimeZoneId = "Europe/Moscow",
            }));

        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "update",
            broadcastId: broadcastId, text: "Updated text",
            scheduledAt: newTime.ToString("O"), timeZoneId: "Europe/Moscow");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("updated");
    }

    [Fact]
    public async Task Update_MissingBroadcastId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "update", text: "Updated");

        Parse(result).GetProperty("error").GetString().Should().Contain("broadcastId is required");
    }

    [Fact]
    public async Task Cancel_HappyPath_ReturnsSuccessAsync()
    {
        var broadcastId = Guid.NewGuid().ToString();

        _client.CancelScheduledBroadcastAsync(
                Arg.Any<CancelScheduledBroadcastRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CancelScheduledBroadcastReply
            {
                Id = new GuidValue(broadcastId),
                Success = true,
            }));

        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "cancel", broadcastId: broadcastId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("cancelled");
    }

    [Fact]
    public async Task Cancel_MissingBroadcastId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "cancel");

        Parse(result).GetProperty("error").GetString().Should().Contain("broadcastId is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageTelegramSchedulingTool.ExecuteAsync(
            _auth, _client, BotId, "unknown");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageTelegramSchedulingTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, BotId, "create");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
