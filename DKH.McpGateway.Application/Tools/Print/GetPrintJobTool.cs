using DKH.PrintService.Contracts.Models.V1;
using DKH.PrintService.Contracts.Services.V1;

namespace DKH.McpGateway.Application.Tools.Print;

[McpServerToolType]
public static class GetPrintJobTool
{
    [McpServerTool(Name = "get_print_job"), Description(
        "Get a single print job by its ID. Returns job type, opaque payload reference, " +
        "routing hints, lifecycle status (Queued/Dispatched/Rendering/Sent/Confirmed/Failed), " +
        "and created/completed timestamps.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        Prints.PrintsClient client,
        [Description("Print job ID (GUID)")] string jobId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (string.IsNullOrWhiteSpace(jobId))
        {
            return McpProtoHelper.FormatError("jobId is required");
        }

        var job = await client.GetJobAsync(
            new GetJobRequest { Id = new GuidValue(jobId) },
            cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapJob(job), McpJsonDefaults.Options);
    }

    // PrintJobModel — GuidValue → .Value; strings direct; enum → .ToString(); Timestamp → "O".
    internal static object MapJob(PrintJobModel j) => new
    {
        id = j.Id?.Value,
        jobType = j.JobType,
        payloadRef = j.PayloadRef,
        routingHints = j.RoutingHints,
        status = j.Status.ToString(),
        createdAt = j.CreatedAt?.ToDateTimeOffset().ToString("O"),
        completedAt = j.CompletedAt?.ToDateTimeOffset().ToString("O"),
    };
}
