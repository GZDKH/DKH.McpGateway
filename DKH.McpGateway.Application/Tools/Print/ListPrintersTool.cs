using DKH.PrintService.Contracts.Models.V1;
using DKH.PrintService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Print;

[McpServerToolType]
public static class ListPrintersTool
{
    [McpServerTool(Name = "list_printers"), Description(
        "List registered printers, optionally filtered to a single location. Returns each " +
        "printer's ID, name, type (Thermal/Label/Office), connection string, location, and " +
        "status (Active/Disabled/Offline).")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Prints.PrintsClient client,
        [Description("Location ID (GUID) to filter by; omit for all locations")] string? locationId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListPrintersRequest();
        if (!string.IsNullOrWhiteSpace(locationId))
        {
            request.LocationId = new GuidValue(locationId);
        }

        var response = await client.ListPrintersAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(
            new { printers = response.Printers.Select(MapPrinter) },
            McpJsonDefaults.Options);
    }

    // PrinterModel — GuidValue → .Value; strings direct; enums → .ToString().
    internal static object MapPrinter(PrinterModel p) => new
    {
        id = p.Id?.Value,
        name = p.Name,
        type = p.Type.ToString(),
        connectionString = p.ConnectionString,
        locationId = p.LocationId?.Value,
        status = p.Status.ToString(),
    };
}
