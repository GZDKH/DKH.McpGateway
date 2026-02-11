using Google.Protobuf;
using ExportRequest = DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.ExportRequest;
using GetImportTemplateRequest = DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.GetImportTemplateRequest;
using ImportRequest = DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.ImportRequest;
using ReferenceDataExchangeClient =
    DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ValidateImportRequest = DKH.ReferenceService.Contracts.Reference.Api.DataExchange.v1.ValidateImportRequest;

namespace DKH.McpGateway.Application.Tools.DataExchange;

[McpServerToolType]
public static class ReferenceDataTool
{
    [McpServerTool(Name = "reference_data"), Description(
        "Bulk import/export reference data (countries, currencies, languages, etc.). " +
        "Actions: 'import' to bulk import, 'export' to download, " +
        "'validate' to dry-run validation, 'template' to get import template. " +
        "Profiles: countries, currencies, languages, state_provinces, state_province_types, " +
        "cities, quantity_units, price_labels, dimensions, delivery_times, weights. " +
        "Formats: json, csv, excel, xml.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ReferenceDataExchangeClient client,
        [Description("Action: import, export, validate, or template")] string action,
        [Description("Data profile: countries, currencies, languages, state_provinces, cities, quantity_units, etc.")] string profile,
        [Description("File format: json, csv, excel, or xml")] string format = "json",
        [Description("JSON content to import (for import/validate action)")] string? content = null,
        [Description("Language for export (e.g. 'en', 'ru')")] string? language = null,
        [Description("Search filter for export (matches code/name)")] string? search = null,
        [Description("Sort expression: 'field:asc' or 'field:desc' (for export)")] string? orderBy = null,
        [Description("Page number, 1-based (for export)")] int? page = null,
        [Description("Page size (for export)")] int? pageSize = null,
        [Description("Include example data row (for template)")] bool? includeExample = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.Equals(action, "import", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(content))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "content is required for import (JSON string with items array)" },
                    McpJsonDefaults.Options);
            }

            var request = new ImportRequest
            {
                Profile = profile,
                Format = format,
                Content = ByteString.CopyFromUtf8(content),
            };

            var response = await client.ImportAsync(request, cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Failed == 0,
                processed = response.Processed,
                failed = response.Failed,
                errors = response.Errors.ToList(),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "export", StringComparison.OrdinalIgnoreCase))
        {
            var request = new ExportRequest
            {
                Profile = profile,
                Format = format,
            };

            if (!string.IsNullOrEmpty(language))
            {
                request.Language = language;
            }

            if (!string.IsNullOrEmpty(search))
            {
                request.Search = search;
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                request.OrderBy = orderBy;
            }

            if (page.HasValue)
            {
                request.Page = page.Value;
            }

            if (pageSize.HasValue)
            {
                request.PageSize = pageSize.Value;
            }

            var response = await client.ExportAsync(request, cancellationToken: cancellationToken);
            var exportedContent = response.Content.ToStringUtf8();

            return JsonSerializer.Serialize(new
            {
                success = true,
                format,
                profile,
                content = exportedContent,
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "validate", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(content))
            {
                return JsonSerializer.Serialize(
                    new { success = false, error = "content is required for validate" },
                    McpJsonDefaults.Options);
            }

            var response = await client.ValidateImportAsync(
                new ValidateImportRequest
                {
                    Profile = profile,
                    Format = format,
                    Content = ByteString.CopyFromUtf8(content),
                },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Valid,
                totalRecords = response.TotalRecords,
                validRecords = response.ValidRecords,
                errors = response.Errors.Select(e => new
                {
                    e.Line,
                    e.Field,
                    e.Message,
                    e.Value,
                }),
                warnings = response.Warnings.Select(w => new
                {
                    w.Line,
                    w.Field,
                    w.Message,
                }),
            }, McpJsonDefaults.Options);
        }

        if (string.Equals(action, "template", StringComparison.OrdinalIgnoreCase))
        {
            var response = await client.GetImportTemplateAsync(
                new GetImportTemplateRequest
                {
                    Profile = profile,
                    Format = format,
                    IncludeExample = includeExample ?? true,
                },
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                filename = response.Filename,
                contentType = response.ContentType,
                content = response.Content.ToStringUtf8(),
            }, McpJsonDefaults.Options);
        }

        return JsonSerializer.Serialize(
            new { success = false, error = $"Unknown action '{action}'. Use: import, export, validate, or template" },
            McpJsonDefaults.Options);
    }
}
