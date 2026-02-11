using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.VariantAttrManagement.v1;

namespace DKH.McpGateway.Application.Tools.Variants;

[McpServerToolType]
public static class ManageVariantAttrTool
{
    [McpServerTool(Name = "manage_variant_attr"), Description(
        "Manage variant attributes: create, update, delete, get, or list. " +
        "For create/update: provide variant attribute JSON with fields: productCode, productAttributeCode, " +
        "productAttributeName, controlType, displayOrder, isRequired, allowCustomValue, textPrompt, defaultCustomValue. " +
        "For delete/get: provide variant attribute code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        VariantAttrManagementService.VariantAttrManagementServiceClient client,
        [Description("Action: create, update, delete, get, or list")] string action,
        [Description("Variant attribute JSON (for create/update)")] string? json = null,
        [Description("Variant attribute code (for delete/get)")] string? code = null,
        [Description("Search text (for list)")] string? search = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "create" or "update" => await ManageAsync(client, apiKeyContext, action, json, cancellationToken),
            "delete" => await DeleteAsync(client, apiKeyContext, code, cancellationToken),
            "get" => await GetAsync(client, apiKeyContext, code, cancellationToken),
            "list" => await ListAsync(client, apiKeyContext, search, page, pageSize, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: create, update, delete, get, or list"),
        };
    }

    private static async Task<string> ManageAsync(
        VariantAttrManagementService.VariantAttrManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update");
        }

        if (action.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            var request = McpProtoHelper.Parser.Parse<CreateVariantAttrRequest>(json);
            var response = await client.CreateAsync(request, cancellationToken: ct);
            return McpProtoHelper.FormatManageResponse(response.Success, "created", response.Code, response.Errors);
        }
        else
        {
            var request = McpProtoHelper.Parser.Parse<UpdateVariantAttrRequest>(json);
            var response = await client.UpdateAsync(request, cancellationToken: ct);
            return McpProtoHelper.FormatManageResponse(response.Success, "updated", response.Code, response.Errors);
        }
    }

    private static async Task<string> DeleteAsync(
        VariantAttrManagementService.VariantAttrManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        var response = await client.DeleteAsync(new DeleteVariantAttrRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatManageResponse(response.Success, "deleted", response.Code, response.Errors);
    }

    private static async Task<string> GetAsync(
        VariantAttrManagementService.VariantAttrManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new GetVariantAttrRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatGetResponse(response.Found, response.Data);
    }

    private static async Task<string> ListAsync(
        VariantAttrManagementService.VariantAttrManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListVariantAttrsRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
