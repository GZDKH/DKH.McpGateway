using DKH.SubscriptionService.Contracts.Subscription.Api.SubscriptionCrud.v1;
using DKH.SubscriptionService.Contracts.Subscription.Models.v1;

namespace DKH.McpGateway.Application.Tools.Subscription;

[McpServerToolType]
public static class ListUserSubscriptionsTool
{
    [McpServerTool(Name = "list_user_subscriptions"), Description(
        "List user subscriptions with optional filtering by userId, statuses " +
        "(comma-separated: Active, Expired, Cancelled, ...), and plan code. Supports pagination.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        SubscriptionCrudService.SubscriptionCrudServiceClient client,
        [Description("User ID (GUID, optional)")] string? userId = null,
        [Description("Comma-separated subscription statuses to filter by (optional)")] string? statuses = null,
        [Description("Plan code filter (optional)")] string? planCode = null,
        [Description("Page number (default 1)")] int? page = null,
        [Description("Page size (default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListUserSubscriptionsRequest
        {
            Pagination = new PaginationRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 20,
            },
        };

        if (!string.IsNullOrWhiteSpace(userId))
        {
            request.UserId = new GuidValue(userId);
        }

        // plan_code is google.protobuf.StringValue → C# nullable string.
        if (!string.IsNullOrWhiteSpace(planCode))
        {
            request.PlanCode = planCode;
        }

        if (!string.IsNullOrWhiteSpace(statuses))
        {
            foreach (var raw in statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<SubscriptionStatus>(raw, ignoreCase: true, out var status))
                {
                    request.Statuses.Add(status);
                }
            }
        }

        var response = await client.ListUserSubscriptionsAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetUserSubscriptionTool.MapSubscription),
            totalCount = response.Metadata?.TotalCount ?? 0,
            page = response.Metadata?.CurrentPage ?? 1,
            pageSize = response.Metadata?.PageSize ?? (pageSize ?? 20),
        }, McpJsonDefaults.Options);
    }
}
