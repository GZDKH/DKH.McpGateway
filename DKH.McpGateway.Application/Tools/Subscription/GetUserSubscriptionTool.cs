using DKH.SubscriptionService.Contracts.Subscription.Api.SubscriptionCrud.v1;
using DKH.SubscriptionService.Contracts.Subscription.Models.v1;

namespace DKH.McpGateway.Application.Tools.Subscription;

[McpServerToolType]
public static class GetUserSubscriptionTool
{
    [McpServerTool(Name = "get_user_subscription"), Description(
        "Get a user's current subscription by user ID. " +
        "Returns plan code, status, and lifecycle timestamps (started, expires, cancelled).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SubscriptionCrudService.SubscriptionCrudServiceClient client,
        [Description("User ID (GUID)")] string userId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return McpProtoHelper.FormatError("userId is required");
        }

        var subscription = await client.GetUserSubscriptionAsync(
            new GetUserSubscriptionRequest { UserId = new GuidValue(userId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapSubscription(subscription), McpJsonDefaults.Options);
    }

    // cancellation_reason is google.protobuf.StringValue → C# nullable string (no .Value).
    internal static object MapSubscription(UserSubscriptionModel s) => new
    {
        id = s.Id?.Value,
        userId = s.UserId?.Value,
        planCode = s.PlanCode,
        status = s.Status.ToString(),
        startedAt = s.StartedAt?.ToDateTimeOffset().ToString("O"),
        expiresAt = s.ExpiresAt?.ToDateTimeOffset().ToString("O"),
        cancelledAt = s.CancelledAt?.ToDateTimeOffset().ToString("O"),
        cancellationReason = s.CancellationReason,
        createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = s.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
