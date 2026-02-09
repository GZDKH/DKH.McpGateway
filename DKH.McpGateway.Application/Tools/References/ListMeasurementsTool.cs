using DKH.ReferenceService.Contracts.Api.MeasurementQuery.V1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing measurement units (weights and dimensions).
/// </summary>
[McpServerToolType]
public static class ListMeasurementsTool
{
    [McpServerTool(Name = "list_measurements"), Description("List measurement units: weights and dimensions with their conversion ratios.")]
    public static async Task<string> ExecuteAsync(
        MeasurementQueryService.MeasurementQueryServiceClient client,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var weightsTask = client.GetWeightsAsync(
            new GetWeightsRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        var dimensionsTask = client.GetDimensionsAsync(
            new GetDimensionsRequest { LanguageCode = languageCode },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(weightsTask, dimensionsTask);

        var result = new
        {
            weights = weightsTask.Result.Weights.Select(static w => new
            {
                code = w.Code,
                name = w.Name,
                ratioToPrimary = w.RatioToPrimary,
            }),
            dimensions = dimensionsTask.Result.Dimensions.Select(static d => new
            {
                code = d.Code,
                name = d.Name,
                ratioToPrimary = d.RatioToPrimary,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
