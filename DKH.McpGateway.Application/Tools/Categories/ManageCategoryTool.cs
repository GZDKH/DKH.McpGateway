using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;

namespace DKH.McpGateway.Application.Tools.Categories;

[McpServerToolType]
public static class ManageCategoryTool
{
    [McpServerTool(Name = "manage_category"), Description(
        "Manage categories: create, update, delete, get, or list. " +
        "For create/update: provide category JSON with fields: code, parentCode, displayOrder, published, " +
        "categoryType, centerLatitude, centerLongitude, translations [{languageCode, name, description, seoName}], " +
        "specs [{group, attribute, option, type, value, showOnPage, order}]. " +
        "For delete/get: provide category code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        CategoryManagementService.CategoryManagementServiceClient client,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Category JSON (for create/update)")] string? json = null,
        [Description("Category code (for delete/get)")] string? code = null,
        [Description("Search text (for list)")] string? search = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        [Description("Language code to filter translations (for get/list)")] string? language = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "create" or "update" => await ManageAsync(client, apiKeyContext, action, json, cancellationToken),
            "delete" => await DeleteAsync(client, apiKeyContext, code, cancellationToken),
            "get" => await GetAsync(client, apiKeyContext, code, language, cancellationToken),
            "list" => await ListAsync(client, apiKeyContext, search, page, pageSize, language, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: create, update, delete, get, or list"),
        };
    }

    private static async Task<string> ManageAsync(
        CategoryManagementService.CategoryManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update");
        }

        if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            var request = McpProtoHelper.Parser.Parse<CreateCategoryRequest>(json);
            var data = await client.CreateAsync(request, cancellationToken: ct);
            return McpProtoHelper.Formatter.Format(data);
        }
        else
        {
            var request = McpProtoHelper.Parser.Parse<UpdateCategoryRequest>(json);
            var data = await client.UpdateAsync(request, cancellationToken: ct);
            return McpProtoHelper.Formatter.Format(data);
        }
    }

    private static async Task<string> DeleteAsync(
        CategoryManagementService.CategoryManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        await client.DeleteAsync(new DeleteCategoryRequest { Code = code }, cancellationToken: ct);
        return $"Deleted: {code}";
    }

    private static async Task<string> GetAsync(
        CategoryManagementService.CategoryManagementServiceClient client,
        IApiKeyContext ctx, string? code, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var data = await client.GetAsync(
            new GetCategoryRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.Formatter.Format(data);
    }

    private static async Task<string> ListAsync(
        CategoryManagementService.CategoryManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListCategoriesRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
