using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class RegisterCarrierCapabilityTool
{
    [McpServerTool(Name = "register_carrier_capability"), Description(
        "Register a new carrier capability record. " +
        "Returns the created record. " +
        "Returns AlreadyExists when a record for the carrier already exists.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CarrierCapabilityService.CarrierCapabilityServiceClient client,
        [Description("Carrier code")] string carrier,
        [Description("Display name")] string displayName,
        [Description("Max parcel weight value (decimal-as-string)")] string maxParcelWeightValue,
        [Description("Max parcel weight unit (Kilogram, Gram, Pound)")] string maxParcelWeightUnit,
        [Description("Max longest side in cm (decimal-as-string)")] string maxLongestSideCm,
        [Description("Supported service levels as comma-separated (e.g. Standard,Express)")] string? serviceLevels = null,
        [Description("Supported zone codes as comma-separated")] string? supportedZones = null,
        [Description("Restriction tags as comma-separated")] string? restrictions = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(carrier))
        {
            return McpProtoHelper.FormatError("carrier is required");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return McpProtoHelper.FormatError("displayName is required");
        }

        var request = new RegisterCarrierCapabilityRequest
        {
            Carrier = carrier,
            DisplayName = displayName,
            MaxParcelWeight = new WeightModel
            {
                Value = maxParcelWeightValue,
                Unit = LogisticsEnumHelper.ParseWeightUnit(maxParcelWeightUnit),
            },
            MaxLongestSideCm = maxLongestSideCm,
        };

        if (!string.IsNullOrWhiteSpace(serviceLevels))
        {
            foreach (var level in serviceLevels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.ServiceLevels.Add(LogisticsEnumHelper.ParseServiceLevel(level));
            }
        }

        if (!string.IsNullOrWhiteSpace(supportedZones))
        {
            foreach (var zone in supportedZones.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.SupportedZones.Add(zone);
            }
        }

        if (!string.IsNullOrWhiteSpace(restrictions))
        {
            foreach (var tag in restrictions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                request.Restrictions.Add(tag);
            }
        }

        var capability = await client.RegisterCarrierCapabilityAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetCarrierCapabilityTool.MapCarrierCapability(capability), McpJsonDefaults.Options);
    }
}
