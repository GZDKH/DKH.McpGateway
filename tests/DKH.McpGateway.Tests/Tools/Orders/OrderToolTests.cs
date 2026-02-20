using DKH.McpGateway.Application.Tools.Orders;
using DKH.OrderService.Contracts.Order.Api.OrderCrud.v1;
using DKH.OrderService.Contracts.Order.Models.Order.v1;
using DKH.Platform.Grpc.Common.Types;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Orders;

public class OrderToolTests
{
    private readonly OrderCrudService.OrderCrudServiceClient _client =
        Substitute.For<OrderCrudService.OrderCrudServiceClient>();

    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    #region OrderSummaryTool

    [Fact]
    public async Task Summary_HappyPath_ReturnsSummaryAsync()
    {
        SetupListOrders(CreateOrderList(
        [
            (OrderStatus.Completed, 100.0, 2),
            (OrderStatus.Completed, 50.0, 1),
            (OrderStatus.Pending, 200.0, 3),
        ]));

        var result = await OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("totalOrders").GetInt32().Should().Be(3);
        json.GetProperty("totalRevenue").GetDouble().Should().Be(850.0);
        json.GetProperty("ordersByStatus").GetProperty("completed").GetInt32().Should().Be(2);
        json.GetProperty("ordersByStatus").GetProperty("pending").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Summary_NoOrders_ReturnsEmptyMessageAsync()
    {
        SetupListOrders(CreateEmptyResponse());

        var result = await OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("totalOrders").GetInt32().Should().Be(0);
        json.GetProperty("message").GetString().Should().Contain("No orders found");
    }

    [Fact]
    public async Task Summary_InvalidPeriodStart_ReturnsErrorAsync()
    {
        var result = await OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "not-a-date", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("Invalid periodStart");
    }

    [Fact]
    public async Task Summary_EndBeforeStart_ReturnsErrorAsync()
    {
        var result = await OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "2024-12-31", periodEnd: "2024-01-01");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("periodEnd must be after periodStart");
    }

    [Fact]
    public async Task Summary_ExceedsOneYear_ReturnsErrorAsync()
    {
        var result = await OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "2023-01-01", periodEnd: "2024-06-01");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("cannot exceed 1 year");
    }

    [Fact]
    public async Task Summary_WithStorefrontId_PassesStorefrontAsync()
    {
        SetupListOrders(CreateEmptyResponse());

        await OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31", storefrontId: StorefrontId);

        _ = _client.Received().ListOrdersAsync(
            Arg.Is<ListOrdersRequest>(r => r.StorefrontId.Value == StorefrontId),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Summary_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListOrdersAsync(
                Arg.Any<ListOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListOrdersResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => OrderSummaryTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    #endregion

    #region OrderTrendsTool

    [Fact]
    public async Task Trends_HappyPath_ReturnsPeriodsAsync()
    {
        SetupListOrders(CreateOrderListWithDates(
        [
            ("2024-01-15", OrderStatus.Completed, 100.0, 1),
            ("2024-01-15", OrderStatus.Completed, 200.0, 2),
            ("2024-01-15", OrderStatus.Completed, 150.0, 1),
            ("2024-01-15", OrderStatus.Completed, 120.0, 1),
            ("2024-01-15", OrderStatus.Completed, 80.0, 1),
            ("2024-02-10", OrderStatus.Pending, 300.0, 3),
            ("2024-02-10", OrderStatus.Pending, 250.0, 2),
            ("2024-02-10", OrderStatus.Pending, 180.0, 1),
            ("2024-02-10", OrderStatus.Pending, 220.0, 1),
            ("2024-02-10", OrderStatus.Pending, 190.0, 1),
        ]));

        var result = await OrderTrendsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-03-01", granularity: "month");

        var json = Parse(result);
        json.GetProperty("granularity").GetString().Should().Be("month");
        json.GetProperty("periods").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Trends_InvalidGranularity_ReturnsErrorAsync()
    {
        var result = await OrderTrendsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31", granularity: "hour");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("granularity must be");
    }

    [Fact]
    public async Task Trends_NoOrders_ReturnsEmptyMessageAsync()
    {
        SetupListOrders(CreateEmptyResponse());

        var result = await OrderTrendsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("totalOrders").GetInt32().Should().Be(0);
        json.GetProperty("message").GetString().Should().Contain("No orders found");
    }

    [Fact]
    public async Task Trends_InvalidPeriod_ReturnsErrorAsync()
    {
        var result = await OrderTrendsTool.ExecuteAsync(_client,
            periodStart: "bad-date", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("Invalid periodStart");
    }

    [Fact]
    public async Task Trends_DefaultGranularity_UsesDayAsync()
    {
        SetupListOrders(CreateOrderListWithDates(
        [
            ("2024-01-15", OrderStatus.Completed, 100.0, 1),
            ("2024-01-15", OrderStatus.Completed, 200.0, 2),
            ("2024-01-15", OrderStatus.Completed, 150.0, 1),
            ("2024-01-15", OrderStatus.Completed, 120.0, 1),
            ("2024-01-15", OrderStatus.Completed, 80.0, 1),
        ]));

        var result = await OrderTrendsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-01-31");

        var json = Parse(result);
        json.GetProperty("granularity").GetString().Should().Be("day");
    }

    [Fact]
    public async Task Trends_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListOrdersAsync(
                Arg.Any<ListOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListOrdersResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => OrderTrendsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    #endregion

    #region OrderStatusDistributionTool

    [Fact]
    public async Task StatusDistribution_HappyPath_ReturnsBreakdownAsync()
    {
        SetupListOrders(CreateOrderList(
        [
            (OrderStatus.Completed, 100.0, 1),
            (OrderStatus.Completed, 200.0, 2),
            (OrderStatus.Completed, 150.0, 1),
            (OrderStatus.Pending, 50.0, 1),
            (OrderStatus.Cancelled, 75.0, 1),
        ]));

        var result = await OrderStatusDistributionTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("totalOrders").GetInt32().Should().Be(5);
        json.GetProperty("statusBreakdown").GetArrayLength().Should().BeGreaterThan(0);
        json.GetProperty("conversionRate").GetDouble().Should().Be(60.0);
        json.GetProperty("cancellationRate").GetDouble().Should().Be(20.0);
    }

    [Fact]
    public async Task StatusDistribution_NoOrders_ReturnsEmptyMessageAsync()
    {
        SetupListOrders(CreateEmptyResponse());

        var result = await OrderStatusDistributionTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("totalOrders").GetInt32().Should().Be(0);
        json.GetProperty("message").GetString().Should().Contain("No orders found");
    }

    [Fact]
    public async Task StatusDistribution_InvalidPeriod_ReturnsErrorAsync()
    {
        var result = await OrderStatusDistributionTool.ExecuteAsync(_client,
            periodStart: "2024-12-31", periodEnd: "2024-01-01");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("periodEnd must be after periodStart");
    }

    [Fact]
    public async Task StatusDistribution_AllCompleted_100PercentConversionAsync()
    {
        SetupListOrders(CreateOrderList(
        [
            (OrderStatus.Completed, 100.0, 1),
            (OrderStatus.Completed, 200.0, 2),
        ]));

        var result = await OrderStatusDistributionTool.ExecuteAsync(_client);

        var json = Parse(result);
        json.GetProperty("conversionRate").GetDouble().Should().Be(100.0);
        json.GetProperty("cancellationRate").GetDouble().Should().Be(0.0);
    }

    [Fact]
    public async Task StatusDistribution_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListOrdersAsync(
                Arg.Any<ListOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListOrdersResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => OrderStatusDistributionTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    #endregion

    #region TopSellingProductsTool

    [Fact]
    public async Task TopSelling_HappyPath_ReturnsProductsAsync()
    {
        var productId = Guid.NewGuid().ToString();
        SetupListOrders(CreateOrderListWithProducts(productId, orderCount: 6));

        var result = await TopSellingProductsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("products").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TopSelling_NoOrders_ReturnsEmptyMessageAsync()
    {
        SetupListOrders(CreateEmptyResponse());

        var result = await TopSellingProductsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("totalOrders").GetInt32().Should().Be(0);
        json.GetProperty("message").GetString().Should().Contain("No orders found");
    }

    [Fact]
    public async Task TopSelling_BelowKAnonymity_ReturnsEmptyProductsWithNoteAsync()
    {
        var productId = Guid.NewGuid().ToString();
        SetupListOrders(CreateOrderListWithProducts(productId, orderCount: 3));

        var result = await TopSellingProductsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("products").GetArrayLength().Should().Be(0);
        json.GetProperty("note").GetString().Should().Contain("k-anonymity");
    }

    [Fact]
    public async Task TopSelling_LimitClamped_RespectsMaxAsync()
    {
        var productId = Guid.NewGuid().ToString();
        SetupListOrders(CreateOrderListWithProducts(productId, orderCount: 6));

        var result = await TopSellingProductsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31", limit: 100);

        var json = Parse(result);
        json.GetProperty("products").GetArrayLength().Should().BeLessThanOrEqualTo(50);
    }

    [Fact]
    public async Task TopSelling_InvalidPeriod_ReturnsErrorAsync()
    {
        var result = await TopSellingProductsTool.ExecuteAsync(_client,
            periodStart: "not-a-date", periodEnd: "2024-12-31");

        var json = Parse(result);
        json.GetProperty("error").GetString().Should().Contain("Invalid periodStart");
    }

    [Fact]
    public async Task TopSelling_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListOrdersAsync(
                Arg.Any<ListOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListOrdersResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => TopSellingProductsTool.ExecuteAsync(_client,
            periodStart: "2024-01-01", periodEnd: "2024-12-31");

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    #endregion

    #region Helpers

    private void SetupListOrders(ListOrdersResponse response)
        => _client.ListOrdersAsync(
                Arg.Any<ListOrdersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static ListOrdersResponse CreateEmptyResponse() => new()
    {
        Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 50 },
    };

    private static ListOrdersResponse CreateOrderList(
        (OrderStatus Status, double UnitPrice, int Quantity)[] orders)
    {
        var response = new ListOrdersResponse
        {
            Metadata = new PaginationMetadata
            {
                TotalCount = orders.Length,
                CurrentPage = 1,
                PageSize = 50,
            },
        };

        foreach (var (status, unitPrice, quantity) in orders)
        {
            var order = new OrderModel
            {
                Id = new GuidValue(Guid.NewGuid().ToString()),
                StorefrontId = new GuidValue(StorefrontId),
                OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..8]}",
                Status = status,
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow),
            };
            order.Items.Add(new OrderItemModel
            {
                ProductId = new GuidValue(Guid.NewGuid().ToString()),
                Sku = "SKU-001",
                Name = "Test Product",
                UnitPrice = unitPrice,
                Quantity = quantity,
            });
            response.Items.Add(order);
        }

        return response;
    }

    private static ListOrdersResponse CreateOrderListWithDates(
        (string Date, OrderStatus Status, double UnitPrice, int Quantity)[] orders)
    {
        var response = new ListOrdersResponse
        {
            Metadata = new PaginationMetadata
            {
                TotalCount = orders.Length,
                CurrentPage = 1,
                PageSize = 50,
            },
        };

        foreach (var (date, status, unitPrice, quantity) in orders)
        {
            var order = new OrderModel
            {
                Id = new GuidValue(Guid.NewGuid().ToString()),
                StorefrontId = new GuidValue(StorefrontId),
                OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..8]}",
                Status = status,
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(date, System.Globalization.CultureInfo.InvariantCulture)),
            };
            order.Items.Add(new OrderItemModel
            {
                ProductId = new GuidValue(Guid.NewGuid().ToString()),
                Sku = "SKU-001",
                Name = "Test Product",
                UnitPrice = unitPrice,
                Quantity = quantity,
            });
            response.Items.Add(order);
        }

        return response;
    }

    private static ListOrdersResponse CreateOrderListWithProducts(string productId, int orderCount)
    {
        var response = new ListOrdersResponse
        {
            Metadata = new PaginationMetadata
            {
                TotalCount = orderCount,
                CurrentPage = 1,
                PageSize = 50,
            },
        };

        for (var i = 0; i < orderCount; i++)
        {
            var order = new OrderModel
            {
                Id = new GuidValue(Guid.NewGuid().ToString()),
                StorefrontId = new GuidValue(StorefrontId),
                OrderNumber = $"ORD-{i:D4}",
                Status = OrderStatus.Completed,
                CreatedAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddDays(-i)),
            };
            order.Items.Add(new OrderItemModel
            {
                ProductId = new GuidValue(productId),
                Sku = "SKU-TOP",
                Name = "Top Product",
                UnitPrice = 49.99,
                Quantity = 2,
            });
            response.Items.Add(order);
        }

        return response;
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    #endregion
}
