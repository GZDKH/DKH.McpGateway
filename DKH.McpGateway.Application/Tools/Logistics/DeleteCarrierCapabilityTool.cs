using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class DeleteCarrierCapabilityTool
{
    [McpServerTool(Name = "delete_carrier_capability"), Description(
        "Delete a carrier capability record by ID. " +
        "Returns NotFound when the ID does not exist.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Carrier capability record ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.DeleteCarrierCapabilityAsync(
            new DeleteCarrierCapabilityRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { deleted = true, id }, McpJsonDefaults.Options);
    }
}
