using DKH.ReferenceService.Contracts.Api.CountriesCrud.V1;
using DKH.ReferenceService.Contracts.Models.Country.V1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageCountryTool
{
    [McpServerTool(Name = "manage_country"), Description(
        "Create, update, or delete a country. " +
        "For create: provide twoLetterCode, threeLetterCode, nativeName, and translations. " +
        "For update/delete: provide twoLetterCode to identify the country.")]
    public static async Task<string> ExecuteAsync(
        CountriesCrudService.CountriesCrudServiceClient client,
        [Description("Action: create, update, or delete")] string action,
        [Description("ISO two-letter code, e.g. 'US', 'CN', 'RU' (required)")] string twoLetterCode,
        [Description("ISO three-letter code, e.g. 'USA', 'CHN', 'RUS' (for create)")] string? threeLetterCode = null,
        [Description("ISO numeric code, e.g. 840 for USA (for create)")] int? numericCode = null,
        [Description("Native name, e.g. 'United States' (for create/update)")] string? nativeName = null,
        [Description("Translations as JSON array: [{\"lang\":\"en\",\"name\":\"United States\"},{\"lang\":\"ru\",\"name\":\"США\"}]")] string? translations = null,
        [Description("Display order (for create/update)")] int? displayOrder = null,
        [Description("Published (for create/update)")] bool? published = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(threeLetterCode) || string.IsNullOrEmpty(nativeName))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "threeLetterCode and nativeName are required for create" },
                    McpJsonDefaults.Options);
            }

            var request = new CreateCountryRequest
            {
                TwoLetterCode = twoLetterCode,
                ThreeLetterCode = threeLetterCode,
                NumericCode = numericCode ?? 0,
                NativeName = nativeName,
            };

            if (displayOrder.HasValue)
            {
                request.DisplayOrder = displayOrder.Value;
            }

            if (published.HasValue)
            {
                request.Published = published.Value;
            }

            AddTranslations(request.Translations, translations);

            var response = await client.CreateCountryAsync(request, cancellationToken: cancellationToken);
            var c = response.Country;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                country = new { c.Id, c.TwoLetterCode, c.ThreeLetterCode, c.NativeName },
            }, McpJsonDefaults.Options);
        }

        var found = await FindCountryByCodeAsync(client, twoLetterCode, cancellationToken);
        if (found is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Country with code '{twoLetterCode}' not found" },
                McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var request = new UpdateCountryRequest
            {
                Id = found.Id,
                TwoLetterCode = twoLetterCode,
                ThreeLetterCode = threeLetterCode ?? found.ThreeLetterCode,
                NumericCode = numericCode ?? found.NumericCode,
                NativeName = nativeName ?? found.NativeName,
            };

            if (displayOrder.HasValue)
            {
                request.DisplayOrder = displayOrder.Value;
            }

            if (published.HasValue)
            {
                request.Published = published.Value;
            }

            if (!string.IsNullOrEmpty(translations))
            {
                AddTranslations(request.Translations, translations);
            }
            else
            {
                request.Translations.AddRange(found.Translations);
            }

            var response = await client.UpdateCountryAsync(request, cancellationToken: cancellationToken);
            var c = response.Country;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                country = new { c.Id, c.TwoLetterCode, c.ThreeLetterCode, c.NativeName },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            await client.DeleteCountryAsync(
                new DeleteCountryRequest { Id = found.Id },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedCode = twoLetterCode,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }

    private static async Task<Country?> FindCountryByCodeAsync(
        CountriesCrudService.CountriesCrudServiceClient client, string code, CancellationToken ct)
    {
        var response = await client.GetCountriesAsync(
            new GetCountriesRequest { Filter = $"TwoLetterCode == \"{code}\"", PageSize = 1 },
            cancellationToken: ct);

        return response.Country.FirstOrDefault();
    }

    private static void AddTranslations(
        Google.Protobuf.Collections.RepeatedField<CountryTranslation> field, string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        var items = JsonSerializer.Deserialize<List<TranslationInput>>(json, McpJsonDefaults.Options);
        if (items is null)
        {
            return;
        }

        foreach (var item in items)
        {
            field.Add(new CountryTranslation
            {
                LanguageCode = item.Lang ?? "",
                Name = item.Name ?? "",
            });
        }
    }

    private sealed class TranslationInput
    {
        public string? Lang { get; set; }
        public string? Name { get; set; }
    }
}
