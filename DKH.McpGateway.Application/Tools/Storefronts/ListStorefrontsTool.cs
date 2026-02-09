using DKH.StorefrontService.Contracts.V1;

namespace DKH.McpGateway.Application.Tools.Storefronts;

[McpServerToolType]
public static class ListStorefrontsTool
{
    [McpServerTool(Name = "list_storefronts"), Description("List all storefronts with their status, code, and creation date.")]
    public static async Task<string> ExecuteAsync(
        StorefrontCrudService.StorefrontCrudServiceClient client,
        [Description("Page number (1-based)")] int page = 1,
        [Description("Page size (max 50)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var response = await client.GetAllAsync(
            new GetAllStorefrontsRequest { Page = page, PageSize = pageSize },
            cancellationToken: cancellationToken);

        var result = new
        {
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
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
