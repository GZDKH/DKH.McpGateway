namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class ManageStockTool
{
    [McpServerTool(Name = "manage_stock"), Description(
        "Retired legacy Inventory surface. These actions have no faithful exact-owned-stock replacement. " +
        "No stock adjustment, page or alert RPC is performed. Use query_stock availability with exact stock scope; " +
        "reservation mutations require manage_reservation genuine Cart/checkout originals and native CAS.")]
    public static Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        [Description("Retired action: set_level, adjust, get_warehouse_stock")] string action,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        apiKeyContext.EnsurePermission(action.ToLowerInvariant() is "set_level" or "adjust" ? McpPermissions.Write : McpPermissions.Read);
        return Task.FromResult(McpProtoHelper.FormatError(StockInput.RetiredMessage));
    }
}
