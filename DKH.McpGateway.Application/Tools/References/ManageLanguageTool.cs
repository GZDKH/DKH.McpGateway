using DKH.ReferenceService.Contracts.Api.LanguagesCrud.V1;
using DKH.ReferenceService.Contracts.Models.Language.V1;

namespace DKH.McpGateway.Application.Tools.References;

[McpServerToolType]
public static class ManageLanguageTool
{
    [McpServerTool(Name = "manage_language"), Description(
        "Create, update, or delete a language. " +
        "For create: provide cultureName and translations. " +
        "For update/delete: provide cultureName to identify the language (e.g. 'en-US', 'ru-RU', 'zh-CN').")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        LanguagesCrudService.LanguagesCrudServiceClient client,
        [Description("Action: create, update, or delete")] string action,
        [Description("Culture name, e.g. 'en-US', 'ru-RU', 'zh-CN' (required)")] string cultureName,
        [Description("Translations as JSON array: [{\"lang\":\"en\",\"name\":\"English\"},{\"lang\":\"ru\",\"name\":\"Английский\"}]")] string? translations = null,
        [Description("Display order (for create/update)")] int? displayOrder = null,
        [Description("Published (for create/update)")] bool? published = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "create", StringComparison.OrdinalIgnoreCase))
        {
            var request = new CreateLanguageRequest
            {
                CultureName = cultureName,
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

            var response = await client.CreateLanguageAsync(request, cancellationToken: cancellationToken);
            var l = response.Languages;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "created",
                language = new { l.Id, l.CultureName, l.NativeName, l.TwoLetterLanguageName },
            }, McpJsonDefaults.Options);
        }

        var found = await FindLanguageByCultureAsync(client, cultureName, cancellationToken);
        if (found is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Language with culture '{cultureName}' not found" },
                McpJsonDefaults.Options);
        }

        if (string.Equals(action, "update", StringComparison.OrdinalIgnoreCase))
        {
            var request = new UpdateLanguageRequest
            {
                Id = found.Id,
                CultureName = cultureName,
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

            var response = await client.UpdateLanguageAsync(request, cancellationToken: cancellationToken);
            var l = response.Languages;

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "updated",
                language = new { l.Id, l.CultureName, l.NativeName, l.TwoLetterLanguageName },
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            await client.DeleteLanguageAsync(
                new DeleteLanguageRequest { Id = found.Id },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                action = "deleted",
                deletedCulture = cultureName,
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: create, update, or delete" },
            McpJsonDefaults.Options);
    }

    private static async Task<Language?> FindLanguageByCultureAsync(
        LanguagesCrudService.LanguagesCrudServiceClient client, string culture, CancellationToken ct)
    {
        var response = await client.GetLanguagesAsync(
            new GetLanguagesRequest { Filter = $"CultureName == \"{culture}\"", PageSize = 1 },
            cancellationToken: ct);

        return response.Languages.FirstOrDefault();
    }

    private static void AddTranslations(
        Google.Protobuf.Collections.RepeatedField<LanguageTranslation> field, string? json)
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
            field.Add(new LanguageTranslation
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
