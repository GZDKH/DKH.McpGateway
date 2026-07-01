using System.Globalization;
using DKH.McpGateway.Application.Tools.Notifications;
using DKH.McpGateway.Application.Tools.Payment;
using DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1;
using DKH.NotificationService.Contracts.Notification.Models.BulkJobStatus.v1;
using DKH.NotificationService.Contracts.Notification.Models.NotificationChannel.v1;
using DKH.NotificationService.Contracts.Notification.Models.NotificationDelivery.v1;
using DKH.NotificationService.Contracts.Notification.Models.NotificationHealth.v1;
using DKH.NotificationService.Contracts.Notification.Models.NotificationStatus.v1;
using DKH.Platform.Grpc.Common.Types;
using Google.Protobuf.WellKnownTypes;
using NotificationGrpc = DKH.NotificationService.Contracts.Notification.Api.NotificationManagement.v1.NotificationManagementService;

namespace DKH.McpGateway.Tests.Tools.Notifications;

public sealed class NotificationToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.ReadOnly();
    private readonly NotificationGrpc.NotificationManagementServiceClient _client =
        Substitute.For<NotificationGrpc.NotificationManagementServiceClient>();

    private static readonly string DeliveryId = Guid.NewGuid().ToString();
    private static readonly string JobId = Guid.NewGuid().ToString();
    private static readonly string OrderId = Guid.NewGuid().ToString();
    private static readonly string RecipientId = Guid.NewGuid().ToString();
    private static readonly string StorefrontId = Guid.NewGuid().ToString();

    [Fact]
    public void NotificationToolSurface_DefinesReadOnlyToolsOnly()
    {
        string[] expected =
        [
            "get_notification_health",
            "get_notification_status",
            "get_bulk_job_status",
            "list_bulk_jobs",
            "get_bulk_job_failures",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Notifications")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().BeEquivalentTo(expected);
        actual.Should().NotContain(name => name.Contains("send", StringComparison.OrdinalIgnoreCase));
        actual.Should().NotContain("cancel_bulk_job");
    }

    [Fact]
    public async Task GetNotificationHealth_ReadOnly_MapsStatusAndChannelsAsync()
    {
        var response = new NotificationHealthResponse
        {
            Statuses = new NotificationHealthStatusSummaryModel
            {
                Total = 10,
                Pending = 1,
                Sent = 8,
                Failed = 1,
            },
        };
        response.Channels.Add(new NotificationChannelStatsModel
        {
            Channel = NotificationChannel.Email,
            Total = 6,
            Pending = 1,
            Sent = 4,
            Failed = 1,
        });

        _client.GetNotificationHealthAsync(
                Arg.Any<GetNotificationHealthRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await GetNotificationHealthTool.ExecuteAsync(_auth, _client));

        json.GetProperty("statuses").GetProperty("total").GetInt32().Should().Be(10);
        json.GetProperty("channels")[0].GetProperty("channel").GetString().Should().Be("Email");
    }

    [Fact]
    public async Task GetNotificationStatus_MissingOrderId_ReturnsErrorAsync()
    {
        var result = await GetNotificationStatusTool.ExecuteAsync(_auth, _client, orderId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("orderId is required");
    }

    [Fact]
    public async Task GetNotificationStatus_MapsDeliveryWithoutRecipientAsync()
    {
        _client.GetNotificationStatusAsync(
                Arg.Any<GetNotificationStatusRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(Delivery()));

        var json = Parse(await GetNotificationStatusTool.ExecuteAsync(_auth, _client, OrderId));
        var raw = json.GetRawText();

        json.GetProperty("id").GetString().Should().Be(DeliveryId);
        json.GetProperty("orderId").GetString().Should().Be(OrderId);
        json.GetProperty("channel").GetString().Should().Be("Email");
        json.GetProperty("status").GetString().Should().Be("Sent");
        raw.Should().NotContain("alice@example.com");
        raw.Should().NotContain("recipient");

        _ = _client.Received(1).GetNotificationStatusAsync(
            Arg.Is<GetNotificationStatusRequest>(r => r.OrderId.Value == OrderId),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBulkJobStatus_MissingJobId_ReturnsErrorAsync()
    {
        var result = await GetBulkJobStatusTool.ExecuteAsync(_auth, _client, jobId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("jobId is required");
    }

    [Fact]
    public async Task GetBulkJobStatus_MapsCountsAndOptionalTimestampsAsync()
    {
        var response = new GetBulkJobStatusResponse
        {
            JobId = new GuidValue(JobId),
            Status = BulkJobStatus.Processing,
            TotalRecipients = 100,
            ProcessedCount = 40,
            SentCount = 35,
            FailedCount = 5,
            CreatedAt = Timestamp("2026-01-01T10:00:00Z"),
            StartedAt = Timestamp("2026-01-01T10:05:00Z"),
        };

        _client.GetBulkJobStatusAsync(
                Arg.Any<GetBulkJobStatusRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await GetBulkJobStatusTool.ExecuteAsync(_auth, _client, JobId));

        json.GetProperty("jobId").GetString().Should().Be(JobId);
        json.GetProperty("status").GetString().Should().Be("Processing");
        json.GetProperty("processedCount").GetInt32().Should().Be(40);
        json.TryGetProperty("recipient", out _).Should().BeFalse();
    }

    [Fact]
    public async Task ListBulkJobs_MapsFilterAndPaginationAsync()
    {
        var response = new ListBulkJobsResponse
        {
            Pagination = new PaginationMetadata
            {
                TotalCount = 1,
                CurrentPage = 2,
                PageSize = 10,
                TotalPages = 3,
                HasNextPage = true,
                HasPreviousPage = true,
            },
        };
        response.Jobs.Add(new BulkJobSummaryModel
        {
            JobId = new GuidValue(JobId),
            IdempotencyKey = "bulk-1",
            Channel = NotificationChannel.Telegram,
            Status = BulkJobStatus.Completed,
            TotalRecipients = 20,
            SentCount = 19,
            FailedCount = 1,
            CreatedAt = Timestamp("2026-01-01T10:00:00Z"),
            StorefrontId = new GuidValue(StorefrontId),
        });

        _client.ListBulkJobsAsync(
                Arg.Any<ListBulkJobsRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await ListBulkJobsTool.ExecuteAsync(_auth, _client, status: "Completed", page: 2, pageSize: 10));

        json.GetProperty("items")[0].GetProperty("channel").GetString().Should().Be("Telegram");
        json.GetProperty("pagination").GetProperty("totalPages").GetInt32().Should().Be(3);

        _ = _client.Received(1).ListBulkJobsAsync(
            Arg.Is<ListBulkJobsRequest>(r =>
                r.Status == BulkJobStatus.Completed &&
                r.Pagination.Page == 2 &&
                r.Pagination.PageSize == 10),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBulkJobFailures_MapsFailuresWithoutRecipientAsync()
    {
        var response = new GetBulkJobFailuresResponse
        {
            Pagination = new PaginationMetadata { TotalCount = 1, CurrentPage = 1, PageSize = 20 },
        };
        response.Failures.Add(new BulkRecipientFailureModel
        {
            RecipientId = new GuidValue(RecipientId),
            Recipient = "+15551234567",
            FailureReason = "invalid destination",
            AttemptCount = 2,
            ProcessedAt = Timestamp("2026-01-01T10:10:00Z"),
        });

        _client.GetBulkJobFailuresAsync(
                Arg.Any<GetBulkJobFailuresRequest>(),
                Arg.Any<Metadata>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await GetBulkJobFailuresTool.ExecuteAsync(_auth, _client, JobId));
        var raw = json.GetRawText();

        json.GetProperty("items")[0].GetProperty("recipientId").GetString().Should().Be(RecipientId);
        json.GetProperty("items")[0].GetProperty("attemptCount").GetInt32().Should().Be(2);
        raw.Should().NotContain("+15551234567");
        raw.Should().NotContain("recipient\":\"");

        _ = _client.Received(1).GetBulkJobFailuresAsync(
            Arg.Is<GetBulkJobFailuresRequest>(r => r.JobId.Value == JobId),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    private static NotificationDeliveryModel Delivery() => new()
    {
        Id = new GuidValue(DeliveryId),
        OrderId = new GuidValue(OrderId),
        Channel = NotificationChannel.Email,
        Status = NotificationStatus.Sent,
        Recipient = "alice@example.com",
        Message = "Order shipped",
        CreatedAtUtc = Timestamp("2026-01-01T10:00:00Z"),
        DeliveredAtUtc = Timestamp("2026-01-01T10:01:00Z"),
        BotId = new GuidValue(Guid.NewGuid().ToString()),
        ParseMode = "HTML",
        StorefrontId = new GuidValue(StorefrontId),
    };

    private static Timestamp Timestamp(string value)
        => Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
            DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
