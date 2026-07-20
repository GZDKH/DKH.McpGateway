using DKH.McpGateway.Application.Tools.DataExchange;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace DKH.McpGateway.Application.Tools.Storefronts;

/// <summary>
/// Binds storefront administration calls to the Workspace selected by the
/// authenticated MCP request. The StorefrontService model remains the source
/// of truth for ownership; foreign and missing resources deliberately produce
/// the same neutral result.
/// </summary>
internal sealed record StorefrontWorkspaceScope(Guid WorkspaceId, Metadata Headers)
{
    private const string NotFoundMessage = "Storefront was not found in the selected Workspace.";

    internal static StorefrontWorkspaceScope Resolve(
        IApiKeyContext apiKeyContext,
        IHttpContextAccessor httpContextAccessor)
    {
        var headers = ProductCatalogWorkspaceRequestContext.CreateRequiredGrpcMetadata(
            apiKeyContext,
            httpContextAccessor);
        var workspaceValue = headers.Single(entry =>
            entry.Key == ProductCatalogWorkspaceRequestContext.GrpcWorkspaceIdHeaderName).Value;

        return new StorefrontWorkspaceScope(Guid.Parse(workspaceValue), headers);
    }

    internal async Task<StorefrontModel> GetByCodeAsync(
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        string storefrontCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetByCodeAsync(
                new GetStorefrontByCodeRequest { Code = storefrontCode },
                Headers,
                cancellationToken: cancellationToken);

            return RequireOwned(response.Storefront);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            throw CreateNotFoundException();
        }
    }

    internal async Task<GetAllStorefrontsResponse> GetAllAsync(
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAllAsync(
            new GetAllStorefrontsRequest
            {
                Pagination = new PaginationRequest { Page = page, PageSize = pageSize },
                OwnerId = new GuidValue(WorkspaceId.ToString("D")),
            },
            Headers,
            cancellationToken: cancellationToken);

        if (response.Storefronts.Any(storefront =>
                storefront.WorkspaceId is null
                || !Guid.TryParse(storefront.WorkspaceId.Value, out var workspaceId)
                || workspaceId != WorkspaceId))
        {
            throw new RpcException(new Status(
                StatusCode.PermissionDenied,
                "Storefront list contained data outside the selected Workspace."));
        }

        return response;
    }

    internal async Task<StorefrontModel> GetByIdAsync(
        StorefrontsCrudService.StorefrontsCrudServiceClient client,
        string storefrontId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(storefrontId, out var parsedId) || parsedId == Guid.Empty)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Storefront ID must be a non-empty GUID."));
        }

        try
        {
            var response = await client.GetAsync(
                new GetStorefrontRequest { Id = new GuidValue(parsedId.ToString("D")) },
                Headers,
                cancellationToken: cancellationToken);

            return RequireOwned(response.Storefront);
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            throw CreateNotFoundException();
        }
    }

    private StorefrontModel RequireOwned(StorefrontModel? storefront)
    {
        if (storefront?.WorkspaceId is null
            || !Guid.TryParse(storefront.WorkspaceId.Value, out var storefrontWorkspaceId)
            || storefrontWorkspaceId != WorkspaceId)
        {
            throw CreateNotFoundException();
        }

        return storefront;
    }

    private static RpcException CreateNotFoundException() => new(new Status(StatusCode.NotFound, NotFoundMessage));
}
