using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class RemoveCarrierZoneMappingTool
{
    [McpServerTool(Name = "remove_carrier_zone_mapping"), Description(
        "Remove a country-to-zone mapping from a carrier capability record. No-op if the mapping is absent. " +
        "Returns the updated record. " +
        "Returns NotFound on bad ID, InvalidArgument on missing countryCode.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Carrier capability record ID")] string id,
        [Description("ISO 3166-alpha-2 country code to remove (e.g. US)")] string countryCode,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return McpProtoHelper.FormatError("countryCode is required");
        }

        var capability = await client.RemoveCarrierZoneMappingAsync(
            new RemoveCarrierZoneMappingRequest { Id = id, CountryCode = countryCode },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCarrierCapabilityTool.MapCarrierCapability(capability), McpJsonDefaults.Options);
    }
}
