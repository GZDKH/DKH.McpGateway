using DKH.InventoryService.Contracts.Inventory.Api.StockReservation.v2;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using DKH.McpGateway.Application;
using DKH.McpGateway.Application.Tools.Inventory;
using DKH.Platform.Grpc.Client;
using DKH.Platform.Identity;
using DKH.Platform.Identity.Interceptors;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class InventoryIdentityAndRegistrationTests
{
    [Fact]
    public async Task GeneratedClient_UsesExistingPlatformInterceptorToForwardOriginalBearerAsync()
    {
        var f = new InventoryToolFixture();
        var user = Substitute.For<IPlatformCurrentUser>();
        user.IsAuthenticated.Returns(true);
        var interceptor = new PlatformUserIdentityPropagationInterceptor(user,
            Options.Create(new PlatformCurrentUserOptions { ForwardAuthorizationHeader = true }), f.Accessor);
        var client = new StockReservationService.StockReservationServiceClient(f.Transport.Intercept(interceptor));
        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(new ExactStockAvailabilityModel { StockScope = InventoryToolFixture.Scope(), AvailableQuantity = 1 });
        await QueryStockTool.ExecuteAsync(f.Auth, client, f.Accessor, "availability", InventoryToolFixture.WorkspaceId,
            InventoryToolFixture.OwnerId, InventoryToolFixture.WarehouseId, InventoryToolFixture.SellableId);
        f.Transport.Calls.Should().ContainSingle();
        var headers = f.Transport.Calls[0].Options.Headers!;
        headers.Single(x => x.Key == "authorization").Value.Should().Be(f.Http.Request.Headers.Authorization.ToString());
        headers.Single(x => x.Key == "x-workspace-id").Value.Should().Be(InventoryToolFixture.WorkspaceId);
    }

    [Fact]
    public void EndpointRegistration_ContainsOnlyExactInventoryClient()
    {
        var grpc = Substitute.For<IPlatformGrpcClientBuilder>();
        grpc.AddMcpGatewayEndpoints();
        var inventoryTypes = grpc.ReceivedCalls().Select(x => x.GetMethodInfo())
            .Where(x => x.IsGenericMethod).SelectMany(x => x.GetGenericArguments())
            .Where(x => x.Namespace?.StartsWith("DKH.InventoryService.Contracts", StringComparison.Ordinal) == true).ToArray();
        inventoryTypes.Should().Equal(typeof(StockReservationService.StockReservationServiceClient));
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    [InlineData("appsettings.Docker.json")]
    public void ConfiguredEndpoints_MatchActualRegistrationAfterEnvironmentInheritance(string file)
    {
        var grpc = Substitute.For<IPlatformGrpcClientBuilder>();
        grpc.AddMcpGatewayEndpoints();
        var registered = grpc.ReceivedCalls().Where(x => x.GetMethodInfo().Name == "AddEndpointFromConfiguration")
            .Select(x => x.GetArguments().OfType<string>().FirstOrDefault() ?? x.GetMethodInfo().GetGenericArguments().Single().Name).ToArray();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DKH.McpGateway.slnx")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "DKH.McpGateway.Api");
        using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "appsettings.json")));
        using var environment = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, file)));
        var defaults = baseline.RootElement.GetProperty("Platform").GetProperty("Grpc").GetProperty("Endpoints").EnumerateObject().Select(x => x.Name);
        var overrides = environment.RootElement.GetProperty("Platform").GetProperty("Grpc").GetProperty("Endpoints").EnumerateObject().Select(x => x.Name);
        defaults.Union(overrides, StringComparer.Ordinal).Should().BeEquivalentTo(registered);
        overrides.Should().NotContain(["StockQueryServiceClient", "StockManagementServiceClient", "LowStockAlertServiceClient"]);
        var stockUrl = environment.RootElement.GetProperty("Platform").GetProperty("Grpc").GetProperty("Endpoints")
            .GetProperty("StockReservationServiceClient").GetProperty("Url").GetString();
        new Uri(stockUrl!).Port.Should().Be(5014, "the actual Inventory service endpoint is 5014; 5013 is MCP");
    }

    [Theory]
    [InlineData("query_stock")]
    [InlineData("manage_stock")]
    [InlineData("manage_reservation")]
    [InlineData("manage_stock_alert")]
    public void StockTools_RemainPrivateAdminSurface(string name)
        => new McpToolSurface().IsStorefrontPublic(name).Should().BeFalse();
}
