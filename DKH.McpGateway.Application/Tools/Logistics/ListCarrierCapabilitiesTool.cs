using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class ListCarrierCapabilitiesTool
{
    [McpServerTool(Name = "list_carrier_capabilities"), Description(
        "List carrier capability records with pagination. " +
        "Returns carrier codes, display names, service levels, supported zones and weight limits.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Page number (default 1)")] int page = 1,
        [Description("Page size (default 20)")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListCarrierCapabilitiesAsync(
            new ListCarrierCapabilitiesRequest { Page = page, PageSize = pageSize },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(GetCarrierCapabilityTool.MapCarrierCapability),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}
