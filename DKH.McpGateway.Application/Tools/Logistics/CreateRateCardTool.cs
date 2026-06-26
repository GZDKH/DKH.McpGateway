using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class CreateRateCardTool
{
    [McpServerTool(Name = "create_rate_card"), Description(
        "Create a new rate card for a carrier and service level. " +
        "Returns the created rate card. " +
        "Returns InvalidArgument on missing required fields or overlapping tariff bands.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        RateCardService.RateCardServiceClient client,
        [Description("Rate card name")] string name,
        [Description("Carrier code")] string carrier,
        [Description("Service level (Economy, Standard, Express, SameDay)")] string serviceLevel,
        [Description("Currency code (e.g. USD)")] string currency,
        [Description("Validity start unix ms")] long validityStartUnixMs,
        [Description("Validity end unix ms (0 = open-ended)")] long validityEndUnixMs = 0,
        [Description("Tariff rows as JSON array: [{\"zone\":\"ZONE_A\",\"minKgExclusive\":\"0\",\"maxKgInclusive\":\"5\",\"mode\":\"Flat\",\"priceAmount\":\"9.99\",\"priceCurrency\":\"USD\"}]")] string? tariffRows = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(name))
        {
            return McpProtoHelper.FormatError("name is required");
        }

        if (string.IsNullOrWhiteSpace(carrier))
        {
            return McpProtoHelper.FormatError("carrier is required");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return McpProtoHelper.FormatError("currency is required");
        }

        var request = new CreateRateCardRequest
        {
            Name = name,
            Carrier = carrier,
            ServiceLevel = LogisticsEnumHelper.ParseServiceLevel(serviceLevel),
            Currency = currency,
            Validity = new ValidityWindowModel
            {
                StartUnixMs = validityStartUnixMs,
                EndUnixMs = validityEndUnixMs,
            },
        };

        if (!string.IsNullOrWhiteSpace(tariffRows))
        {
            try
            {
                var rows = JsonSerializer.Deserialize<JsonElement[]>(tariffRows);
                if (rows != null)
                {
                    foreach (var r in rows)
                    {
                        request.TariffRows.Add(new TariffRowModel
                        {
                            Zone = r.TryGetProperty("zone", out var z) ? z.GetString() ?? string.Empty : string.Empty,
                            MinKgExclusive = r.TryGetProperty("minKgExclusive", out var mn) ? mn.GetString() ?? "0" : "0",
                            MaxKgInclusive = r.TryGetProperty("maxKgInclusive", out var mx) ? mx.GetString() ?? "0" : "0",
                            Mode = LogisticsEnumHelper.ParseTariffPricingMode(r.TryGetProperty("mode", out var m) ? m.GetString() : null),
                            Price = new MoneyModel
                            {
                                Amount = r.TryGetProperty("priceAmount", out var pa) ? pa.GetString() ?? "0" : "0",
                                Currency = r.TryGetProperty("priceCurrency", out var pc) ? pc.GetString() ?? string.Empty : string.Empty,
                            },
                        });
                    }
                }
            }
            catch (JsonException)
            {
                return McpProtoHelper.FormatError("tariffRows JSON is invalid");
            }
        }

        var card = await client.CreateRateCardAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetRateCardTool.MapRateCard(card), McpJsonDefaults.Options);
    }
}
