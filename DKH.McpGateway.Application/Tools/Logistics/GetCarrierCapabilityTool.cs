using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class GetCarrierCapabilityTool
{
    [McpServerTool(Name = "get_carrier_capability"), Description(
        "Get a carrier capability record by ID or carrier code (ID wins if both set, at least one required). " +
        "Returns service levels, supported zones, max parcel weight, restrictions and zone-by-country mapping. " +
        "Returns NotFound when no matching record exists.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Carrier capability record ID (preferred)")] string? id = null,
        [Description("Carrier code (used if ID not provided)")] string? carrier = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(carrier))
        {
            return McpProtoHelper.FormatError("id or carrier is required");
        }

        var capability = await client.GetCarrierCapabilityAsync(
            new GetCarrierCapabilityRequest
            {
                Id = id ?? string.Empty,
                Carrier = carrier ?? string.Empty,
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapCarrierCapability(capability), McpJsonDefaults.Options);
    }

    internal static object MapCarrierCapability(CarrierCapabilityModel m) => new
    {
        id = m.Id,
        carrier = m.Carrier,
        displayName = m.DisplayName,
        serviceLevels = m.ServiceLevels.Select(s => s.ToString()).ToArray(),
        supportedZones = m.SupportedZones.ToArray(),
        restrictions = m.Restrictions.ToArray(),
        maxParcelWeight = m.MaxParcelWeight is null ? null : (object)new
        {
            value = m.MaxParcelWeight.Value,
            unit = m.MaxParcelWeight.Unit.ToString(),
        },
        maxLongestSideCm = m.MaxLongestSideCm,
        zoneByCountry = m.ZoneByCountry,
    };
}
