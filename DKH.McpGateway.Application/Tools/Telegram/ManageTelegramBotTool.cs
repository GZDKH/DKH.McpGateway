using DKH.TelegramBotService.Contracts.TelegramBot.Api.BotCrud.v1;

namespace DKH.McpGateway.Application.Tools.Telegram;

[McpServerToolType]
public static class ManageTelegramBotTool
{
    [McpServerTool(Name = "manage_telegram_bot"), Description(
        "Manage Telegram bots linked to storefronts. " +
        "Actions: 'create' to register a new bot, 'list' to view bots for a storefront, " +
        "'deactivate' to disable a bot, 'get' to get bot by storefront, 'stats' to view bot statistics.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        BotsCrudService.BotsCrudServiceClient client,
        [Description("Action: create, list, deactivate, get, stats, or ping")] string action,
        [Description("Storefront ID (required for create/list/get)")] string? storefrontId = null,
        [Description("Bot API token from Telegram (required for create)")] string? token = null,
        [Description("Mini App URL (for create)")] string? miniAppUrl = null,
        [Description("Bot ID (for deactivate/stats)")] string? botId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "ping", StringComparison.OrdinalIgnoreCase))
        {
            var response = await client.PingAsync(
                new PingRequest(),
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                message = response.Message,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(storefrontId) || string.IsNullOrEmpty(token))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "storefrontId and token are required for create" },
                    McpJsonDefaults.Options);
            }

            var request = new CreateBotRequest
            {
                StorefrontId = new GuidValue(storefrontId),
                Token = token,
                MiniAppUrl = miniAppUrl ?? "",
            };

            var response = await client.CreateBotAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                bot = new
                {
                    response.BotId,
                    response.Username,
                    response.WebhookUrl,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(storefrontId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "storefrontId is required for list" },
                    McpJsonDefaults.Options);
            }

            var response = await client.ListBotsAsync(
                new ListBotsRequest { StorefrontId = new GuidValue(storefrontId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                bots = response.Bots.Select(b => new
                {
                    b.BotId,
                    b.StorefrontId,
                    b.Username,
                    b.WebhookUrl,
                    b.MiniAppUrl,
                    b.IsActive,
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "get", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(storefrontId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "storefrontId is required for get" },
                    McpJsonDefaults.Options);
            }

            var response = await client.GetBotByStorefrontIdAsync(
                new GetBotByStorefrontIdRequest { StorefrontId = new GuidValue(storefrontId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                bot = new
                {
                    response.Bot.BotId,
                    response.Bot.StorefrontId,
                    response.Bot.Username,
                    response.Bot.WebhookUrl,
                    response.Bot.MiniAppUrl,
                    response.Bot.IsActive,
                },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "deactivate", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(botId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "botId is required for deactivate" },
                    McpJsonDefaults.Options);
            }

            var response = await client.DeactivateBotAsync(
                new DeactivateBotRequest { BotId = new GuidValue(botId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "deactivated",
                botId,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "stats", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(botId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "botId is required for stats" },
                    McpJsonDefaults.Options);
            }

            var response = await client.GetBotStatsAsync(
                new GetBotStatsRequest { BotId = new GuidValue(botId) },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                stats = new
                {
                    response.BotId,
                    response.TotalChannels,
                    response.TotalSubscribers,
                    response.TotalManagerGroups,
                    response.TotalUpdatesProcessed,
                },
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, list, deactivate, get, stats, or ping" },
            McpJsonDefaults.Options);
    }
}
