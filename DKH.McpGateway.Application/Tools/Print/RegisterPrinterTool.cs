using DKH.PrintService.Contracts.Models.V1;
using DKH.PrintService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Print;

[McpServerToolType]
public static class RegisterPrinterTool
{
    [McpServerTool(Name = "register_printer"), Description(
        "Register a new printer in the print registry. Requires a display name and a connection " +
        "string; type defaults to Unspecified when not one of Thermal/Label/Office. Returns the " +
        "created printer.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Prints.PrintsClient client,
        [Description("Printer display name (required)")] string name,
        [Description("Connection string / device address (required)")] string connectionString,
        [Description("Printer type: Thermal, Label, or Office (optional)")] string? type = null,
        [Description("Location ID (GUID) the printer belongs to (optional)")] string? locationId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(name))
        {
            return McpProtoHelper.FormatError("name is required");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return McpProtoHelper.FormatError("connectionString is required");
        }

        var request = new RegisterPrinterRequest
        {
            Name = name,
            ConnectionString = connectionString,
            Type = Enum.TryParse<PrinterType>(type, ignoreCase: true, out var parsedType)
                ? parsedType
                : PrinterType.Unspecified,
        };

        if (!string.IsNullOrWhiteSpace(locationId))
        {
            request.LocationId = new GuidValue(locationId);
        }

        var printer = await client.RegisterPrinterAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(ListPrintersTool.MapPrinter(printer), McpJsonDefaults.Options);
    }
}
