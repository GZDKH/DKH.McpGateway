using DKH.ReferenceService.Contracts.Reference.Api.DimensionManagement.v1;
using DKH.ReferenceService.Contracts.Reference.Api.WeightManagement.v1;

namespace DKH.McpGateway.Application.Tools.References;

/// <summary>
/// MCP tool for listing measurement units (weights and dimensions).
/// </summary>
[McpServerToolType]
public static class ListMeasurementsTool
{
    [McpServerTool(Name = "list_measurements"), Description("List measurement units: weights and dimensions with their conversion ratios.")]
    public static async Task<string> ExecuteAsync(
        WeightManagementService.WeightManagementServiceClient weightClient,
        DimensionManagementService.DimensionManagementServiceClient dimensionClient,
        [Description("Language code for translations (e.g. 'ru-RU', 'en-US')")] string languageCode = "ru-RU",
        CancellationToken cancellationToken = default)
    {
        var weightsTask = weightClient.ListAsync(
            new ListWeightsRequest { Language = languageCode, PageSize = 1000 },
            cancellationToken: cancellationToken).ResponseAsync;

        var dimensionsTask = dimensionClient.ListAsync(
            new ListDimensionsRequest { Language = languageCode, PageSize = 1000 },
            cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(weightsTask, dimensionsTask);

        var result = new
        {
            weights = weightsTask.Result.Items.Select(static w => new
            {
                code = w.Code,
                name = w.Translations.FirstOrDefault()?.Name ?? string.Empty,
                ratioToPrimary = w.RatioToPrimary,
            }),
            dimensions = dimensionsTask.Result.Items.Select(static d => new
            {
                code = d.Code,
                name = d.Translations.FirstOrDefault()?.Name ?? string.Empty,
                ratioToPrimary = d.RatioToPrimary,
            }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
