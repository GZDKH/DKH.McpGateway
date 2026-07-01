using DKH.McpGateway.Application.Tools.Print;
using DKH.Platform.Grpc.Common.Types;
using DKH.PrintService.Contracts.Models.V1;
using DKH.PrintService.Contracts.Services.V1;

namespace DKH.McpGateway.Tests.Tools.Print;

public class PrintToolTests
{
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();

    private readonly Prints.PrintsClient _client = Substitute.For<Prints.PrintsClient>();

    private static readonly string JobId = Guid.NewGuid().ToString();
    private static readonly string LocationId = Guid.NewGuid().ToString();

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    // ---- get_print_job ----

    [Fact]
    public async Task GetPrintJob_MissingId_ReturnsErrorAsync()
    {
        var result = await GetPrintJobTool.ExecuteAsync(_auth, _client, jobId: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("jobId is required");
    }

    [Fact]
    public async Task GetPrintJob_HappyPath_MapsJobAsync()
    {
        _client.GetJobAsync(Arg.Any<GetJobRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PrintJobModel
            {
                Id = new GuidValue(JobId),
                JobType = "receipt",
                PayloadRef = "s3://blob/1",
                RoutingHints = "tray=1",
                Status = JobStatus.Queued,
            }));

        var json = Parse(await GetPrintJobTool.ExecuteAsync(_auth, _client, jobId: JobId));

        json.GetProperty("id").GetString().Should().Be(JobId);
        json.GetProperty("jobType").GetString().Should().Be("receipt");
        json.GetProperty("status").GetString().Should().Be("Queued");
    }

    // ---- list_printers ----

    [Fact]
    public async Task ListPrinters_HappyPath_ReturnsPrintersAsync()
    {
        var response = new ListPrintersResponse();
        response.Printers.Add(new PrinterModel
        {
            Id = new GuidValue(Guid.NewGuid().ToString()),
            Name = "Front desk",
            Type = PrinterType.Thermal,
            ConnectionString = "tcp://10.0.0.5",
            Status = PrinterStatus.Active,
        });
        _client.ListPrintersAsync(Arg.Any<ListPrintersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(response));

        var json = Parse(await ListPrintersTool.ExecuteAsync(_auth, _client));

        json.GetProperty("printers").GetArrayLength().Should().Be(1);
        json.GetProperty("printers")[0].GetProperty("type").GetString().Should().Be("Thermal");
    }

    [Fact]
    public async Task ListPrinters_LocationFilter_SetsRequestAsync()
    {
        _client.ListPrintersAsync(Arg.Any<ListPrintersRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListPrintersResponse()));

        await ListPrintersTool.ExecuteAsync(_auth, _client, locationId: LocationId);

        _ = _client.Received(1).ListPrintersAsync(
            Arg.Is<ListPrintersRequest>(r => r.LocationId.Value == LocationId),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    // ---- register_printer ----

    [Fact]
    public async Task RegisterPrinter_MissingName_ReturnsErrorAsync()
    {
        var result = await RegisterPrinterTool.ExecuteAsync(_auth, _client, name: "", connectionString: "tcp://x");

        Parse(result).GetProperty("error").GetString().Should().Contain("name is required");
    }

    [Fact]
    public async Task RegisterPrinter_MissingConnectionString_ReturnsErrorAsync()
    {
        var result = await RegisterPrinterTool.ExecuteAsync(_auth, _client, name: "P", connectionString: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("connectionString is required");
    }

    [Fact]
    public async Task RegisterPrinter_ParsesTypeAndMapsAsync()
    {
        _client.RegisterPrinterAsync(Arg.Any<RegisterPrinterRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PrinterModel
            {
                Id = new GuidValue(Guid.NewGuid().ToString()),
                Name = "Label-1",
                Type = PrinterType.Label,
                ConnectionString = "usb://label",
                Status = PrinterStatus.Active,
            }));

        await RegisterPrinterTool.ExecuteAsync(_auth, _client, name: "Label-1",
            connectionString: "usb://label", type: "label", locationId: LocationId);

        _ = _client.Received(1).RegisterPrinterAsync(
            Arg.Is<RegisterPrinterRequest>(r =>
                r.Name == "Label-1" &&
                r.Type == PrinterType.Label &&
                r.ConnectionString == "usb://label" &&
                r.LocationId.Value == LocationId),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterPrinter_UnknownType_DefaultsUnspecifiedAsync()
    {
        _client.RegisterPrinterAsync(Arg.Any<RegisterPrinterRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PrinterModel { Name = "X" }));

        await RegisterPrinterTool.ExecuteAsync(_auth, _client, name: "X", connectionString: "y", type: "bogus");

        _ = _client.Received(1).RegisterPrinterAsync(
            Arg.Is<RegisterPrinterRequest>(r => r.Type == PrinterType.Unspecified),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    // ---- route_print_job ----

    [Fact]
    public async Task RoutePrintJob_MissingJobType_ReturnsErrorAsync()
    {
        var result = await RoutePrintJobTool.ExecuteAsync(_auth, _client, jobType: "", payloadRef: "s3://x");

        Parse(result).GetProperty("error").GetString().Should().Contain("jobType is required");
    }

    [Fact]
    public async Task RoutePrintJob_MissingPayloadRef_ReturnsErrorAsync()
    {
        var result = await RoutePrintJobTool.ExecuteAsync(_auth, _client, jobType: "receipt", payloadRef: "");

        Parse(result).GetProperty("error").GetString().Should().Contain("payloadRef is required");
    }

    [Fact]
    public async Task RoutePrintJob_HappyPath_SetsRequestAsync()
    {
        _client.RoutePrintJobAsync(Arg.Any<RoutePrintJobRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PrintJobModel
            {
                Id = new GuidValue(JobId),
                JobType = "receipt",
                Status = JobStatus.Queued,
            }));

        var json = Parse(await RoutePrintJobTool.ExecuteAsync(_auth, _client,
            jobType: "receipt", payloadRef: "s3://blob"));

        json.GetProperty("status").GetString().Should().Be("Queued");
        _ = _client.Received(1).RoutePrintJobAsync(
            Arg.Is<RoutePrintJobRequest>(r =>
                r.JobType == "receipt" && r.PayloadRef == "s3://blob" && r.RoutingHints == ""),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }

    // ---- list_print_jobs ----

    [Fact]
    public async Task ListPrintJobs_HappyPath_ReturnsMetadataAsync()
    {
        _client.ListJobsAsync(Arg.Any<ListJobsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListJobsResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 1, PageSize = 20 },
            }));

        var json = Parse(await ListPrintJobsTool.ExecuteAsync(_auth, _client));

        json.GetProperty("totalCount").GetInt32().Should().Be(0);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListPrintJobs_StatusAndPaging_SetsRequestAsync()
    {
        _client.ListJobsAsync(Arg.Any<ListJobsRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new ListJobsResponse
            {
                Metadata = new PaginationMetadata { TotalCount = 0, CurrentPage = 3, PageSize = 5 },
            }));

        await ListPrintJobsTool.ExecuteAsync(_auth, _client, status: "failed", page: 3, pageSize: 5);

        _ = _client.Received(1).ListJobsAsync(
            Arg.Is<ListJobsRequest>(r =>
                r.Status == JobStatus.Failed && r.Pagination.Page == 3 && r.Pagination.PageSize == 5),
            Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
    }
}
