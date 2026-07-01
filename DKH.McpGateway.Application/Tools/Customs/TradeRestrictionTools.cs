using DKH.CustomsService.Contracts.Customs.Api.TradeRestrictions.v1;
using DKH.CustomsService.Contracts.Customs.Models.v1;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class CreateTradeRestrictionTool
{
    [McpServerTool(Name = "create_trade_restriction"), Description("Create a customs trade restriction for a destination/origin/HS prefix.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TradeRestrictionsService.TradeRestrictionsServiceClient client,
        [Description("Destination country code")] string destinationCountry,
        [Description("HS code prefix")] string hsCodePrefix,
        [Description("TradeRestrictionType enum name")] string type,
        [Description("Restriction reason")] string reason,
        [Description("Optional origin country code")] string? originCountry = null,
        [Description("Optional ISO-8601 valid-from timestamp")] string? validFrom = null,
        [Description("Optional ISO-8601 valid-until timestamp")] string? validUntil = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(destinationCountry))
        {
            return McpProtoHelper.FormatError("destinationCountry is required");
        }

        if (string.IsNullOrWhiteSpace(hsCodePrefix))
        {
            return McpProtoHelper.FormatError("hsCodePrefix is required");
        }

        if (!CustomsToolInput.TryParseEnum<TradeRestrictionType>(type, out var parsedType))
        {
            return McpProtoHelper.FormatError("type is invalid");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return McpProtoHelper.FormatError("reason is required");
        }

        var request = new CreateTradeRestrictionRequest
        {
            DestinationCountry = destinationCountry,
            OriginCountry = originCountry,
            HsCodePrefix = hsCodePrefix,
            Type = parsedType,
            Reason = reason,
            ValidFrom = CustomsToolInput.ParseTimestamp(validFrom),
            ValidUntil = CustomsToolInput.ParseTimestamp(validUntil),
        };

        var restriction = await client.CreateTradeRestrictionAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapTradeRestriction(restriction), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class CheckTradeRestrictionTool
{
    [McpServerTool(Name = "check_trade_restriction"), Description("Check whether a route/HS code is restricted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TradeRestrictionsService.TradeRestrictionsServiceClient client,
        [Description("Destination country code")] string destinationCountry,
        [Description("HS code")] string hsCode,
        [Description("Optional origin country code")] string? originCountry = null,
        [Description("Optional ISO-8601 timestamp filter")] string? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(destinationCountry))
        {
            return McpProtoHelper.FormatError("destinationCountry is required");
        }

        if (string.IsNullOrWhiteSpace(hsCode))
        {
            return McpProtoHelper.FormatError("hsCode is required");
        }

        var request = new CheckTradeRestrictionRequest
        {
            DestinationCountry = destinationCountry,
            OriginCountry = originCountry,
            HsCode = hsCode,
            AsOfUtc = CustomsToolInput.ParseTimestamp(asOfUtc),
        };

        var response = await client.CheckTradeRestrictionAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(new
        {
            match = response.Match is null ? null : CustomsMapper.MapTradeRestriction(response.Match),
        }, McpJsonDefaults.Options);
    }
}
