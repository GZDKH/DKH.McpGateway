using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class SetCarrierZoneMappingTool
{
    [McpServerTool(Name = "set_carrier_zone_mapping"), Description(
        "Add or update a country-to-zone mapping on a carrier capability record. " +
        "The zone must be present in the record's supportedZones list. " +
        "Returns the updated record. " +
        "Returns NotFound on bad ID, InvalidArgument when the zone is not in supportedZones.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Carrier capability record ID")] string id,
        [Description("ISO 3166-alpha-2 country code (e.g. US)")] string countryCode,
        [Description("Zone code (must exist in supportedZones)")] string zone,
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

        if (string.IsNullOrWhiteSpace(zone))
        {
            return McpProtoHelper.FormatError("zone is required");
        }

        var capability = await client.SetCarrierZoneMappingAsync(
            new SetCarrierZoneMappingRequest { Id = id, CountryCode = countryCode, Zone = zone },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCarrierCapabilityTool.MapCarrierCapability(capability), McpJsonDefaults.Options);
    }
}
