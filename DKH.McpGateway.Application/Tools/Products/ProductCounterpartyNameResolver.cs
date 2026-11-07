using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;

namespace DKH.McpGateway.Application.Tools.Products;

/// <summary>
/// ADR-020 (brand/manufacturer removal) — resolves Brand and Manufacturer display
/// names from product counterparty links via a single
/// <c>BatchGetCounterpartyBasics</c> call. Replaces the legacy
/// <c>product.Brand</c> / <c>product.Manufacturer</c> proto fields, which McpGateway
/// no longer reads. Picks the link with <c>role=="Brand"</c> / <c>"Manufacturer"</c>
/// and the lowest <c>display_order</c> (0 = primary); Supplier/Distributor links are
/// intentionally not resolved here.
/// </summary>
internal static class ProductCounterpartyNameResolver
{
    public static async Task<(string? BrandName, string? ManufacturerName)> ResolveAsync(
        IEnumerable<(string Role, int DisplayOrder, string? Id)> counterparties,
        CounterpartyCrudService.CounterpartyCrudServiceClient client,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var links = counterparties
            .Where(static c => c.Role is "Brand" or "Manufacturer")
            .ToList();

        if (links.Count == 0)
        {
            return (null, null);
        }

        var ids = links
            .Select(static c => c.Id)
            .Where(static v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return (null, null);
        }

        var request = new BatchGetCounterpartyBasicsRequest();
        request.Ids.AddRange(ids.Select(static id => new GuidValue(id!)));

        var response = await client.BatchGetCounterpartyBasicsAsync(request, cancellationToken: cancellationToken);

        var names = response.Basics.ToDictionary(
            static b => b.Id.Value,
            b => b.DisplayName?.Values.TryGetValue(languageCode, out var name) == true
                ? name
                : b.DisplayName?.Values.Values.FirstOrDefault() ?? string.Empty);

        return (ResolveName(links, "Brand", names), ResolveName(links, "Manufacturer", names));
    }

    private static string? ResolveName(
        IReadOnlyCollection<(string Role, int DisplayOrder, string? Id)> links,
        string role,
        Dictionary<string, string> names)
    {
        var id = links
            .Where(c => c.Role == role)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => c.Id)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return names.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name) ? name : null;
    }
}
