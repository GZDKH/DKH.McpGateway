using DKH.LogisticsService.Contracts.Models.v1;
using DKH.LogisticsService.Contracts.Services.v1;

namespace DKH.McpGateway.Application.Tools.Logistics;

[McpServerToolType]
public static class CalculateRatesTool
{
    [McpServerTool(Name = "calculate_rates"), Description(
        "Calculate shipping rates for parcels between two locations. " +
        "Returns route options with carrier, service level, transit time, price breakdown and surcharges. " +
        "Returns InvalidArgument on malformed request, FailedPrecondition when no carrier covers the zone+service-level.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        QuoteService.QuoteServiceClient client,
        [Description("Origin country code (ISO 3166-alpha-2)")] string originCountry,
        [Description("Destination country code (ISO 3166-alpha-2)")] string destinationCountry,
        [Description("Origin region (optional)")] string? originRegion = null,
        [Description("Origin postal code (optional)")] string? originPostalCode = null,
        [Description("Origin city (optional)")] string? originCity = null,
        [Description("Destination region (optional)")] string? destinationRegion = null,
        [Description("Destination postal code (optional)")] string? destinationPostalCode = null,
        [Description("Destination city (optional)")] string? destinationCity = null,
        [Description("Parcels as JSON array: [{\"weightValue\":\"1.5\",\"weightUnit\":\"Kilogram\",\"length\":\"30\",\"width\":\"20\",\"height\":\"15\",\"lengthUnit\":\"Centimeter\",\"quantity\":1}]")] string? parcels = null,
        [Description("Preferred currency code (e.g. USD)")] string? preferredCurrency = null,
        [Description("Preferred service levels as comma-separated (e.g. Standard,Express)")] string? preferredServiceLevels = null,
        [Description("Restriction tags as comma-separated (optional)")] string? restrictionTags = null,
        [Description("Unix timestamp ms of the request (0 = now)")] long requestedAtUnixMs = 0,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(originCountry))
        {
            return McpProtoHelper.FormatError("originCountry is required");
        }

        if (string.IsNullOrWhiteSpace(destinationCountry))
        {
            return McpProtoHelper.FormatError("destinationCountry is required");
        }

        var quote = new QuoteRequestModel
        {
            Origin = new LocationCodeModel
            {
                CountryCode = originCountry,
                Region = originRegion ?? string.Empty,
                PostalCode = originPostalCode ?? string.Empty,
                City = originCity ?? string.Empty,
            },
            Destination = new LocationCodeModel
            {
                CountryCode = destinationCountry,
                Region = destinationRegion ?? string.Empty,
                PostalCode = destinationPostalCode ?? string.Empty,
                City = destinationCity ?? string.Empty,
            },
            PreferredCurrency = preferredCurrency ?? string.Empty,
            RequestedAtUnixMs = requestedAtUnixMs,
        };

        if (!string.IsNullOrWhiteSpace(parcels))
        {
            try
            {
                var parcelItems = JsonSerializer.Deserialize<JsonElement[]>(parcels);
                if (parcelItems != null)
                {
                    foreach (var p in parcelItems)
                    {
                        var parcel = new QuoteParcelModel
                        {
                            ActualWeight = new WeightModel
                            {
                                Value = p.TryGetProperty("weightValue", out var wv) ? wv.GetString() ?? "0" : "0",
                                Unit = LogisticsEnumHelper.ParseWeightUnit(p.TryGetProperty("weightUnit", out var wu) ? wu.GetString() : null),
                            },
                            Dimensions = new DimensionsModel
                            {
                                Length = p.TryGetProperty("length", out var l) ? l.GetString() ?? "0" : "0",
                                Width = p.TryGetProperty("width", out var w) ? w.GetString() ?? "0" : "0",
                                Height = p.TryGetProperty("height", out var h) ? h.GetString() ?? "0" : "0",
                                Unit = LogisticsEnumHelper.ParseLengthUnit(p.TryGetProperty("lengthUnit", out var lu) ? lu.GetString() : null),
                            },
                            Quantity = p.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1,
                        };
                        quote.Parcels.Add(parcel);
                    }
                }
            }
            catch (JsonException)
            {
                return McpProtoHelper.FormatError("parcels JSON is invalid");
            }
        }

        if (!string.IsNullOrWhiteSpace(preferredServiceLevels))
        {
            foreach (var level in preferredServiceLevels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                quote.PreferredServiceLevels.Add(LogisticsEnumHelper.ParseServiceLevel(level));
            }
        }

        if (!string.IsNullOrWhiteSpace(restrictionTags))
        {
            foreach (var tag in restrictionTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                quote.RestrictionTags.Add(tag);
            }
        }

        var response = await client.CalculateRatesAsync(
            new CalculateRatesRequest { Quote = quote },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            options = response.Options.Select(o => new
            {
                carrier = o.Carrier,
                serviceLevel = o.ServiceLevel.ToString(),
                estimatedTransitSeconds = o.EstimatedTransitSeconds,
                restrictions = o.Restrictions.ToArray(),
                price = o.Price is null ? null : (object)new
                {
                    basePrice = o.Price.BasePrice is null ? null : (object)new
                    {
                        amount = o.Price.BasePrice.Amount,
                        currency = o.Price.BasePrice.Currency,
                    },
                    total = o.Price.Total is null ? null : (object)new
                    {
                        amount = o.Price.Total.Amount,
                        currency = o.Price.Total.Currency,
                    },
                    surcharges = o.Price.Surcharges.Select(s => new
                    {
                        name = s.Name,
                        kind = s.Kind,
                        amount = s.Amount is null ? null : (object)new
                        {
                            amount = s.Amount.Amount,
                            currency = s.Amount.Currency,
                        },
                    }),
                },
                fxRate = o.FxRate is null ? null : (object)new
                {
                    baseCurrency = o.FxRate.BaseCurrency,
                    targetCurrency = o.FxRate.TargetCurrency,
                    rate = o.FxRate.Rate,
                    asOf = DateTimeOffset.FromUnixTimeMilliseconds(o.FxRate.AsOfUnixMs).ToString("O"),
                },
            }),
        }, McpJsonDefaults.Options);
    }
}
