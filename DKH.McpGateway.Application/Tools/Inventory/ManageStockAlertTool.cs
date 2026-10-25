namespace DKH.McpGateway.Application.Tools.Inventory;

[McpServerToolType]
public static class ManageStockAlertTool
{
    [McpServerTool(Name = "manage_stock_alert"), Description(
        "Retired legacy Inventory surface. These actions have no faithful exact-owned-stock replacement. " +
        "No stock adjustment, page or alert RPC is performed. Use query_stock availability with exact stock scope; " +
        "reservation mutations require manage_reservation genuine Cart/checkout originals and native CAS.")]
    public static Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        [Description("Retired action: list, configure, acknowledge")] string action,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        apiKeyContext.EnsurePermission(action.ToLowerInvariant() is "configure" or "acknowledge" ? McpPermissions.Write : McpPermissions.Read);
        return Task.FromResult(McpProtoHelper.FormatError(StockInput.RetiredMessage));
    }
}
