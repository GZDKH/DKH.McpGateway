using System.Globalization;
using DKH.CustomsService.Contracts.Customs.Api.Declarations.v1;
using DKH.CustomsService.Contracts.DutyRule.Models.V2;
using DKH.CustomsService.Contracts.NationalHsCode.Models.V2;
using DKH.CustomsService.Contracts.NomenclatureSystem.Models.V2;
using DKH.CustomsService.Contracts.WcoHsCode.Models.V2;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Customs;

internal static class CustomsToolInput
{
    internal static readonly JsonSerializerOptions JsonOptions = new(McpJsonDefaults.Options)
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static Timestamp? ParseTimestamp(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));

    internal static TEnum ParseEnumOrDefault<TEnum>(string? value)
        where TEnum : struct, System.Enum
        => !string.IsNullOrWhiteSpace(value) && System.Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : default;

    internal static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, System.Enum
        => System.Enum.TryParse(value, ignoreCase: true, out parsed);

    internal static string? PopulateDeclarationItems(RepeatedField<DeclarationItemRequest> destination, string? itemsJson)
    {
        if (string.IsNullOrWhiteSpace(itemsJson))
        {
            return "itemsJson is required";
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<DeclarationItemInput>>(itemsJson, JsonOptions);
            if (items is null || items.Count == 0)
            {
                return "itemsJson must contain at least one item";
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.HsCode))
                {
                    return "item.hsCode is required";
                }

                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    return "item.description is required";
                }

                destination.Add(new DeclarationItemRequest
                {
                    HsCode = item.HsCode,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    DeclaredValue = item.DeclaredValue,
                });
            }
        }
        catch (JsonException)
        {
            return "itemsJson is invalid JSON";
        }

        return null;
    }

    internal static string? PopulateDutyLines(RepeatedField<DutyLine> destination, string? linesJson)
    {
        if (string.IsNullOrWhiteSpace(linesJson))
        {
            return "linesJson is required";
        }

        try
        {
            var lines = JsonSerializer.Deserialize<List<DutyLineInput>>(linesJson, JsonOptions);
            if (lines is null || lines.Count == 0)
            {
                return "linesJson must contain at least one line";
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.HsCode))
                {
                    return "line.hsCode is required";
                }

                if (string.IsNullOrWhiteSpace(line.Quantity))
                {
                    return "line.quantity is required";
                }

                if (string.IsNullOrWhiteSpace(line.DeclaredValue))
                {
                    return "line.declaredValue is required";
                }

                if (string.IsNullOrWhiteSpace(line.ValueCurrency))
                {
                    return "line.valueCurrency is required";
                }

                destination.Add(new DutyLine
                {
                    HsCode = line.HsCode,
                    SystemCode = line.SystemCode ?? string.Empty,
                    Origin = line.Origin ?? string.Empty,
                    Quantity = line.Quantity,
                    DeclaredValue = line.DeclaredValue,
                    ValueCurrency = line.ValueCurrency,
                });
            }
        }
        catch (JsonException)
        {
            return "linesJson is invalid JSON";
        }

        return null;
    }

    internal static string? PopulateWcoTranslations(
        RepeatedField<WcoHsCodeTranslationModel> destination,
        string? translationsJson)
    {
        var parse = ParseTranslationInputs(translationsJson, "translationsJson");
        if (parse.Error is not null)
        {
            return parse.Error;
        }

        foreach (var item in parse.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return "translation.description is required";
            }

            destination.Add(new WcoHsCodeTranslationModel
            {
                LanguageCode = item.Code,
                Description = item.Description,
                ShortName = item.ShortName ?? string.Empty,
            });
        }

        return null;
    }

    internal static string? PopulateNationalTranslations(
        RepeatedField<NationalHsCodeTranslationModel> destination,
        string? translationsJson)
    {
        var parse = ParseTranslationInputs(translationsJson, "translationsJson");
        if (parse.Error is not null)
        {
            return parse.Error;
        }

        foreach (var item in parse.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return "translation.description is required";
            }

            destination.Add(new NationalHsCodeTranslationModel
            {
                LanguageCode = item.Code,
                Description = item.Description,
                ShortName = item.ShortName ?? string.Empty,
            });
        }

        return null;
    }

    internal static string? PopulateNomenclatureTranslations(
        RepeatedField<NomenclatureSystemTranslationModel> destination,
        string? translationsJson)
    {
        if (string.IsNullOrWhiteSpace(translationsJson))
        {
            return null;
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<NomenclatureTranslationInput>>(translationsJson, JsonOptions) ?? [];
            foreach (var item in items)
            {
                var code = item.LanguageCode ?? item.Lang;
                if (string.IsNullOrWhiteSpace(code))
                {
                    return "translation.languageCode is required";
                }

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    return "translation.name is required";
                }

                destination.Add(new NomenclatureSystemTranslationModel
                {
                    LanguageCode = code,
                    Name = item.Name,
                    Description = item.Description ?? string.Empty,
                });
            }
        }
        catch (JsonException)
        {
            return "translationsJson is invalid JSON";
        }

        return null;
    }

    internal static string? PopulateIsoCountryCodes(RepeatedField<string> destination, string? isoCountryCodesJson)
    {
        if (string.IsNullOrWhiteSpace(isoCountryCodesJson))
        {
            return null;
        }

        try
        {
            var codes = JsonSerializer.Deserialize<List<string>>(isoCountryCodesJson, JsonOptions) ?? [];
            foreach (var code in codes.Where(code => !string.IsNullOrWhiteSpace(code)))
            {
                destination.Add(code);
            }
        }
        catch (JsonException)
        {
            return "isoCountryCodesJson is invalid JSON";
        }

        return null;
    }

    private static (IReadOnlyList<TranslationInput> Items, string? Error) ParseTranslationInputs(
        string? translationsJson,
        string argumentName)
    {
        if (string.IsNullOrWhiteSpace(translationsJson))
        {
            return ([], null);
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<TranslationInput>>(translationsJson, JsonOptions) ?? [];
            foreach (var item in items)
            {
                item.Code = item.LanguageCode ?? item.Lang ?? string.Empty;
                if (string.IsNullOrWhiteSpace(item.Code))
                {
                    return ([], "translation.languageCode is required");
                }
            }

            return (items, null);
        }
        catch (JsonException)
        {
            return ([], $"{argumentName} is invalid JSON");
        }
    }

    private sealed record DeclarationItemInput(
        string HsCode,
        string Description,
        double Quantity,
        double DeclaredValue);

    private sealed record DutyLineInput(
        string HsCode,
        string? SystemCode,
        string? Origin,
        string Quantity,
        string DeclaredValue,
        string ValueCurrency);

    private sealed class TranslationInput
    {
        public string? Lang { get; init; }
        public string? LanguageCode { get; init; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; init; }
        public string? ShortName { get; init; }
    }

    private sealed class NomenclatureTranslationInput
    {
        public string? Lang { get; init; }
        public string? LanguageCode { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
    }
}
