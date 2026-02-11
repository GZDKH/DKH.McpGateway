using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductAttrGroupManagement.v1;

namespace DKH.McpGateway.Application.Tools.ProductAttributes;

[McpServerToolType]
public static class ManageProductAttrGroupTool
{
    [McpServerTool(Name = "manage_product_attr_group"), Description(
        "Manage product attribute groups: create, update, delete, get, or list. " +
        "For create/update: provide product attribute group JSON with fields: code, iconName, displayOrder, published, " +
        "isCollapsible, isExpandedByDefault, translations [{languageCode, name, description}], attributes [{...}]. " +
        "For delete/get: provide product attribute group code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient client,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Product attribute group JSON (for create/update)")] string? json = null,
        [Description("Product attribute group code (for delete/get)")] string? code = null,
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
        ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update");
        }

        if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            var request = McpProtoHelper.Parser.Parse<CreateProductAttrGroupRequest>(json);
            var response = await client.CreateAsync(request, cancellationToken: ct);
            return McpProtoHelper.FormatManageResponse(response.Success, "created", response.Code, response.Errors);
        }
        else
        {
            var request = McpProtoHelper.Parser.Parse<UpdateProductAttrGroupRequest>(json);
            var response = await client.UpdateAsync(request, cancellationToken: ct);
            return McpProtoHelper.FormatManageResponse(response.Success, "updated", response.Code, response.Errors);
        }
    }

    private static async Task<string> DeleteAsync(
        ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        var response = await client.DeleteAsync(new DeleteProductAttrGroupRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatManageResponse(response.Success, "deleted", response.Code, response.Errors);
    }

    private static async Task<string> GetAsync(
        ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient client,
        IApiKeyContext ctx, string? code, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new GetProductAttrGroupRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.FormatGetResponse(response.Found, response.Data);
    }

    private static async Task<string> ListAsync(
        ProductAttrGroupManagementService.ProductAttrGroupManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListProductAttrGroupsRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
