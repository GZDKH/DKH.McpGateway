using DKH.SubscriptionService.Contracts.Subscription.Api.SubscriptionCrud.v1;
using DKH.SubscriptionService.Contracts.Subscription.Models.v1;

namespace DKH.McpGateway.Application.Tools.Subscription;

[McpServerToolType]
public static class ListPlansTool
{
    [McpServerTool(Name = "list_subscription_plans"), Description(
        "List all subscription plans (tiers). Returns each plan's code, localized display names, " +
        "price (in minor units), currency, and feature list.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SubscriptionCrudService.SubscriptionCrudServiceClient client,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListPlansAsync(new ListPlansRequest(), cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(MapPlan),
        }, McpJsonDefaults.Options);
    }

    internal static object MapPlan(SubscriptionPlanModel p) => new
    {
        id = p.Id?.Value,
        code = p.Code,
        displayName = p.DisplayName?.Values,
        priceMinor = p.PriceMinor,
        currency = p.Currency,
        features = p.Features,
        createdAt = p.CreatedAt?.ToDateTimeOffset().ToString("O"),
        updatedAt = p.UpdatedAt?.ToDateTimeOffset().ToString("O"),
    };
}
