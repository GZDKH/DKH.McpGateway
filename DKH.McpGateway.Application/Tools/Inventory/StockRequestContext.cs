using System.Globalization;
using System.Security.Claims;
using System.Text;
using DKH.ApiManagementService.Contracts.ApiManagement.Models.ApiKey.v1;
using DKH.Platform.Identity;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Inventory;

/// <summary>Preserves the admitted HTTP caller across an exact stock operation.</summary>
internal sealed class StockRequestContext : IDisposable
{
    private readonly IApiKeyContext _apiKey;
    private readonly IHttpContextAccessor _accessor;
    private readonly HttpContext _http;
    private readonly ClaimsPrincipal _principal;
    private readonly HttpCurrentUser _currentUser;
    private readonly byte[] _claims;
    private readonly string _bearer;
    private readonly string _workspaceHeader;
    private readonly string? _apiKeyId;
    private readonly string[] _permissions;
    private readonly string _permission;
    private readonly CancellationTokenSource _cancellation;

    private StockRequestContext(IApiKeyContext apiKey, IHttpContextAccessor accessor, string permission, CancellationToken cancellationToken)
    {
        _apiKey = apiKey;
        _accessor = accessor;
        _permission = permission;
        apiKey.EnsurePermission(permission);
        _http = accessor.HttpContext ?? throw Denied();
        _principal = _http.User;
        _currentUser = new HttpCurrentUser(accessor);
        if (!apiKey.IsAuthenticated || apiKey.Scope != ApiKeyScope.Mcp || _principal.Identity?.IsAuthenticated != true)
        {
            throw Denied();
        }

        RequireHumanIdentity();

        _bearer = SingleHeader(_http, "Authorization");
        if (!_bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(_bearer[7..]))
        {
            throw Denied();
        }

        _workspaceHeader = SingleHeader(_http, "X-Workspace-Id");
        WorkspaceId = StockInput.RequiredGuid(_workspaceHeader, "X-Workspace-Id");
        var expiries = _principal.FindAll("exp").ToArray();
        if (expiries.Length != 1 || !long.TryParse(expiries[0].Value, CultureInfo.InvariantCulture, out var expiry)
            || expiry <= DateTimeOffset.UtcNow.ToUnixTimeSeconds() || expiry > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
        {
            throw Denied();
        }

        Deadline = DateTimeOffset.FromUnixTimeSeconds(expiry).UtcDateTime;
        _claims = Snapshot(_principal);
        _apiKeyId = apiKey.ApiKeyId;
        _permissions = [.. apiKey.Permissions];
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _http.RequestAborted);
        try
        {
            RequireCurrent();
        }
        catch
        {
            _cancellation.Dispose();
            throw;
        }
    }

    internal Guid WorkspaceId { get; }

    internal DateTime Deadline { get; }

    internal CancellationToken CancellationToken => _cancellation.Token;

    internal Metadata Headers => new() { { "x-workspace-id", WorkspaceId.ToString("D") } };

    internal static StockRequestContext Capture(IApiKeyContext apiKey, IHttpContextAccessor accessor, string permission, CancellationToken cancellationToken)
        => new(apiKey, accessor, permission, cancellationToken);

    internal async Task<T> InvokeAsync<T>(Func<Metadata, DateTime, CancellationToken, AsyncUnaryCall<T>> invoke)
    {
        RequireCurrent();
        try
        {
            using var call = invoke(Headers, Deadline, CancellationToken);
            return await call.ResponseAsync;
        }
        finally
        {
            // The native call, including its disposal/error path, has ended.
            RequireCurrent();
        }
    }

    internal void RequireCurrent()
    {
        _cancellation.Token.ThrowIfCancellationRequested();
        _apiKey.EnsurePermission(_permission);
        if (!ReferenceEquals(_accessor.HttpContext, _http) || !ReferenceEquals(_http.User, _principal)
            || !_apiKey.IsAuthenticated || _apiKey.Scope != ApiKeyScope.Mcp || _apiKey.ApiKeyId != _apiKeyId
            || !_apiKey.Permissions.SequenceEqual(_permissions, StringComparer.Ordinal)
            || SingleHeader(_http, "Authorization") != _bearer || SingleHeader(_http, "X-Workspace-Id") != _workspaceHeader
            || !_claims.AsSpan().SequenceEqual(Snapshot(_principal)) || DateTime.UtcNow >= Deadline)
        {
            throw Denied();
        }

        RequireHumanIdentity();
    }

    private void RequireHumanIdentity()
    {
        // Reuse Platform's current identity view and Inventory's accepted named
        // HUMAN discriminator. Identity shape is not a stock-owner permission.
        var claims = _principal.Identities.Where(identity => identity.IsAuthenticated).SelectMany(identity => identity.Claims).ToArray();
        var subjects = claims.Where(claim => claim.Type == "sub").ToArray();
        var issuers = claims.Where(claim => claim.Type == "iss").ToArray();
        var names = claims.Where(claim => claim.Type == "preferred_username").ToArray();
        if (!_currentUser.IsAuthenticated || subjects.Length != 1 || !Guid.TryParse(subjects[0].Value, out var subject) || subject == Guid.Empty
            || _currentUser.UserId != subject || issuers.Length != 1 || string.IsNullOrWhiteSpace(issuers[0].Value)
            || _currentUser.GetClaim("iss") != issuers[0].Value || names.Length != 1 || string.IsNullOrWhiteSpace(names[0].Value)
            || _currentUser.GetClaim("preferred_username") != names[0].Value
            || names[0].Value.StartsWith("service-account-", StringComparison.Ordinal))
        {
            throw Denied();
        }
    }

    public void Dispose() => _cancellation.Dispose();

    private static string SingleHeader(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        return values.Count == 1 && !string.IsNullOrWhiteSpace(values[0]) ? values[0]! : throw Denied();
    }

    private static byte[] Snapshot(ClaimsPrincipal principal)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        principal.WriteTo(writer);
        writer.Flush();
        return stream.ToArray();
    }

    private static UnauthorizedAccessException Denied() => new("Exact stock tools require the unchanged admitted named HUMAN HTTP bearer, principal and selected Workspace.");
}
