using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductsCrud.v1;

namespace DKH.McpGateway.Application.Tools.Products;

[McpServerToolType]
public static class DeleteProductTool
{
    [McpServerTool(Name = "delete_product"), Description(
        "Delete a product from the catalog by its code. " +
        "Searches for the product by code, then deletes it.")]
    public static async Task<string> ExecuteAsync(
        ProductsCrudService.ProductsCrudServiceClient client,
        [Description("Product code (e.g. 'TEA-XIHU-LONGJING-SHIFENG')")] string productCode,
        CancellationToken cancellationToken = default)
    {
        var searchResponse = await client.GetProductsAsync(
            new GetProductsRequest
            {
                Search = productCode,
                Page = 1,
                PageSize = 10,
            },
            cancellationToken: cancellationToken);

        var product = searchResponse.Products.FirstOrDefault(p =>
            string.Equals(p.Code, productCode, StringComparison.OrdinalIgnoreCase));

        if (product is null)
        {
            return JsonSerializer.Serialize(
                new { success = false, error = $"Product with code '{productCode}' not found" },
                McpJsonDefaults.Options);
        }

        await client.DeleteProductAsync(
            new DeleteProductRequest { ProductId = product.Id },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            success = true,
            action = "deleted",
            deletedCode = product.Code,
        }, McpJsonDefaults.Options);
    }
}
