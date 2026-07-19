using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;

using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class ProductStatsTool
{
    [McpServerTool(Name = "product_stats"), Description("Get product catalog statistics: total count and top categories.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor,
        ProductManagementService.ProductManagementServiceClient searchClient,
        CategoryManagementService.CategoryManagementServiceClient categoryClient,
        [Description("Catalog SEO name")] string catalogSeoName = "main-catalog",
        [Description("Language code")] string languageCode = "ru",
        CancellationToken cancellationToken = default)
    {
        // #85: bind the lookup to the selected, server-validated Workspace
        // BEFORE any downstream RPC — reuses the #83 resolver; ProductCatalog
        // independently verifies the propagated caller's membership.
        var workspaceMetadata = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            apiKeyContext, httpContextAccessor);
        apiKeyContext.EnsurePermission(McpPermissions.Read);
        var productsTask = searchClient.SearchProductsAsync(
            new SearchProductsRequest
            {
                CatalogSeoName = catalogSeoName,
                LanguageCode = languageCode,
                SearchTerm = "",
                Page = 1,
                PageSize = 1,
            },
            headers: workspaceMetadata, cancellationToken: cancellationToken).ResponseAsync;

        var categoriesTask = categoryClient.GetCategoryTreeAsync(
            new GetCategoryTreeRequest { CatalogSeoName = catalogSeoName, LanguageCode = languageCode, MaxDepth = 1 },
            headers: workspaceMetadata, cancellationToken: cancellationToken).ResponseAsync;

        await Task.WhenAll(productsTask, categoriesTask);

        var categories = categoriesTask.Result.RootCategories;

        var result = new
        {
            totalProducts = productsTask.Result.TotalCount,
            totalCategories = categories.Count,
            topCategories = categories
                .OrderByDescending(c => c.ProductCount)
                .Take(10)
                .Select(static c => new { name = c.Name, seoName = c.SeoName, productCount = c.ProductCount }),
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }
}
