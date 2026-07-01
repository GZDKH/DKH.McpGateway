using DKH.PrintService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Print;

[McpServerToolType]
public static class RoutePrintJobTool
{
    [McpServerTool(Name = "route_print_job"), Description(
        "Queue a print job for routing to a printer. Requires a job type and an opaque payload " +
        "reference (a storage key, not payload content); routing hints are optional operational " +
        "metadata. Returns the persisted job in Queued status.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Prints.PrintsClient client,
        [Description("Job type / document kind (required)")] string jobType,
        [Description("Opaque payload storage reference (required)")] string payloadRef,
        [Description("Routing hints — operational metadata (optional)")] string? routingHints = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(jobType))
        {
            return McpProtoHelper.FormatError("jobType is required");
        }

        if (string.IsNullOrWhiteSpace(payloadRef))
        {
            return McpProtoHelper.FormatError("payloadRef is required");
        }

        var job = await client.RoutePrintJobAsync(
            new RoutePrintJobRequest
            {
                JobType = jobType,
                PayloadRef = payloadRef,
                RoutingHints = routingHints ?? "",
            },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(GetPrintJobTool.MapJob(job), McpJsonDefaults.Options);
    }
}
