using DKH.McpGateway.Application.Tools.Telegram;
using DKH.Platform.Grpc.Common.Types;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotCrud.v1;
using DKH.TelegramBotService.Contracts.TelegramBot.Models.Channel.v1;

namespace DKH.McpGateway.Tests.Tools.Telegram;

public class ManageTelegramChannelsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly BotsCrudService.BotsCrudServiceClient _client =
        Substitute.For<BotsCrudService.BotsCrudServiceClient>();

    private static readonly string BotId = Guid.NewGuid().ToString();

    [Fact]
    public async Task List_HappyPath_ReturnsChannelsAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _client.GetChannelsAsync(
                Arg.Any<GetChannelsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetChannelsReply
            {
                Channels =
                {
                    new ChannelModel
                    {
                        ChannelId = new GuidValue(channelId),
                        BotId = new GuidValue(BotId),
                        TelegramChannelId = "@test_channel",
                        Name = "Test Channel",
                        SubscribersCount = 100,
                        IsVerified = true,
                    },
                },
            }));

        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "list");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("channels").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Add_HappyPath_ReturnsAddedChannelAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _client.AddChannelAsync(
                Arg.Any<AddChannelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new AddChannelReply
            {
                ChannelId = new GuidValue(channelId),
                TelegramChannelId = "@my_channel",
                Name = "My Channel",
            }));

        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "add", telegramChannelId: "@my_channel", channelName: "My Channel");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("added");
    }

    [Fact]
    public async Task Add_MissingTelegramChannelId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "add");

        Parse(result).GetProperty("error").GetString().Should().Contain("telegramChannelId is required");
    }

    [Fact]
    public async Task Remove_HappyPath_ReturnsSuccessAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _client.RemoveChannelAsync(
                Arg.Any<RemoveChannelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new RemoveChannelReply { Success = true }));

        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "remove", channelId: channelId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("removed");
    }

    [Fact]
    public async Task Remove_MissingChannelId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "remove");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelId is required");
    }

    [Fact]
    public async Task UpdateStats_HappyPath_ReturnsSubscribersCountAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _client.UpdateChannelStatsAsync(
                Arg.Any<UpdateChannelStatsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new UpdateChannelStatsReply
            {
                SubscribersCount = 250,
                Success = true,
            }));

        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "update_stats", channelId: channelId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("stats_updated");
        json.GetProperty("subscribersCount").GetInt32().Should().Be(250);
    }

    [Fact]
    public async Task UpdateStats_MissingChannelId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "update_stats");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelId is required");
    }

    [Fact]
    public async Task Broadcast_HappyPath_ReturnsSentInfoAsync()
    {
        var channelId = Guid.NewGuid().ToString();

        _client.BroadcastToChannelAsync(
                Arg.Any<BroadcastToChannelRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new BroadcastToChannelReply
            {
                MessageId = 42,
                Success = true,
                ErrorMessage = "",
            }));

        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "broadcast",
            channelId: channelId, message: "Hello!", parseMode: "HTML");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("broadcast_sent");
        json.GetProperty("messageId").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task Broadcast_MissingChannelIdOrMessage_ReturnsErrorAsync()
    {
        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "broadcast", message: "Hello!");

        Parse(result).GetProperty("error").GetString().Should().Contain("channelId and message are required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageTelegramChannelsTool.ExecuteAsync(
            _auth, _client, BotId, "unknown");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageTelegramChannelsTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, BotId, "list");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
