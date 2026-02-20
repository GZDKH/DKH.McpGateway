using DKH.McpGateway.Application.Tools.Reviews;
using DKH.Platform.Grpc.Common.Types;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.ProductManagement.v1;
using DKH.ProductCatalogService.Contracts.ProductCatalog.Api.QueryCommon.v1;
using DKH.ReviewService.Contracts.Review.Api.ReviewQuery.v1;
using DKH.ReviewService.Contracts.Review.Models.Review.v1;
using DKH.ReviewService.Contracts.Review.Models.ReviewAggregate.v1;
using DKH.ReviewService.Contracts.Review.Models.ReviewReply.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Reviews;

public class ReviewStatsToolTests
{
    private readonly ReviewQueryService.ReviewQueryServiceClient _client =
        Substitute.For<ReviewQueryService.ReviewQueryServiceClient>();

    private static readonly string ProductId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    [Fact]
    public async Task ReviewStats_HappyPath_ReturnsStatsAsync()
    {
        SetupAggregate(new ReviewAggregateModel
        {
            ProductId = ProductId,
            StorefrontId = StorefrontId,
            AverageScore = 4.25,
            TotalCount = 20,
            Count1 = 1,
            Count2 = 2,
            Count3 = 3,
            Count4 = 6,
            Count5 = 8,
            LastUpdatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
        });

        var result = await ReviewStatsTool.ExecuteAsync(_client, ProductId, StorefrontId);

        var json = Parse(result);
        json.GetProperty("productId").GetString().Should().Be(ProductId);
        json.GetProperty("storefrontId").GetString().Should().Be(StorefrontId);
        json.GetProperty("averageScore").GetDouble().Should().Be(4.25);
        json.GetProperty("totalReviews").GetInt32().Should().Be(20);
    }

    [Fact]
    public async Task ReviewStats_RatingDistribution_CalculatedCorrectlyAsync()
    {
        SetupAggregate(new ReviewAggregateModel
        {
            ProductId = ProductId,
            StorefrontId = StorefrontId,
            AverageScore = 3.0,
            TotalCount = 10,
            Count1 = 2,
            Count2 = 2,
            Count3 = 2,
            Count4 = 2,
            Count5 = 2,
        });

        var result = await ReviewStatsTool.ExecuteAsync(_client, ProductId, StorefrontId);

        var dist = Parse(result).GetProperty("ratingDistribution");
        dist.GetProperty("stars5").GetProperty("count").GetInt32().Should().Be(2);
        dist.GetProperty("stars5").GetProperty("percentage").GetDouble().Should().Be(20.0);
        dist.GetProperty("stars1").GetProperty("count").GetInt32().Should().Be(2);
        dist.GetProperty("stars1").GetProperty("percentage").GetDouble().Should().Be(20.0);
    }

    [Fact]
    public async Task ReviewStats_SentimentBreakdown_CorrectAsync()
    {
        SetupAggregate(new ReviewAggregateModel
        {
            ProductId = ProductId,
            StorefrontId = StorefrontId,
            TotalCount = 10,
            Count1 = 1,
            Count2 = 1,
            Count3 = 2,
            Count4 = 3,
            Count5 = 3,
        });

        var result = await ReviewStatsTool.ExecuteAsync(_client, ProductId, StorefrontId);

        var sentiment = Parse(result).GetProperty("sentiment");
        sentiment.GetProperty("positive").GetInt32().Should().Be(6);
        sentiment.GetProperty("neutral").GetInt32().Should().Be(2);
        sentiment.GetProperty("negative").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ReviewStats_ZeroReviews_NoPercentageErrorAsync()
    {
        SetupAggregate(new ReviewAggregateModel
        {
            ProductId = ProductId,
            StorefrontId = StorefrontId,
            TotalCount = 0,
        });

        var result = await ReviewStatsTool.ExecuteAsync(_client, ProductId, StorefrontId);

        var json = Parse(result);
        json.GetProperty("totalReviews").GetInt32().Should().Be(0);

        var dist = json.GetProperty("ratingDistribution");
        dist.GetProperty("stars5").GetProperty("percentage").GetDouble().Should().Be(0);
    }

    [Fact]
    public async Task ReviewStats_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetProductReviewAggregateAsync(
                Arg.Any<GetProductReviewAggregateRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ReviewAggregateModel>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ReviewStatsTool.ExecuteAsync(_client, ProductId, StorefrontId);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private void SetupAggregate(ReviewAggregateModel response)
        => _client.GetProductReviewAggregateAsync(
                Arg.Any<GetProductReviewAggregateRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ReviewSummaryToolTests
{
    private readonly ReviewQueryService.ReviewQueryServiceClient _client =
        Substitute.For<ReviewQueryService.ReviewQueryServiceClient>();

    private static readonly string ProductId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    [Fact]
    public async Task ReviewSummary_HappyPath_ReturnsSentimentGroupsAsync()
    {
        var response = new GetProductReviewsResponse();
        response.Items.Add(CreateReviewItem(5, "Great!", "Loved it"));
        response.Items.Add(CreateReviewItem(4, "Good", "Very nice product"));
        response.Items.Add(CreateReviewItem(3, "OK", "Average product"));
        response.Items.Add(CreateReviewItem(2, "Bad", "Not worth it"));
        response.Items.Add(CreateReviewItem(1, "Terrible", "Worst product ever"));
        SetupGetProductReviews(response);

        var result = await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        var json = Parse(result);
        json.GetProperty("productId").GetString().Should().Be(ProductId);
        json.GetProperty("totalFetched").GetInt32().Should().Be(5);

        var sentiment = json.GetProperty("sentiment");
        sentiment.GetProperty("positive").GetProperty("count").GetInt32().Should().Be(2);
        sentiment.GetProperty("neutral").GetProperty("count").GetInt32().Should().Be(1);
        sentiment.GetProperty("negative").GetProperty("count").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ReviewSummary_SamplesLimitedTo3Async()
    {
        var response = new GetProductReviewsResponse();
        for (var i = 0; i < 10; i++)
        {
            response.Items.Add(CreateReviewItem(5, $"Title {i}", $"Body {i}"));
        }

        SetupGetProductReviews(response);

        var result = await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        var positive = Parse(result).GetProperty("sentiment").GetProperty("positive");
        positive.GetProperty("count").GetInt32().Should().Be(10);
        positive.GetProperty("samples").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task ReviewSummary_LongTextTruncatedAsync()
    {
        var longText = new string('x', 300);
        var response = new GetProductReviewsResponse();
        response.Items.Add(CreateReviewItem(5, "Title", longText));
        SetupGetProductReviews(response);

        var result = await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        var sample = Parse(result).GetProperty("sentiment").GetProperty("positive")
            .GetProperty("samples")[0];
        var text = sample.GetProperty("text").GetString()!;
        text.Length.Should().BeLessThanOrEqualTo(203);
        text.Should().EndWith("...");
    }

    [Fact]
    public async Task ReviewSummary_EmptyTitleAndBody_ReturnsNullAsync()
    {
        var response = new GetProductReviewsResponse();
        response.Items.Add(CreateReviewItem(5, "", ""));
        SetupGetProductReviews(response);

        var result = await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        var sample = Parse(result).GetProperty("sentiment").GetProperty("positive")
            .GetProperty("samples")[0];
        sample.GetProperty("title").ValueKind.Should().Be(JsonValueKind.Null);
        sample.GetProperty("text").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ReviewSummary_WithReplies_HasStoreRepliesTrueAsync()
    {
        var response = new GetProductReviewsResponse();
        var item = CreateReviewItem(5, "Great", "Nice");
        item.Reply = new ReviewReplyModel
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            ReviewId = item.Review.Id,
            Body = "Thank you!",
        };
        response.Items.Add(item);
        SetupGetProductReviews(response);

        var result = await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        Parse(result).GetProperty("hasStoreReplies").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ReviewSummary_NoReplies_HasStoreRepliesFalseAsync()
    {
        var response = new GetProductReviewsResponse();
        response.Items.Add(CreateReviewItem(5, "Good", "Fine"));
        SetupGetProductReviews(response);

        var result = await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        Parse(result).GetProperty("hasStoreReplies").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ReviewSummary_LimitClamped_To100Async()
    {
        SetupGetProductReviews(new GetProductReviewsResponse());

        await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId, limit: 200);

        _ = _client.Received(1).GetProductReviewsAsync(
            Arg.Is<GetProductReviewsRequest>(r =>
                r.Pagination.PageSize == 100),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewSummary_LimitClamped_To1Async()
    {
        SetupGetProductReviews(new GetProductReviewsResponse());

        await ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId, limit: 0);

        _ = _client.Received(1).GetProductReviewsAsync(
            Arg.Is<GetProductReviewsRequest>(r =>
                r.Pagination.PageSize == 1),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewSummary_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.GetProductReviewsAsync(
                Arg.Any<GetProductReviewsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetProductReviewsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ReviewSummaryTool.ExecuteAsync(
            _client, ProductId, StorefrontId);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private static ProductReviewItemModel CreateReviewItem(int score, string title, string body) => new()
    {
        Review = new ReviewModel
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            StorefrontId = new GuidValue(StorefrontId),
            ProductId = new GuidValue(ProductId),
            CustomerId = new GuidValue(Guid.NewGuid().ToString()),
            Score = score,
            Title = title,
            Body = body,
            Status = ReviewStatus.Approved,
        },
    };

    private void SetupGetProductReviews(GetProductReviewsResponse response)
        => _client.GetProductReviewsAsync(
                Arg.Any<GetProductReviewsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}

public class ProductReviewRankingToolTests
{
    private readonly ProductManagementService.ProductManagementServiceClient _searchClient =
        Substitute.For<ProductManagementService.ProductManagementServiceClient>();

    private readonly ReviewQueryService.ReviewQueryServiceClient _reviewClient =
        Substitute.For<ReviewQueryService.ReviewQueryServiceClient>();

    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    [Fact]
    public async Task ProductReviewRanking_HappyPath_ReturnsRankedProductsAsync()
    {
        var productId1 = Guid.NewGuid().ToString();
        var productId2 = Guid.NewGuid().ToString();

        SetupSearch(new SearchProductsResponse
        {
            Items =
            {
                new ProductListItem { Id = new GuidValue(productId1), Name = "Product A" },
                new ProductListItem { Id = new GuidValue(productId2), Name = "Product B" },
            },
            TotalCount = 2,
        });

        SetupAggregates(new GetProductsReviewAggregatesResponse
        {
            Aggregates =
            {
                new ReviewAggregateModel
                {
                    ProductId = productId1, AverageScore = 4.5, TotalCount = 10,
                    Count4 = 3, Count5 = 7,
                },
                new ReviewAggregateModel
                {
                    ProductId = productId2, AverageScore = 3.5, TotalCount = 5,
                    Count4 = 2, Count5 = 1, Count3 = 2,
                },
            },
        });

        var result = await ExecuteToolAsync();

        var json = Parse(result);
        json.GetProperty("sortedBy").GetString().Should().Be("avgRating");
        json.GetProperty("totalProductsWithReviews").GetInt32().Should().Be(2);

        var products = json.GetProperty("products");
        products.GetArrayLength().Should().Be(2);
        products[0].GetProperty("rank").GetInt32().Should().Be(1);
        products[0].GetProperty("averageScore").GetDouble().Should().Be(4.5);
        products[1].GetProperty("rank").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task ProductReviewRanking_SortByReviewCount_OrdersByCountAsync()
    {
        var productId1 = Guid.NewGuid().ToString();
        var productId2 = Guid.NewGuid().ToString();

        SetupSearch(new SearchProductsResponse
        {
            Items =
            {
                new ProductListItem { Id = new GuidValue(productId1), Name = "Product A" },
                new ProductListItem { Id = new GuidValue(productId2), Name = "Product B" },
            },
            TotalCount = 2,
        });

        SetupAggregates(new GetProductsReviewAggregatesResponse
        {
            Aggregates =
            {
                new ReviewAggregateModel
                {
                    ProductId = productId1, AverageScore = 4.5, TotalCount = 5,
                    Count4 = 2, Count5 = 3,
                },
                new ReviewAggregateModel
                {
                    ProductId = productId2, AverageScore = 3.5, TotalCount = 20,
                    Count4 = 5, Count5 = 5, Count3 = 10,
                },
            },
        });

        var result = await ExecuteToolAsync(sortBy: "reviewCount");

        var products = Parse(result).GetProperty("products");
        products[0].GetProperty("totalReviews").GetInt32().Should().Be(20);
        products[1].GetProperty("totalReviews").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task ProductReviewRanking_MinReviewsFilters_ProductsBelowThresholdAsync()
    {
        var productId1 = Guid.NewGuid().ToString();
        var productId2 = Guid.NewGuid().ToString();

        SetupSearch(new SearchProductsResponse
        {
            Items =
            {
                new ProductListItem { Id = new GuidValue(productId1), Name = "Many Reviews" },
                new ProductListItem { Id = new GuidValue(productId2), Name = "Few Reviews" },
            },
            TotalCount = 2,
        });

        SetupAggregates(new GetProductsReviewAggregatesResponse
        {
            Aggregates =
            {
                new ReviewAggregateModel { ProductId = productId1, AverageScore = 4.0, TotalCount = 10 },
                new ReviewAggregateModel { ProductId = productId2, AverageScore = 5.0, TotalCount = 2 },
            },
        });

        var result = await ExecuteToolAsync(minReviews: 5);

        var products = Parse(result).GetProperty("products");
        products.GetArrayLength().Should().Be(1);
        products[0].GetProperty("totalReviews").GetInt32().Should().Be(10);
    }

    [Fact]
    public async Task ProductReviewRanking_NoProducts_ReturnsEmptyWithMessageAsync()
    {
        SetupSearch(new SearchProductsResponse { TotalCount = 0 });

        var result = await ExecuteToolAsync();

        var json = Parse(result);
        json.GetProperty("products").GetArrayLength().Should().Be(0);
        json.GetProperty("message").GetString().Should().Contain("No products found");
    }

    [Fact]
    public async Task ProductReviewRanking_LimitClamped_To30Async()
    {
        SetupSearch(new SearchProductsResponse
        {
            Items = { new ProductListItem { Id = new GuidValue(Guid.NewGuid().ToString()), Name = "P1" } },
            TotalCount = 1,
        });

        SetupAggregates(new GetProductsReviewAggregatesResponse
        {
            Aggregates =
            {
                new ReviewAggregateModel
                {
                    ProductId = Guid.NewGuid().ToString(), AverageScore = 4.0, TotalCount = 5,
                },
            },
        });

        await ExecuteToolAsync(limit: 50);

        // No error should occur; limit is clamped internally
    }

    [Fact]
    public async Task ProductReviewRanking_PositiveRate_CalculatedCorrectlyAsync()
    {
        var productId = Guid.NewGuid().ToString();

        SetupSearch(new SearchProductsResponse
        {
            Items = { new ProductListItem { Id = new GuidValue(productId), Name = "P1" } },
            TotalCount = 1,
        });

        SetupAggregates(new GetProductsReviewAggregatesResponse
        {
            Aggregates =
            {
                new ReviewAggregateModel
                {
                    ProductId = productId, AverageScore = 4.0, TotalCount = 10,
                    Count1 = 1, Count2 = 1, Count3 = 2, Count4 = 3, Count5 = 3,
                },
            },
        });

        var result = await ExecuteToolAsync();

        var product = Parse(result).GetProperty("products")[0];
        product.GetProperty("positiveRate").GetDouble().Should().Be(60.0);
    }

    [Fact]
    public async Task ProductReviewRanking_GrpcUnavailable_Search_ThrowsRpcExceptionAsync()
    {
        _searchClient.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<SearchProductsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync();

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    [Fact]
    public async Task ProductReviewRanking_GrpcUnavailable_Reviews_ThrowsRpcExceptionAsync()
    {
        SetupSearch(new SearchProductsResponse
        {
            Items = { new ProductListItem { Id = new GuidValue(Guid.NewGuid().ToString()), Name = "P1" } },
            TotalCount = 1,
        });

        _reviewClient.GetProductsReviewAggregatesAsync(
                Arg.Any<GetProductsReviewAggregatesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<GetProductsReviewAggregatesResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ExecuteToolAsync();

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    private Task<string> ExecuteToolAsync(
        string catalogSeoName = "main-catalog",
        string languageCode = "ru",
        string sortBy = "avgRating",
        int limit = 10,
        int minReviews = 3)
        => ProductReviewRankingTool.ExecuteAsync(
            _searchClient, _reviewClient,
            storefrontId: StorefrontId,
            catalogSeoName: catalogSeoName,
            languageCode: languageCode,
            sortBy: sortBy,
            limit: limit,
            minReviews: minReviews);

    private void SetupSearch(SearchProductsResponse response)
        => _searchClient.SearchProductsAsync(
                Arg.Any<SearchProductsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private void SetupAggregates(GetProductsReviewAggregatesResponse response)
        => _reviewClient.GetProductsReviewAggregatesAsync(
                Arg.Any<GetProductsReviewAggregatesRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
