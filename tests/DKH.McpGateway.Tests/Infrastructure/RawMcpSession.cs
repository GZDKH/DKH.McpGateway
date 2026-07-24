using System.Net;
using System.Net.Http.Json;

namespace DKH.McpGateway.Tests.Infrastructure;

/// <summary>
/// Minimal raw JSON-RPC MCP client over HTTP. Each method is a single independent
/// request/response POST (Streamable HTTP), so no background SSE stream races with per-request
/// headers under the in-memory <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>. Handles both
/// <c>application/json</c> and <c>text/event-stream</c> response bodies.
/// </summary>
internal sealed class RawMcpSession(HttpClient httpClient, string? apiKey, string? bearer)
{
    private const string ProtocolVersion = "2025-06-18";
    private int _id;
    private string? _sessionId;

    internal async Task InitializeAsync()
    {
        _ = await RequestAsync("initialize", new
        {
            protocolVersion = ProtocolVersion,
            capabilities = new { },
            clientInfo = new { name = "raw-test-client", version = "1.0.0" },
        });

        using var notification = BuildRequest(new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized",
        });
        using var response = await httpClient.SendAsync(notification);
        EnsureAccepted(response.StatusCode);
    }

    internal async Task<IReadOnlyList<string>> ListToolNamesAsync()
        => await ListNamesAsync("tools/list", "tools");

    internal async Task<IReadOnlyList<string>> ListPromptNamesAsync()
        => await ListNamesAsync("prompts/list", "prompts");

    internal async Task<IReadOnlyList<string>> ListResourceNamesAsync()
        => await ListNamesAsync("resources/list", "resources");

    internal async Task<ToolCallOutcome> CallToolAsync(
        string name, IReadOnlyDictionary<string, object?>? arguments = null)
    {
        var result = await RequestAsync("tools/call", new
        {
            name,
            arguments = arguments ?? new Dictionary<string, object?>(),
        });

        var isError = result.TryGetProperty("isError", out var flag)
            && flag.ValueKind == JsonValueKind.True;

        var text = string.Empty;
        if (result.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            text = string.Join(
                "\n",
                content.EnumerateArray()
                    .Where(block => block.TryGetProperty("text", out _))
                    .Select(block => block.GetProperty("text").GetString()));
        }

        return new ToolCallOutcome(isError, text);
    }

    private async Task<IReadOnlyList<string>> ListNamesAsync(string method, string arrayProperty)
    {
        var result = await RequestAsync(method, parameters: null);
        if (!result.TryGetProperty(arrayProperty, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. array.EnumerateArray().Select(element => element.GetProperty("name").GetString()!)];
    }

    private async Task<JsonElement> RequestAsync(string method, object? parameters)
    {
        var payload = parameters is null
            ? (object)new { jsonrpc = "2.0", id = ++_id, method }
            : new { jsonrpc = "2.0", id = ++_id, method, @params = parameters };

        using var request = BuildRequest(payload);
        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new McpHttpRejectedException(response.StatusCode);
        }

        if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessionValues))
        {
            _sessionId = sessionValues.FirstOrDefault() ?? _sessionId;
        }

        var body = await response.Content.ReadAsStringAsync();
        var message = ParseJsonRpc(body, response.Content.Headers.ContentType?.MediaType);

        if (message.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("message", out var m) ? m.GetString() : "unknown";
            throw new InvalidOperationException($"JSON-RPC error from {method}: {errorMessage}");
        }

        // Clone so the element outlives the parsed document.
        return message.GetProperty("result").Clone();
    }

    private HttpRequestMessage BuildRequest(object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
        request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", ProtocolVersion);
        if (apiKey is not null)
        {
            request.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
        }

        if (bearer is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", bearer);
        }

        if (_sessionId is not null)
        {
            request.Headers.TryAddWithoutValidation("Mcp-Session-Id", _sessionId);
        }

        return request;
    }

    private static JsonElement ParseJsonRpc(string body, string? mediaType)
    {
        var json = mediaType == "text/event-stream" ? ExtractEventStreamData(body) : body;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string ExtractEventStreamData(string body)
    {
        // Take the first "data:" payload of the SSE response — a single request yields one message.
        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            if (trimmed.StartsWith("data:", StringComparison.Ordinal))
            {
                return trimmed["data:".Length..].Trim();
            }
        }

        return body;
    }

    private static void EnsureAccepted(HttpStatusCode statusCode)
    {
        if (statusCode is not (HttpStatusCode.OK or HttpStatusCode.Accepted or HttpStatusCode.NoContent))
        {
            throw new McpHttpRejectedException(statusCode);
        }
    }
}

/// <summary>Result of a raw <c>tools/call</c>: whether it was an error and the returned text.</summary>
internal readonly record struct ToolCallOutcome(bool IsError, string Text);

/// <summary>Raised when the HTTP MCP endpoint rejects a request (e.g. ingress denies the credential).</summary>
internal sealed class McpHttpRejectedException(HttpStatusCode statusCode)
    : Exception($"MCP HTTP request rejected with status {(int)statusCode} {statusCode}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
