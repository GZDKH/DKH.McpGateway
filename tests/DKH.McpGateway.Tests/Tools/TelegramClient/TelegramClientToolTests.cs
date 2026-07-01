using DKH.McpGateway.Application.Tools.Common;
using DKH.McpGateway.Application.Tools.Payment;
using DKH.McpGateway.Application.Tools.TelegramClient;
using DKH.Platform.Grpc.Common.Types;
using DKH.TelegramClientService.Contracts.TelegramClient.Models.Session.V1;

namespace DKH.McpGateway.Tests.Tools.TelegramClient;

public sealed class TelegramClientToolTests
{
    [Fact]
    public void TelegramClientToolSurface_DefinesEverySpecTool()
    {
        string[] expected =
        [
            "get_messages",
            "search_messages",
            "get_media",
            "download_media",
            "export_messages",
            "add_monitored_chat",
            "remove_monitored_chat",
            "pause_monitored_chat",
            "resume_monitored_chat",
            "list_monitored_chats",
            "get_monitoring_status",
            "trigger_backfill",
            "get_telegram_session",
            "list_telegram_sessions",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.TelegramClient")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().Contain(expected);
    }

    [Fact]
    public void TelegramClientToolSurface_DoesNotExposeCredentialOrAuthTools()
    {
        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.TelegramClient")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToArray();

        actual.Should().NotContain(name => name.Contains("auth", StringComparison.OrdinalIgnoreCase));
        actual.Should().NotContain(name => name.Contains("password", StringComparison.OrdinalIgnoreCase));
        actual.Should().NotContain(name => name.Contains("disconnect", StringComparison.OrdinalIgnoreCase));
        actual.Should().NotContain(name => name.Contains("reconnect", StringComparison.OrdinalIgnoreCase));
        actual.Should().NotContain(name => name.Contains("create_session", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapSession_OmitsPhoneNumber()
    {
        var model = new SessionModel
        {
            SessionId = new GuidValue("session-1"),
            PhoneNumber = "+15551234567",
            DisplayName = "Archive Session",
            Username = "public_handle",
            Status = "connected",
        };

        var json = JsonSerializer.Serialize(TelegramClientMapper.MapSession(model), McpJsonDefaults.Options);

        json.Should().Contain("session-1");
        json.Should().Contain("Archive Session");
        json.Should().Contain("public_handle");
        json.Should().NotContain("+15551234567");
        json.Should().NotContain("phoneNumber");
    }
}
