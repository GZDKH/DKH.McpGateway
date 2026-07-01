using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using DKH.EngagementService.Contracts.Engagement.Models.V1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Engagement;

internal static class EngagementToolInput
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static GuidValue ToGuid(string value) => new(value.Trim());

    internal static GuidValue? ToGuidOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new GuidValue(value.Trim());

    internal static string? OptionalString(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static bool Required(string? value, string fieldName, [NotNullWhen(false)] out string? error)
    {
        error = string.IsNullOrWhiteSpace(value) ? $"{fieldName} is required" : null;
        return error is null;
    }

    internal static Timestamp? ParseTimestamp(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).ToUniversalTime());

    internal static TEnum ParseOptionalEnum<TEnum>(string? value, out string? error)
        where TEnum : struct, System.Enum
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        if (TryParseEnum<TEnum>(value, out var parsed))
        {
            return parsed;
        }

        error = $"{typeof(TEnum).Name} value is invalid";
        return default;
    }

    internal static TEnum ParseRequiredEnum<TEnum>(string? value, string fieldName, out string? error)
        where TEnum : struct, System.Enum
    {
        error = null;
        if (!TryParseEnum<TEnum>(value, out var parsed) || EqualityComparer<TEnum>.Default.Equals(parsed, default))
        {
            error = $"{fieldName} is invalid";
            return default;
        }

        return parsed;
    }

    internal static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, System.Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (System.Enum.TryParse(value.Trim(), ignoreCase: true, out parsed))
        {
            return true;
        }

        var normalized = NormalizeEnumToken(value);
        foreach (var enumName in System.Enum.GetNames<TEnum>())
        {
            var normalizedEnumName = NormalizeEnumToken(enumName);
            if (string.Equals(normalizedEnumName, normalized, StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(normalizedEnumName, StringComparison.OrdinalIgnoreCase))
            {
                parsed = System.Enum.Parse<TEnum>(enumName);
                return true;
            }
        }

        return false;
    }

    internal static FormSchemaModel? ParseSchema(string? schemaJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return null;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<FormSchemaDto>(schemaJson, JsonOptions);
            var schema = new FormSchemaModel();
            foreach (var sectionDto in dto?.Sections ?? [])
            {
                var section = new FormSectionModel
                {
                    Key = sectionDto.Key ?? string.Empty,
                    Title = sectionDto.Title ?? string.Empty,
                    Order = sectionDto.Order,
                };

                foreach (var fieldDto in sectionDto.Fields ?? [])
                {
                    var fieldType = ParseRequiredEnum<FormFieldType>(fieldDto.Type, "field.type", out error);
                    if (error is not null)
                    {
                        return null;
                    }

                    var field = new FormFieldModel
                    {
                        Key = fieldDto.Key ?? string.Empty,
                        Label = fieldDto.Label ?? string.Empty,
                        Type = fieldType,
                        Required = fieldDto.Required,
                        Order = fieldDto.Order,
                        MinValue = OptionalString(fieldDto.MinValue),
                        MaxValue = OptionalString(fieldDto.MaxValue),
                        HelpText = OptionalString(fieldDto.HelpText),
                    };
                    field.Options.Add(fieldDto.Options?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? []);
                    section.Fields.Add(field);
                }

                schema.Sections.Add(section);
            }

            return schema;
        }
        catch (JsonException)
        {
            error = "schemaJson must be a JSON object";
            return null;
        }
    }

    internal static FormAnswerModel[] ParseAnswers(string? answersJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(answersJson))
        {
            return [];
        }

        try
        {
            var items = JsonSerializer.Deserialize<FormAnswerDto[]>(answersJson, JsonOptions) ?? [];
            return [.. items.Select(dto =>
            {
                var answer = new FormAnswerModel
                {
                    FieldKey = dto.FieldKey ?? string.Empty,
                    Value = OptionalString(dto.Value),
                };
                answer.Values.Add(dto.Values?.Where(v => !string.IsNullOrWhiteSpace(v)) ?? []);
                answer.AttachmentIds.Add((dto.AttachmentIds ?? []).Where(v => !string.IsNullOrWhiteSpace(v)).Select(ToGuid));
                return answer;
            })];
        }
        catch (JsonException)
        {
            error = "answersJson must be a JSON array";
            return [];
        }
    }

    private static string NormalizeEnumToken(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
        }

        return builder.ToString();
    }

    private sealed record FormSchemaDto(FormSectionDto[]? Sections);

    private sealed record FormSectionDto(string? Key, string? Title, int Order, FormFieldDto[]? Fields);

    private sealed record FormFieldDto(
        string? Key,
        string? Label,
        string? Type,
        bool Required,
        int Order,
        string[]? Options,
        string? MinValue,
        string? MaxValue,
        string? HelpText);

    private sealed record FormAnswerDto(string? FieldKey, string? Value, string[]? Values, string[]? AttachmentIds);
}
