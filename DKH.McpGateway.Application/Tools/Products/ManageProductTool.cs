using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class ManageProductTool
{
    [McpServerTool(Name = "manage_product"), Description(
        "Manage products: create, update, upsert, delete, get, or list. " +
        "For create/update/upsert: provide product JSON with fields: code, sku, mpn, gtin, displayOrder, published, " +
        "brand (code), manufacturer (code), price, oldPrice, catalogPrice, productCost, " +
        "markAsNew, markAsNewStartDate, markAsNewEndDate, " +
        "translations [{languageCode, name, description, seoName}], " +
        "specifications [{group, attribute, option, type, value, showOnPage, order}], " +
        "tags [{code}], media [{media, isCover, displayOrder, titles [{languageCode, title}]}], " +
        "tierPrices [{catalog, quantity, price}], catalogPrices [{catalog, price, oldPrice}], " +
        "packages [{package, quantity, isDefault}], " +
        "origins [{country, state, city, place, altitude {min, max, unit}, coordinates {lat, lng}, notes}], " +
        "related [{product, order}], crossSells [{product}]. " +
        "For delete/get: provide product code. For list: optionally provide search, page, pageSize.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        ProductManagementService.ProductManagementServiceClient client,
        [Description("Action: create, update, upsert, delete, get, or list")] string action,
        [Description("Product JSON (for create/update/upsert)")] string? json = null,
        [Description("Product code (for delete/get)")] string? code = null,
        [Description("Search text (for list)")] string? search = null,
        [Description("Page number (for list, default 1)")] int? page = null,
        [Description("Page size (for list, default 20)")] int? pageSize = null,
        [Description("Language code to filter translations (for get/list)")] string? language = null,
        CancellationToken cancellationToken = default)
    {
        return action.ToLowerInvariant() switch
        {
            "create" or "update" or "upsert" => await ManageAsync(client, apiKeyContext, action, json, cancellationToken),
            "delete" => await DeleteAsync(client, apiKeyContext, code, cancellationToken),
            "get" => await GetAsync(client, apiKeyContext, code, language, cancellationToken),
            "list" => await ListAsync(client, apiKeyContext, search, page, pageSize, language, cancellationToken),
            _ => McpProtoHelper.FormatError($"Unknown action '{action}'. Use: create, update, upsert, delete, get, or list"),
        };
    }

    private static async Task<string> ManageAsync(
        ProductManagementService.ProductManagementServiceClient client,
        IApiKeyContext ctx, string action, string? json, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(json))
        {
            return McpProtoHelper.FormatError("json is required for create/update/upsert");
        }

        var data = McpProtoHelper.Parser.Parse<ProductData>(json);
        var request = new ManageProductRequest { Data = data };

        var response = action.ToLowerInvariant() switch
        {
            "create" => await client.CreateAsync(request, cancellationToken: ct),
            "update" => await client.UpdateAsync(request, cancellationToken: ct),
            _ => await client.UpsertAsync(request, cancellationToken: ct),
        };

        return McpProtoHelper.FormatManageResponse(response.Success, response.Action, response.Code, response.Errors);
    }

    private static async Task<string> DeleteAsync(
        ProductManagementService.ProductManagementServiceClient client,
        IApiKeyContext ctx, string? code, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Write);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for delete");
        }

        var response = await client.DeleteAsync(new DeleteProductRequest { Code = code }, cancellationToken: ct);
        return McpProtoHelper.FormatManageResponse(response.Success, response.Action, response.Code, response.Errors);
    }

    private static async Task<string> GetAsync(
        ProductManagementService.ProductManagementServiceClient client,
        IApiKeyContext ctx, string? code, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        if (string.IsNullOrWhiteSpace(code))
        {
            return McpProtoHelper.FormatError("code is required for get");
        }

        var response = await client.GetAsync(
            new GetProductRequest { Code = code, Language = language ?? "" }, cancellationToken: ct);
        return McpProtoHelper.FormatGetResponse(response.Found, response.Data);
    }

    private static async Task<string> ListAsync(
        ProductManagementService.ProductManagementServiceClient client,
        IApiKeyContext ctx, string? search, int? page, int? pageSize, string? language, CancellationToken ct)
    {
        ctx.EnsurePermission(McpPermissions.Read);
        var response = await client.ListAsync(new ListProductsRequest
        {
            Search = search ?? "",
            Page = page ?? 1,
            PageSize = pageSize ?? 20,
            Language = language ?? "",
        }, cancellationToken: ct);

        return McpProtoHelper.FormatListResponse(response.Items, response.TotalCount, response.Page, response.PageSize);
    }
}
