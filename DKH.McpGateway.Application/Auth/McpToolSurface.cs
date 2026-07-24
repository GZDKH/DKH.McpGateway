using System.Reflection;

namespace DKH.McpGateway.Application.Auth;

/// <summary>
/// Classifies every registered MCP tool into the <b>public storefront surface</b> or the
/// <b>admin surface</b>, so ingress authentication can be split from per-surface authorization
/// (issue #86). A tool is public only when its declaring type carries
/// <see cref="StorefrontPublicToolAttribute"/>; every other tool is admin (default-closed).
/// </summary>
/// <remarks>
/// The map is built once by reflecting the tool assembly, mirroring the SDK's own
/// <c>WithToolsFromAssembly</c> discovery, so it can never drift from name conventions —
/// classification is by attribute, never by the <c>storefront_</c> name prefix.
/// </remarks>
public sealed class McpToolSurface
{
    private readonly HashSet<string> _publicToolNames;

    /// <summary>Builds the surface map from the application tool assembly.</summary>
    public McpToolSurface()
        : this(typeof(McpToolSurface).Assembly)
    {
    }

    internal McpToolSurface(Assembly toolAssembly)
    {
        ArgumentNullException.ThrowIfNull(toolAssembly);

        _publicToolNames = toolAssembly
            .GetTypes()
            .Where(static type => type.GetCustomAttribute<StorefrontPublicToolAttribute>() is not null)
            .SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            .Select(static method => method.GetCustomAttribute<McpServerToolAttribute>())
            .Where(static attribute => attribute is not null)
            .Select(static attribute => attribute!.Name)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name!)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The tool names exposed to storefront-scoped API keys.</summary>
    public IReadOnlyCollection<string> StorefrontPublicToolNames => _publicToolNames;

    /// <summary>
    /// <see langword="true"/> when the named tool belongs to the public storefront surface and may
    /// be discovered/invoked with a storefront-scoped key; <see langword="false"/> for every admin
    /// tool and every unknown name (default-closed).
    /// </summary>
    public bool IsStorefrontPublic(string toolName)
        => !string.IsNullOrEmpty(toolName) && _publicToolNames.Contains(toolName);
}
