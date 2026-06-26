using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class UpdateRateCardTool
{
    [McpServerTool(Name = "update_rate_card"), Description(
        "Update a rate card name, validity window and tariff rows (rows replace wholesale). " +
        "Returns the updated rate card. " +
        "Returns NotFound on bad ID, InvalidArgument on overlapping tariff bands.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        RateCardService.RateCardServiceClient client,
        [Description("Rate card ID (required)")] string id,
        [Description("New name (optional)")] string? name = null,
        [Description("Validity start unix ms (optional)")] long? validityStartUnixMs = null,
        [Description("Validity end unix ms (optional, 0 = open-ended)")] long? validityEndUnixMs = null,
        [Description("Tariff rows as JSON array (replaces all rows): [{\"zone\":\"ZONE_A\",\"minKgExclusive\":\"0\",\"maxKgInclusive\":\"5\",\"mode\":\"Flat\",\"priceAmount\":\"9.99\",\"priceCurrency\":\"USD\"}]")] string? tariffRows = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(id))
        {
            return McpProtoHelper.FormatError("id is required");
        }

        var request = new UpdateRateCardRequest { Id = id };

        if (!string.IsNullOrWhiteSpace(name))
        {
            request.Name = name;
        }

        if (validityStartUnixMs.HasValue || validityEndUnixMs.HasValue)
        {
            request.Validity = new ValidityWindowModel
            {
                StartUnixMs = validityStartUnixMs ?? 0,
                EndUnixMs = validityEndUnixMs ?? 0,
            };
        }

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

        var card = await client.UpdateRateCardAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetRateCardTool.MapRateCard(card), McpJsonDefaults.Options);
    }
}
