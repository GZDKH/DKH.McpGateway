using System.Diagnostics;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using ExportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ExportRequest;
using GetImportTemplateRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.GetImportTemplateRequest;
using ImportOptions = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportOptions;
using ImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ImportRequest;
using ProductCatalogDataExchangeClient =
    DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.DataExchangeService.DataExchangeServiceClient;
using ValidateImportRequest = DKH.ProductCatalogService.Contracts.ProductCatalog.Api.DataExchange.v1.ValidateImportRequest;

namespace DKH.McpGateway.Application.Tools.DataExchange;

[McpServerToolType]
public static class ProductCatalogDataTool
{
    [McpServerTool(Name = "product_catalog_data"), Description(
        "Workspace-scoped merchant import/export for product catalog data. " +
        "Requires authenticated HTTP, an MCP-scoped API key, and exactly one X-Workspace-Id header. " +
        "Actions: 'import' to bulk import data, 'export' to download data, " +
        "'validate' to dry-run validation, 'template' to get import template. " +
        "Profiles: products, categories, tags, packages, " +
        "catalogs, product_attributes, product_attribute_groups, product_attribute_options, " +
        "specification_attributes, specification_attribute_groups, specification_attribute_options. " +
        "Formats: json, csv, excel, xml.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        ProductCatalogDataExchangeClient client,
        [Description("Action: import, export, validate, or template")] string action,
        [Description("Data profile: products, categories, tags, packages, catalogs, etc.")] string profile,
        [Description("File format: json, csv, excel, or xml")] string format = "json",
        [Description("JSON content to import (for import/validate action)")] string? content = null,
        [Description("Update existing records if code matches (for import)")] bool? updateExisting = null,
        [Description("Skip rows with errors instead of failing (for import)")] bool? skipErrors = null,
        [Description("Auto-create missing spec groups (for import)")] bool? createMissingGroups = null,
        [Description("Auto-create missing spec attributes (for import)")] bool? createMissingAttributes = null,
        [Description("Auto-create missing spec options (for import)")] bool? createMissingOptions = null,
        [Description("Default language for translations (for import, e.g. 'en')")] string? defaultLanguage = null,
        [Description("Language for export (e.g. 'en', 'ru')")] string? language = null,
        [Description("Search filter for export (matches code/name)")] string? search = null,
        [Description("Filter by published status: true/false (for export)")] string? published = null,
        [Description("Sort expression: 'field:asc' or 'field:desc' (for export)")] string? orderBy = null,
        [Description("Page number, 1-based (for export)")] int? page = null,
        [Description("Page size (for export)")] int? pageSize = null,
        [Description("Include example data row (for template)")] bool? includeExample = null,
        CancellationToken cancellationToken = default)
    {
        var workspaceMetadata = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            apiKeyContext,
            httpContextAccessor);

        var normalizedAction = action?.Trim().ToLowerInvariant();
        if (normalizedAction is not ("import" or "export" or "validate" or "template"))
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Unknown action '{action}'. Use: import, export, validate, or template" },
                McpJsonDefaults.Options);
        }

        apiKeyContext.EnsurePermission(
            normalizedAction == "import" ? McpPermissions.Write : McpPermissions.Read);

        if (normalizedAction == "import")
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
                Options = new ImportOptions
                {
                    UpdateExisting = updateExisting ?? false,
                    SkipErrors = skipErrors ?? false,
                    CreateMissingGroups = createMissingGroups ?? true,
                    CreateMissingAttributes = createMissingAttributes ?? true,
                    CreateMissingOptions = createMissingOptions ?? true,
                    DefaultLanguage = defaultLanguage ?? "en",
                },
            };

            var response = await client.ImportAsync(
                request,
                headers: workspaceMetadata,
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = response.Failed == 0,
                processed = response.Processed,
                failed = response.Failed,
                errors = response.Errors.ToList(),
            }, McpJsonDefaults.Options);
        }

        if (normalizedAction == "export")
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

            if (!string.IsNullOrEmpty(published))
            {
                request.Published = published;
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

            var response = await client.ExportAsync(
                request,
                headers: workspaceMetadata,
                cancellationToken: cancellationToken);
            var exportedContent = response.Content.ToStringUtf8();

            return JsonSerializer.Serialize(new
            {
                success = true,
                format,
                profile,
                content = exportedContent,
            }, McpJsonDefaults.Options);
        }

        if (normalizedAction == "validate")
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
                headers: workspaceMetadata,
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

        if (normalizedAction == "template")
        {
            var response = await client.GetImportTemplateAsync(
                new GetImportTemplateRequest
                {
                    Profile = profile,
                    Format = format,
                    IncludeExample = includeExample ?? true,
                },
                headers: workspaceMetadata,
                cancellationToken: cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                filename = response.Filename,
                contentType = response.ContentType,
                content = response.Content.ToStringUtf8(),
            }, McpJsonDefaults.Options);
        }

        throw new UnreachableException();
    }
}
