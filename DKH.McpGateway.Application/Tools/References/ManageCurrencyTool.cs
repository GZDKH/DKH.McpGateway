using DKH.ReferenceService.Contracts.Api.CurrenciesCrud.V1;
using DKH.ReferenceService.Contracts.Models.Currency.V1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageCurrencyTool
{
    [McpServerTool(Name = "manage_currency"), Description(
        "Create, update, or delete a currency. " +
        "For create: provide code, symbol, rate, and translations. " +
        "For update/delete: provide code to identify the currency (e.g. 'USD', 'EUR', 'CNY').")]
    public static async Task<string> ExecuteAsync(
        CurrenciesCrudService.CurrenciesCrudServiceClient client,
        [Description("Action: create, update, or delete")] string action,
        [Description("Currency code, e.g. 'USD', 'EUR', 'CNY' (required)")] string code,
        [Description("Currency symbol, e.g. '$', '€', '¥' (for create)")] string? symbol = null,
        [Description("Exchange rate relative to primary currency (for create/update)")] double? rate = null,
        [Description("Is primary currency (for create/update)")] bool? isPrimary = null,
        [Description("Custom formatting string (for create/update)")] string? customFormatting = null,
        [Description("Translations as JSON array: [{\"lang\":\"en\",\"name\":\"US Dollar\"},{\"lang\":\"ru\",\"name\":\"Доллар США\"}]")] string? translations = null,
        [Description("Display order (for create/update)")] int? displayOrder = null,
        [Description("Published (for create/update)")] bool? published = null,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "symbol is required for create" },
                    McpJsonDefaults.Options);
            }

            var request = new CreateCurrencyRequest
            {
                Code = code,
                Symbol = symbol,
                Rate = rate ?? 1.0,
                IsPrimary = isPrimary ?? false,
                CustomFormatting = customFormatting ?? "",
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

            var response = await client.CreateCurrencyAsync(request, cancellationToken: cancellationToken);
            var c = response.Currency;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                currency = new { c.Id, c.Code, c.Symbol, c.Rate, c.IsPrimary },
            }, McpJsonDefaults.Options);
        }

        var found = await client.GetCurrencyByCodeAsync(
            new GetCurrencyByCodeRequest { Code = code },
            cancellationToken: cancellationToken);

        if (found.Currency is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Currency with code '{code}' not found" },
                McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var request = new UpdateCurrencyRequest
            {
                Id = found.Currency.Id,
                Code = code,
                Symbol = symbol ?? found.Currency.Symbol,
                Rate = rate ?? found.Currency.Rate,
                IsPrimary = isPrimary ?? found.Currency.IsPrimary,
                CustomFormatting = customFormatting ?? found.Currency.CustomFormatting,
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
                request.Translations.AddRange(found.Currency.Translations);
            }

            var response = await client.UpdateCurrencyAsync(request, cancellationToken: cancellationToken);
            var c = response.Currency;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                currency = new { c.Id, c.Code, c.Symbol, c.Rate, c.IsPrimary },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            await client.DeleteCurrencyAsync(
                new DeleteCurrencyRequest { Id = found.Currency.Id },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedCode = code,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }

    private static void AddTranslations(
        Google.Protobuf.Collections.RepeatedField<CurrencyTranslation> field, string? json)
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
            field.Add(new CurrencyTranslation
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
