using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ListStorefrontsTool
{
    [McpServerTool(Name = "list_storefronts"), Description("List all storefronts with their status, code, and creation date.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 50)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var scope = StorefrontWorkspaceScope.Resolve(apiKeyContext, httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var response = await scope.GetAllAsync(client, page, pageSize, cancellationToken);

        var result = new
        {
            totalCount = response.Pagination.TotalCount,
            page = response.Pagination.CurrentPage,
            pageSize = response.Pagination.PageSize,
            storefronts = response.Storefronts.Select(static s => new
            {
                id = s.Id,
                code = s.Code,
                name = s.Name,
                status = s.Status.ToString(),
                createdAt = s.CreatedAt?.ToDateTimeOffset().ToString("O"),
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
