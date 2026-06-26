using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class GetRateCardTool
{
    [McpServerTool(Name = "get_rate_card"), Description(
        "Get a single rate card by its ID. " +
        "Returns carrier, service level, currency, validity window, tariff rows and status. " +
        "Returns NotFound when the ID does not exist.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        RateCardService.RateCardServiceClient client,
        [Description("Rate card ID")] string id,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var card = await client.GetRateCardAsync(
            new GetRateCardRequest { Id = id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapRateCard(card), McpJsonDefaults.Options);
    }

    internal static object MapRateCard(RateCardModel c) => new
    {
        id = c.Id,
        name = c.Name,
        carrier = c.Carrier,
        serviceLevel = c.ServiceLevel.ToString(),
        currency = c.Currency,
        status = c.Status.ToString(),
        validity = c.Validity is null ? null : (object)new
        {
            startUnixMs = c.Validity.StartUnixMs,
            endUnixMs = c.Validity.EndUnixMs,
        },
        tariffRows = c.TariffRows.Select(r => new
        {
            zone = r.Zone,
            minKgExclusive = r.MinKgExclusive,
            maxKgInclusive = r.MaxKgInclusive,
            mode = r.Mode.ToString(),
            price = r.Price is null ? null : (object)new
            {
                amount = r.Price.Amount,
                currency = r.Price.Currency,
            },
        }),
    };
}
