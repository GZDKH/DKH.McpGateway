using DKH.McpGateway.Application.Tools.Storefronts;
using DKH.Platform.Grpc.Common.Types;
using DKH.StorefrontService.Contracts.Storefront.Api.StorefrontCrud.v1;
using DKH.StorefrontService.Contracts.Storefront.Models.Storefront.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Storefronts;

public class ListStorefrontsToolTests
{
    private readonly StorefrontsCrudService.StorefrontsCrudServiceClient _client =
        Substitute.For<StorefrontsCrudService.StorefrontsCrudServiceClient>();

    [Fact]
    public async Task List_HappyPath_ReturnsStorefrontsAsync()
    {
        var response = new GetAllStorefrontsResponse
        {
            Pagination = new PaginationMetadata
            {
                TotalCount = 1,
                CurrentPage = 1,
                PageSize = 20,
            },
        };
        response.Storefronts.Add(new StorefrontSummaryModel
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Code = "test-store",
            Name = "Test Store",
            Status = StorefrontStatus.Active,
            CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
        });
        SetupGetAll(response);

        var result = await ListStorefrontsTool.ExecuteAsync(_client);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(1);
        json.GetProperty("storefronts").GetArrayLength().Should().Be(1);
        json.GetProperty("storefronts")[0].GetProperty("code").GetString().Should().Be("test-store");
    }

    [Fact]
    public async Task List_EmptyResult_ReturnsEmptyArrayAsync()
    {
        SetupGetAll(new GetAllStorefrontsResponse
        {
            Pagination = new PaginationMetadata
            {
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 20,
            },
        });

        var result = await ListStorefrontsTool.ExecuteAsync(_client);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("storefronts").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task List_ClampsPageSizeAsync()
    {
        SetupGetAll(new GetAllStorefrontsResponse
        {
            Pagination = new PaginationMetadata
            {
                TotalCount = 0,
                CurrentPage = 1,
                PageSize = 50,
            },
        });

        await ListStorefrontsTool.ExecuteAsync(_client, pageSize: 999);

        _ = _client.Received(1).GetAllAsync(
            Arg.Is<GetAllStorefrontsRequest>(r => r.Pagination.PageSize == 50),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetAllAsync(
                Arg.Any<GetAllStorefrontsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetAllStorefrontsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListStorefrontsTool.ExecuteAsync(_client);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private void SetupGetAll(GetAllStorefrontsResponse response)
        => _client.GetAllAsync(
                Arg.Any<GetAllStorefrontsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
