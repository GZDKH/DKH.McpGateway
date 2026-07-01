using DKH.CustomsService.Contracts.Duty.Api.V2;

namespace DKH.McpGateway.Application.Tools.Customs;

[McpServerToolType]
public static class GetDutyRuleTool
{
    [McpServerTool(Name = "get_duty_rule"), Description("Get a single customs duty rule by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DutyService.DutyServiceClient client,
        [Description("Duty rule ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var rule = await client.GetDutyRuleAsync(new GetDutyRuleRequest { Id = id }, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapDutyRule(rule), McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class ListDutyRulesTool
{
    [McpServerTool(Name = "list_duty_rules"), Description(
        "List customs duty rules. Optional filters: destinationCountry, originCountry, systemCode, hsCodePrefix, activeOnly, asOfUtc.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DutyService.DutyServiceClient client,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        [Description("Destination country code filter")] string? destinationCountry = null,
        [Description("Origin country code filter")] string? originCountry = null,
        [Description("Nomenclature system code filter")] string? systemCode = null,
        [Description("HS code prefix filter")] string? hsCodePrefix = null,
        [Description("Only active rules")] bool activeOnly = true,
        [Description("Optional ISO-8601 timestamp filter")] string? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListDutyRulesRequest
        {
            Page = page,
            PageSize = pageSize,
            DestinationCountry = destinationCountry ?? string.Empty,
            OriginCountry = originCountry ?? string.Empty,
            SystemCode = systemCode ?? string.Empty,
            HsCodePrefix = hsCodePrefix ?? string.Empty,
            ActiveOnly = activeOnly,
        };

        var parsedAsOf = CustomsToolInput.ParseTimestamp(asOfUtc);
        if (parsedAsOf is not null)
        {
            request.AsOfUtc = parsedAsOf;
        }

        var response = await client.ListDutyRulesAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            items = response.Items.Select(CustomsMapper.MapDutyRule),
            totalCount = response.TotalCount,
            page = response.Page,
            pageSize = response.PageSize,
        }, McpJsonDefaults.Options);
    }
}

[McpServerToolType]
public static class CalculateCustomsDutiesTool
{
    [McpServerTool(Name = "calculate_customs_duties"), Description(
        "Calculate customs duties for HS lines. " +
        "linesJson: JSON array [{\"hsCode\":\"0902\",\"systemCode\":\"CN\",\"origin\":\"JP\",\"quantity\":\"1\",\"declaredValue\":\"10.00\",\"valueCurrency\":\"USD\"}].")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DutyService.DutyServiceClient client,
        [Description("Destination country code")] string destinationCountry,
        [Description("Duty input lines JSON array")] string linesJson,
        [Description("Optional ISO-8601 timestamp filter")] string? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(destinationCountry))
        {
            return McpProtoHelper.FormatError("destinationCountry is required");
        }

        var request = new CalculateDutiesRequest { DestinationCountry = destinationCountry };

        var parsedAsOf = CustomsToolInput.ParseTimestamp(asOfUtc);
        if (parsedAsOf is not null)
        {
            request.AsOfUtc = parsedAsOf;
        }

        var parseError = CustomsToolInput.PopulateDutyLines(request.Lines, linesJson);
        if (parseError is not null)
        {
            return McpProtoHelper.FormatError(parseError);
        }

        var response = await client.CalculateDutiesAsync(request, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(CustomsMapper.MapDutyCalculation(response), McpJsonDefaults.Options);
    }
}
