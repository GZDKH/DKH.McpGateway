using System.Diagnostics;
using DKH.ApiManagementService.Contracts.Services.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DKH.McpGateway.Application.Auth;

public sealed class ApiKeyUsageRecorder(
    RequestDelegate next,
    ApiKeyUsageService.ApiKeyUsageServiceClient usageClient,
    ILogger<ApiKeyUsageRecorder> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        if (context.Items.TryGetValue("ApiKeyId", out var apiKeyIdObj) &&
            apiKeyIdObj is string apiKeyId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await usageClient.RecordUsageAsync(new RecordUsageRequest
                    {
                        ApiKeyId = apiKeyId,
                        Endpoint = context.Request.Path.ToString(),
                        StatusCode = context.Response.StatusCode,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "",
                        UserAgent = context.Request.Headers.UserAgent.ToString(),
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to record API key usage for {ApiKeyId}", apiKeyId);
                }
            });
        }
    }
}
