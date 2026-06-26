using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class DeleteRateCardTool
{
    [McpServerTool(Name = "delete_rate_card"), Description(
        "Delete a rate card by ID. " +
        "Returns NotFound when the ID does not exist.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        RateCardService.RateCardServiceClient client,
        [Description("Rate card ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        await client.DeleteRateCardAsync(
            new DeleteRateCardRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new { deleted = true, id }, McpJsonDefaults.Options);
    }
}
