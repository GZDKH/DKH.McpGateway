using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class ExpireRateCardTool
{
    [McpServerTool(Name = "expire_rate_card"), Description(
        "Mark a rate card as expired, removing it from rate calculation. " +
        "Returns the updated rate card. " +
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

        var card = await client.ExpireRateCardAsync(
            new ExpireRateCardRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetRateCardTool.MapRateCard(card), McpJsonDefaults.Options);
    }
}
