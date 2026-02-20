using DKH.McpGateway.Application.Tools.Telegram;
using DKH.Platform.Grpc.Common.Types;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotCrud.v1;

namespace DKH.McpGateway.Tests.Tools.Telegram;

public class ManageTelegramBotToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly BotsCrudService.BotsCrudServiceClient _client =
        Substitute.For<BotsCrudService.BotsCrudServiceClient>();

    [Fact]
    public async Task Ping_HappyPath_ReturnsSuccessAsync()
    {
        _client.PingAsync(
                Arg.Any<PingRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PingReply { Message = "pong" }));

        var result = await ManageTelegramBotTool.ExecuteAsync(_auth, _client, "ping");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("message").GetString().Should().Be("pong");
    }

    [Fact]
    public async Task Create_HappyPath_ReturnsBotInfoAsync()
    {
        var storefrontId = Guid.NewGuid().ToString();

        _client.CreateBotAsync(
                Arg.Any<CreateBotRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CreateBotReply
            {
                BotId = new GuidValue(Guid.NewGuid().ToString()),
                Username = "test_bot",
                WebhookUrl = "https://example.com/webhook",
            }));

        var result = await ManageTelegramBotTool.ExecuteAsync(
            _auth, _client, "create", storefrontId: storefrontId, token: "123:ABC");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("created");
    }

    [Fact]
    public async Task Create_MissingStorefrontId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(
            _auth, _client, "create", token: "123:ABC");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontId and token are required");
    }

    [Fact]
    public async Task Create_MissingToken_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(
            _auth, _client, "create", storefrontId: Guid.NewGuid().ToString());

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontId and token are required");
    }

    [Fact]
    public async Task List_MissingStorefrontId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(_auth, _client, "list");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontId is required");
    }

    [Fact]
    public async Task Get_MissingStorefrontId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(_auth, _client, "get");

        Parse(result).GetProperty("error").GetString().Should().Contain("storefrontId is required");
    }

    [Fact]
    public async Task Deactivate_MissingBotId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(_auth, _client, "deactivate");

        Parse(result).GetProperty("error").GetString().Should().Contain("botId is required");
    }

    [Fact]
    public async Task Stats_MissingBotId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(_auth, _client, "stats");

        Parse(result).GetProperty("error").GetString().Should().Contain("botId is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageTelegramBotTool.ExecuteAsync(_auth, _client, "unknown");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageTelegramBotTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, "ping");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
