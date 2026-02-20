using DKH.McpGateway.Application.Tools.Telegram;
using DKH.Platform.Grpc.Common.Types;
using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotCrud.v1;
using DKH.TelegramBotService.Contracts.TelegramBot.Models.ManagerGroup.v1;

namespace DKH.McpGateway.Tests.Tools.Telegram;

public class ManageTelegramManagerGroupsToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly BotsCrudService.BotsCrudServiceClient _client =
        Substitute.For<BotsCrudService.BotsCrudServiceClient>();

    private static readonly string BotId = Guid.NewGuid().ToString();

    [Fact]
    public async Task List_HappyPath_ReturnsGroupsAsync()
    {
        var groupId = Guid.NewGuid().ToString();

        _client.GetManagerGroupsAsync(
                Arg.Any<GetManagerGroupsRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new GetManagerGroupsReply
            {
                ManagerGroups =
                {
                    new ManagerGroupModel
                    {
                        ManagerGroupId = new GuidValue(groupId),
                        BotId = new GuidValue(BotId),
                        TelegramGroupId = "-1001234567890",
                        Name = "Order Notifications",
                        NotificationTypes = { "order_created", "payment_received" },
                    },
                },
            }));

        var result = await ManageTelegramManagerGroupsTool.ExecuteAsync(
            _auth, _client, BotId, "list");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("managerGroups").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Add_HappyPath_ReturnsAddedGroupAsync()
    {
        var groupId = Guid.NewGuid().ToString();

        _client.AddManagerGroupAsync(
                Arg.Any<AddManagerGroupRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new AddManagerGroupReply
            {
                ManagerGroupId = new GuidValue(groupId),
                TelegramGroupId = "-1001234567890",
                Name = "My Group",
                NotificationTypes = { "order_created" },
            }));

        var result = await ManageTelegramManagerGroupsTool.ExecuteAsync(
            _auth, _client, BotId, "add",
            telegramGroupId: "-1001234567890", groupName: "My Group",
            notificationTypes: "order_created");

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("added");
    }

    [Fact]
    public async Task Add_MissingTelegramGroupId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramManagerGroupsTool.ExecuteAsync(
            _auth, _client, BotId, "add");

        Parse(result).GetProperty("error").GetString().Should().Contain("telegramGroupId is required");
    }

    [Fact]
    public async Task Remove_HappyPath_ReturnsSuccessAsync()
    {
        var groupId = Guid.NewGuid().ToString();

        _client.RemoveManagerGroupAsync(
                Arg.Any<RemoveManagerGroupRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new RemoveManagerGroupReply { Success = true }));

        var result = await ManageTelegramManagerGroupsTool.ExecuteAsync(
            _auth, _client, BotId, "remove", managerGroupId: groupId);

        var json = Parse(result);
        json.GetProperty("success").GetBoolean().Should().BeTrue();
        json.GetProperty("action").GetString().Should().Be("removed");
    }

    [Fact]
    public async Task Remove_MissingManagerGroupId_ReturnsErrorAsync()
    {
        var result = await ManageTelegramManagerGroupsTool.ExecuteAsync(
            _auth, _client, BotId, "remove");

        Parse(result).GetProperty("error").GetString().Should().Contain("managerGroupId is required");
    }

    [Fact]
    public async Task UnknownAction_ReturnsErrorAsync()
    {
        var result = await ManageTelegramManagerGroupsTool.ExecuteAsync(
            _auth, _client, BotId, "unknown");

        Parse(result).GetProperty("error").GetString().Should().Contain("Unknown action");
    }

    [Fact]
    public async Task Unauthenticated_ThrowsUnauthorizedAsync()
    {
        var act = () => ManageTelegramManagerGroupsTool.ExecuteAsync(
            ApiKeyContextMocks.Unauthenticated(), _client, BotId, "list");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
