using DKH.McpGateway.Application.Resources;
using DKH.Platform.Grpc.Common.Types;
using SfBranding = DKH.StorefrontService.Contracts.Storefront.Api.StorefrontBrandingManagement.v1;
using SfCrud = DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using SfFeatures = DKH.StorefrontService.Contracts.Storefront.Api.StorefrontFeaturesManagement.v1;
using SfModels = DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;

namespace DKH.McpGateway.Tests.Resources;

public class StorefrontResourcesTests
{
    [Fact]
    public async Task GetStorefronts_Success_ReturnsJsonWithStorefrontsAsync()
    {
        var client = Substitute.For<SfCrud.StorefrontsCrudService.StorefrontsCrudServiceClient>();
        var response = new SfCrud.GetAllStorefrontsResponse
        {
            Pagination = new() { TotalCount = 0 },
        };
        client.GetAllAsync(
                Arg.Any<SfCrud.GetAllStorefrontsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var result = await StorefrontResources.GetStorefrontsAsync(client);

        var json = JsonDocument.Parse(result).RootElement;
        json.TryGetProperty("storefronts", out _).Should().BeTrue();
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetStorefronts_PassesPaginationAsync()
    {
        var client = Substitute.For<SfCrud.StorefrontsCrudService.StorefrontsCrudServiceClient>();
        client.GetAllAsync(
                Arg.Any<SfCrud.GetAllStorefrontsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new SfCrud.GetAllStorefrontsResponse
            {
                Pagination = new() { TotalCount = 0 },
            }));

        await StorefrontResources.GetStorefrontsAsync(client);

        _ = client.Received(1).GetAllAsync(
            Arg.Is<SfCrud.GetAllStorefrontsRequest>(r =>
                r.Pagination.Page == 1 && r.Pagination.PageSize == 50),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStorefrontConfig_Success_ReturnsCompositeJsonAsync()
    {
        var crudClient = Substitute.For<SfCrud.StorefrontsCrudService.StorefrontsCrudServiceClient>();
        var brandingClient = Substitute.For<SfBranding.StorefrontBrandingManagementService.StorefrontBrandingManagementServiceClient>();
        var featuresClient = Substitute.For<SfFeatures.StorefrontFeaturesManagementService.StorefrontFeaturesManagementServiceClient>();

        var storefront = new SfModels.StorefrontModel
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Code = "main",
            Name = "Main Store",
            Status = SfModels.StorefrontStatus.Active,
        };
        crudClient.GetByCodeAsync(
                Arg.Any<SfCrud.GetStorefrontByCodeRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(
                new SfCrud.GetStorefrontByCodeResponse { Storefront = storefront }));

        brandingClient.GetBrandingAsync(
                Arg.Any<SfBranding.GetBrandingRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(
                new SfBranding.GetBrandingResponse()));

        featuresClient.GetFeaturesAsync(
                Arg.Any<SfFeatures.GetFeaturesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(
                new SfFeatures.GetFeaturesResponse()));

        var result = await StorefrontResources.GetStorefrontConfigAsync(
            crudClient, brandingClient, featuresClient, storefrontCode: "main");

        var json = JsonDocument.Parse(result).RootElement;
        json.GetProperty("code").GetString().Should().Be("main");
        json.GetProperty("name").GetString().Should().Be("Main Store");
    }

    [Fact]
    public async Task GetStorefronts_GrpcFailure_PropagatesAsync()
    {
        var client = Substitute.For<SfCrud.StorefrontsCrudService.StorefrontsCrudServiceClient>();
        client.GetAllAsync(
                Arg.Any<SfCrud.GetAllStorefrontsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<SfCrud.GetAllStorefrontsResponse>(
                StatusCode.Unavailable));

        var act = () => StorefrontResources.GetStorefrontsAsync(client);

        await act.Should().ThrowAsync<RpcException>();
    }
}
