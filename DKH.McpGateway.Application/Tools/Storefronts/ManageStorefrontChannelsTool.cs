using DKH.StorefrontService.Contracts.V1;
using DKH.StorefrontService.Contracts.V1.Models;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ManageStorefrontChannelsTool
{
    [McpServerTool(Name = "manage_storefront_channels"), Description(
        "Manage storefront sales channels (Telegram, WeChat, VK, Web, Mobile). " +
        "Actions: 'list' to view channels, 'add' to create, 'update' to modify, 'remove' to delete.")]
    public static async Task<string> ExecuteAsync(
        StorefrontCrudService.StorefrontCrudServiceClient crudClient,
        StorefrontChannelService.StorefrontChannelServiceClient channelClient,
        [Description("Storefront code (e.g. 'my-store')")] string storefrontCode,
        [Description("Action: list, add, update, or remove")] string action,
        [Description("Channel ID (for update/remove, returned from list)")] string? channelId = null,
        [Description("Channel type: Telegram, WeChat, VK, Web, Mobile (for add)")] string? channelType = null,
        [Description("External ID (e.g. Telegram bot username, for add)")] string? externalId = null,
        [Description("Display name (for add/update)")] string? displayName = null,
        [Description("Is active (for update)")] bool? isActive = null,
        [Description("Purpose: Sales, Support, or Notifications (for add/update)")] string? purpose = null,
        CancellationToken cancellationToken = default)
    {
        var storefront = await crudClient.GetByCodeAsync(
            new GetStorefrontByCodeRequest { Code = storefrontCode },
            cancellationToken: cancellationToken);

        if (storefront.Storefront is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Storefront '{storefrontCode}' not found" },
                McpJsonDefaults.Options);
        }

        var storefrontId = storefront.Storefront.Id;

        if (string.Equals(action, "list", StringComparison.OrdinalIgnoreCase))
        {
            var response = await channelClient.GetChannelsAsync(
                new GetChannelsRequest { StorefrontId = storefrontId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                channels = response.Channels.Select(c => new
                {
                    channelId = c.Id,
                    type = c.ChannelType.ToString(),
                    externalId = c.ExternalId,
                    displayName = c.DisplayName,
                    isDefault = c.IsDefault,
                    isActive = c.IsActive,
                    purpose = c.Purpose.ToString(),
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "add", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(channelType) || string.IsNullOrEmpty(externalId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "channelType and externalId are required for add" },
                    McpJsonDefaults.Options);
            }

            var request = new AddChannelRequest
            {
                StorefrontId = storefrontId,
                ChannelType = ParseChannelType(channelType),
                ExternalId = externalId,
                Purpose = ParseChannelPurpose(purpose),
            };

            if (displayName is not null)
            {
                request.DisplayName = displayName;
            }

            var response = await channelClient.AddChannelAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "added",
                channel = new { channelId = response.Channel.Id, type = response.Channel.ChannelType.ToString() },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(channelId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "channelId is required for update" },
                    McpJsonDefaults.Options);
            }

            var request = new UpdateChannelRequest
            {
                StorefrontId = storefrontId,
                ChannelId = channelId,
                IsActive = isActive ?? true,
                Purpose = ParseChannelPurpose(purpose),
            };

            if (displayName is not null)
            {
                request.DisplayName = displayName;
            }

            var response = await channelClient.UpdateChannelAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                channel = new { channelId = response.Channel.Id, isActive = response.Channel.IsActive },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "remove", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(channelId))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "channelId is required for remove" },
                    McpJsonDefaults.Options);
            }

            var response = await channelClient.RemoveChannelAsync(
                new RemoveChannelRequest { StorefrontId = storefrontId, ChannelId = channelId },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Success,
                action = "removed",
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: list, add, update, or remove" },
            McpJsonDefaults.Options);
    }

    private static ChannelType ParseChannelType(string? type) => type?.ToUpperInvariant() switch
    {
        "TELEGRAM" => ChannelType.Telegram,
        "WECHAT" => ChannelType.Wechat,
        "VK" => ChannelType.Vk,
        "WEB" => ChannelType.Web,
        "MOBILE" => ChannelType.Mobile,
        _ => ChannelType.Unspecified,
    };

    private static ChannelPurpose ParseChannelPurpose(string? purpose) => purpose?.ToUpperInvariant() switch
    {
        "SALES" => ChannelPurpose.Sales,
        "SUPPORT" => ChannelPurpose.Support,
        "NOTIFICATIONS" => ChannelPurpose.Notifications,
        _ => ChannelPurpose.Sales,
    };
}
