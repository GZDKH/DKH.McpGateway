using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class ActivateRateCardTool
{
    [McpServerTool(Name = "activate_rate_card"), Description(
        "Activate a rate card, making it eligible for rate calculation. " +
        "Returns the updated rate card. " +
        "Returns NotFound on bad ID, FailedPrecondition when the card is empty, already expired, or archived.")]
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

        var card = await client.ActivateRateCardAsync(
            new ActivateRateCardRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetRateCardTool.MapRateCard(card), McpJsonDefaults.Options);
    }
}
