using System.Security.Claims;
using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.Platform.Grpc.Common.Types;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Tests.Tools.Storefronts;

internal static class StorefrontWorkspaceTestContext
{
    internal static readonly Guid WorkspaceId = Guid.Parse("f3450806-164e-40c5-837f-4550cbd57efc");

    internal static readonly FixedHttpContextAccessor HttpContextAccessor = CreateAccessor(WorkspaceId);

    internal static GuidValue WorkspaceGuidValue => new(WorkspaceId.ToString("D"));

    internal static async Task AssertNeutralNotFoundAsync(Func<Task<string>> action)
    {
        var exception = await action.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Status.Detail.Should().Be("Storefront was not found in the selected Workspace.");
    }

    internal static FixedHttpContextAccessor CreateAccessor(Guid workspaceId)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "storefront-workspace-test-user")],
                authenticationType: "Test")),
        };
        context.Request.Headers[ProductCatalogWorkspaceRequestContext.WorkspaceIdHeaderName] =
            workspaceId.ToString("D");

        return new FixedHttpContextAccessor { HttpContext = context };
    }

    internal sealed class FixedHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
