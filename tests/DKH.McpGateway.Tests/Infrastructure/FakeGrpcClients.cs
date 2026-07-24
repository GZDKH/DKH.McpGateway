using DKH.ApiManagementService.Contracts.ApiManagement.Api.ApiKeyQuery.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCatalogManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;

namespace DKH.McpGateway.Tests.Infrastructure;

/// <summary>
/// Thread-safe fake API-key validation client: resolves each raw key through the supplied delegate.
/// </summary>
internal sealed class FakeApiKeyQueryClient(Func<ValidateApiKeyRequest, ValidateApiKeyResponse> resolve)
    : ApiKeyQueryService.ApiKeyQueryServiceClient
{
    public override AsyncUnaryCall<ValidateApiKeyResponse> ValidateApiKeyAsync(
        ValidateApiKeyRequest request, CallOptions options)
        => GrpcTestHelpers.CreateAsyncUnaryCall(resolve(request));
}

/// <summary>Thread-safe storefront features client returning the configured <c>McpEnabled</c> value.</summary>
internal sealed class FakeStorefrontFeaturesClient(Func<bool> mcpEnabled)
    : StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient
{
    public override AsyncUnaryCall<GetFeaturesResponse> GetFeaturesAsync(
        GetFeaturesRequest request, CallOptions options)
        => GrpcTestHelpers.CreateAsyncUnaryCall(new GetFeaturesResponse
        {
            Features = new StorefrontFeaturesModel { McpEnabled = mcpEnabled() },
        });
}

/// <summary>Thread-safe storefront catalogs client returning an empty catalog set.</summary>
internal sealed class FakeStorefrontCatalogsClient
    : StorefrontCatalogManagementService.StorefrontCatalogManagementServiceClient
{
    public override AsyncUnaryCall<GetCatalogsResponse> GetCatalogsAsync(
        GetCatalogsRequest request, CallOptions options)
        => GrpcTestHelpers.CreateAsyncUnaryCall(new GetCatalogsResponse());
}
