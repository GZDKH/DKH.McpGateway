using DKH.McpGateway.Application.Tools.Payment;
using DKH.PaymentService.Contracts.Services.V1;

namespace DKH.McpGateway.Tests.Tools.Payment;

public class PaymentToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly Payments.PaymentsClient _client =
        Substitute.For<Payments.PaymentsClient>();

    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    // ---- GetPayment ----

    [Fact]
    public async Task GetPayment_MissingPaymentId_ReturnsErrorAsync()
    {
        var result = await GetPaymentTool.ExecuteAsync(_auth, _client, paymentId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("paymentId is required");
    }

    // ---- ListPayments ----

    [Fact]
    public async Task ListPayments_HappyPath_ReturnsMetadataAsync()
    {
        SetupListPayments(new ListPaymentsResponse { TotalCount = 0, Page = 1, PageSize = 20 });

        var result = await ListPaymentsTool.ExecuteAsync(_auth, _client);

        var json = Parse(result);
        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListPayments_StorefrontFilter_SetsRequestAsync()
    {
        SetupListPayments(new ListPaymentsResponse { TotalCount = 0, Page = 2, PageSize = 10 });

        await ListPaymentsTool.ExecuteAsync(_auth, _client, storefrontId: StorefrontId, page: 2, pageSize: 10);

        _ = _client.Received(1).ListPaymentsAsync(
            Arg.Is<ListPaymentsRequest>(r =>
                r.StorefrontId.Value == StorefrontId &&
                r.Page == 2 &&
                r.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListPayments_GrpcUnavailable_ThrowsRpcExceptionAsync()
    {
        _client.ListPaymentsAsync(
                Arg.Any<ListPaymentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<ListPaymentsResponse>(
                StatusCode.Unavailable, "Service unavailable"));

        var act = () => ListPaymentsTool.ExecuteAsync(_auth, _client);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unavailable);
    }

    // ---- GetPaymentPlan ----

    [Fact]
    public async Task GetPaymentPlan_MissingPlanId_ReturnsErrorAsync()
    {
        var result = await GetPaymentPlanTool.ExecuteAsync(_auth, _client, planId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("planId is required");
    }

    // ---- ListPaymentPlans ----

    [Fact]
    public async Task ListPaymentPlans_HappyPath_ReturnsMetadataAsync()
    {
        _client.ListPaymentPlansAsync(
                Arg.Any<ListPaymentPlansRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(
                new ListPaymentPlansResponse { TotalCount = 0, Page = 1, PageSize = 20 }));

        var result = await ListPaymentPlansTool.ExecuteAsync(_auth, _client);

        Parse(result).GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    private void SetupListPayments(ListPaymentsResponse response)
        => _client.ListPaymentsAsync(
                Arg.Any<ListPaymentsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
