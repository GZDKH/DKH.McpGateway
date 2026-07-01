using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class ListServiceTemplatesTool
{
    [McpServerTool(Name = "list_service_templates"), Description("List engagement service templates.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        string? serviceType = null,
        bool publishedOnly = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var request = new ListServiceTemplatesRequest
        {
            ServiceType = EngagementToolInput.ParseOptionalEnum<ServiceType>(serviceType, out var error),
            PublishedOnly = publishedOnly,
            Page = Math.Max(page, 1),
            PageSize = Math.Clamp(pageSize, 1, 100),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListServiceTemplatesAsync(request, cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapPage(response.Items, response.TotalCount, response.Page, response.PageSize, EngagementMapper.MapTemplate));
    }
}
