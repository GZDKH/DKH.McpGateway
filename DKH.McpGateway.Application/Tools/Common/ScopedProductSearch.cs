using DKH.SearchService.Contracts.Search.Api.ProductSearch.v1;
using DKH.SearchService.Contracts.Search.Models.ProductSearch.v1;

namespace DKH.McpGateway.Application.Tools.Common;

internal sealed record ProductSearchPage(int Found, int SearchTimeMs, IReadOnlyList<ProductSearchModel> Products);

/// <summary>Supported scoped lexical/semantic retrieval for actual MCP search callers.</summary>
internal static class ScopedProductSearch
{
    internal static async Task<ProductSearchPage> ExecuteAsync(
        ProductSearchService.ProductSearchServiceClient client,
        string query, string filterBy, int page, int pageSize, bool semanticSearch, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        if (semanticSearch)
        {
            if ((long)page * pageSize > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Semantic search supports at most 100 windowed results.");
            }

            var window = page * pageSize;
            var response = await client.HybridSearchAsync(new HybridSearchRequest
            {
                Query = query,
                FilterBy = filterBy,
                Mode = SearchMode.Semantic,
                Page = page,
                PerPage = pageSize,
                MaxResults = window,
                PerEngineTopK = Math.Max(50, window),
                PerEngineTimeoutMs = 350,
            }, cancellationToken: cancellationToken);
            return new ProductSearchPage(response.Found, response.SearchTimeMs,
                [.. response.Hits.Select(static hit => hit.Product)]);
        }

        var lexical = await client.SearchProductsAsync(new SearchProductsRequest
        {
            Query = query,
            FilterBy = filterBy,
            Page = page,
            PerPage = pageSize,
        }, cancellationToken: cancellationToken);
        return new ProductSearchPage(lexical.Found, lexical.SearchTimeMs,
            [.. lexical.Hits.Select(static hit => hit.Document)]);
    }
}
