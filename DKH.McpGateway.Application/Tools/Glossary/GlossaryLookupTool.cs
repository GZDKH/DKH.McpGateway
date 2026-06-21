using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.CategoryManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.SpecAttributeManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.TagManagement.v1;

namespace DKH.McpGateway.Application.Tools.Glossary;

/// <summary>
/// MCP tool that resolves a free-text term to a canonical catalog term.
/// The catalog "glossary" is composed of tags, specification attributes and categories;
/// this tool searches across them and returns the best canonical match plus all candidates.
/// </summary>
[McpServerToolType]
public static class GlossaryLookupTool
{
    [McpServerTool(Name = "glossary_lookup"), Description(
        "Resolve a free-text term to a canonical catalog term. Searches tags, specification attributes and " +
        "categories and returns the best canonical match plus all candidates grouped by type. " +
        "Use the 'type' parameter to restrict the lookup. Supports multilingual lookup (lang).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        TagManagementService.TagManagementServiceClient tagClient,
        SpecAttributeManagementService.SpecAttributeManagementServiceClient specClient,
        CategoryManagementService.CategoryManagementServiceClient categoryClient,
        [Description("Term to resolve (e.g. 'oolong', 'caffeine')")] string term,
        [Description("Restrict lookup: 'all', 'tag', 'specification', or 'category'")] string type = "all",
        [Description("Language code (e.g. 'en', 'ru')")] string lang = "ru",
        [Description("Catalog SEO name (used for category lookup)")] string catalogSeoName = "main-catalog",
        [Description("Maximum candidates per type (max 50)")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(term))
        {
            return McpProtoHelper.FormatError("term is required");
        }

        limit = Math.Clamp(limit, 1, 50);
        var normalizedType = type.ToLowerInvariant();
        var includeAll = normalizedType is "all" or "";

        var tags = includeAll || normalizedType == "tag"
            ? await LookupTagsAsync(tagClient, term, lang, limit, cancellationToken)
            : new List<GlossaryMatch>();

        var specifications = includeAll || normalizedType == "specification"
            ? await LookupSpecificationsAsync(specClient, term, lang, limit, cancellationToken)
            : new List<GlossaryMatch>();

        var categories = includeAll || normalizedType == "category"
            ? await LookupCategoriesAsync(categoryClient, term, lang, catalogSeoName, limit, cancellationToken)
            : new List<GlossaryMatch>();

        var canonical = ResolveCanonical(term, tags, specifications, categories);

        var result = new
        {
            term,
            lang,
            canonical = canonical is null ? null : Project(canonical),
            matches = new
            {
                tags = tags.Select(Project),
                specifications = specifications.Select(Project),
                categories = categories.Select(Project),
            },
        };

        return JsonSerializer.Serialize(result, McpJsonDefaults.Options);
    }

    private static object Project(GlossaryMatch match) => new
    {
        type = match.Type,
        name = match.Name,
        code = match.Code,
    };

    private static async Task<List<GlossaryMatch>> LookupTagsAsync(
        TagManagementService.TagManagementServiceClient client,
        string term, string lang, int limit, CancellationToken ct)
    {
        var response = await client.ListAsync(
            new ListTagsRequest { Search = term, Page = 1, PageSize = limit, Language = lang },
            cancellationToken: ct);

        return response.Items.Select(item => ToMatch("tag", item)).ToList();
    }

    private static async Task<List<GlossaryMatch>> LookupSpecificationsAsync(
        SpecAttributeManagementService.SpecAttributeManagementServiceClient client,
        string term, string lang, int limit, CancellationToken ct)
    {
        var response = await client.ListAsync(
            new ListSpecAttributesRequest { Search = term, Page = 1, PageSize = limit, Language = lang },
            cancellationToken: ct);

        return response.Items.Select(item => ToMatch("specification", item)).ToList();
    }

    private static async Task<List<GlossaryMatch>> LookupCategoriesAsync(
        CategoryManagementService.CategoryManagementServiceClient client,
        string term, string lang, string catalogSeoName, int limit, CancellationToken ct)
    {
        var response = await client.GetCategoryTreeAsync(
            new GetCategoryTreeRequest { CatalogSeoName = catalogSeoName, LanguageCode = lang },
            cancellationToken: ct);

        var matches = new List<GlossaryMatch>();
        foreach (var node in response.RootCategories)
        {
            CollectCategoryMatches(node, term, matches);
        }

        return matches.Take(limit).ToList();
    }

    private static void CollectCategoryMatches(CategoryNode node, string term, List<GlossaryMatch> matches)
    {
        if (node.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(new GlossaryMatch("category", node.Name, node.SeoName));
        }

        foreach (var child in node.Children)
        {
            CollectCategoryMatches(child, term, matches);
        }
    }

    /// <summary>
    /// Converts a protobuf list item to a glossary match by reading its JSON projection,
    /// which avoids a compile-time dependency on the concrete contract message shape.
    /// </summary>
    private static GlossaryMatch ToMatch(string type, Google.Protobuf.IMessage item)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(McpProtoHelper.Formatter.Format(item));
        var name = ReadString(element, "name") ?? ReadString(element, "code") ?? string.Empty;
        var code = ReadString(element, "code") ?? ReadString(element, "seoName");
        return new GlossaryMatch(type, name, code);
    }

    private static string? ReadString(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(propertyName, out var value)
           && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static GlossaryMatch? ResolveCanonical(
        string term,
        IReadOnlyList<GlossaryMatch> tags,
        IReadOnlyList<GlossaryMatch> specifications,
        IReadOnlyList<GlossaryMatch> categories)
    {
        var ordered = tags.Concat(specifications).Concat(categories).ToList();
        if (ordered.Count == 0)
        {
            return null;
        }

        return ordered.FirstOrDefault(m => string.Equals(m.Name, term, StringComparison.OrdinalIgnoreCase))
               ?? ordered[0];
    }

    private sealed record GlossaryMatch(string Type, string Name, string? Code);
}
