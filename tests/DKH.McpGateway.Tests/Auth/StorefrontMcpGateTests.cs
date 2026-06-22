using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Features.v1;
using Microsoft.Extensions.Logging.Abstractions;

namespace DKH.McpGateway.Tests.Auth;

public class StorefrontMcpGateTests
{
    private readonly StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient _featuresClient =
        Substitute.For<StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient>();

    private readonly Guid _storefrontId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private StorefrontMcpGate CreateGate()
        => new(_featuresClient, NullLogger<StorefrontMcpGate>.Instance);

    private void SetupFeatures(GetFeaturesResponse response)
        => _featuresClient.GetFeaturesAsync(
                Arg.Any<GetFeaturesRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    [Fact]
    public async Task EnsureMcpEnabled_WhenEnabled_DoesNotThrowAsync()
    {
        SetupFeatures(new GetFeaturesResponse
        {
            Features = new StorefrontFeaturesModel { McpEnabled = true },
        });

        var act = async () => await CreateGate().EnsureMcpEnabledAsync(_storefrontId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureMcpEnabled_WhenDisabled_ThrowsPermissionDeniedAsync()
    {
        SetupFeatures(new GetFeaturesResponse
        {
            Features = new StorefrontFeaturesModel { McpEnabled = false },
        });

        var act = async () => await CreateGate().EnsureMcpEnabledAsync(_storefrontId);

        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task EnsureMcpEnabled_WhenFeaturesMissing_ThrowsPermissionDeniedAsync()
    {
        // A storefront with no features payload has not opted in → deny (fail-closed).
        SetupFeatures(new GetFeaturesResponse());

        var act = async () => await CreateGate().EnsureMcpEnabledAsync(_storefrontId);

        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task EnsureMcpEnabled_WhenFeaturesServiceUnavailable_PropagatesAsync()
    {
        _featuresClient.GetFeaturesAsync(
                Arg.Any<GetFeaturesRequest>(), Arg.Any<Metadata>(), Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetFeaturesResponse>(
                StatusCode.Unavailable, "features service down"));

        var act = async () => await CreateGate().EnsureMcpEnabledAsync(_storefrontId);

        // Fail-closed: a transport failure must surface, never be silently treated as "allowed".
        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should().Be(StatusCode.Unavailable);
    }
}
