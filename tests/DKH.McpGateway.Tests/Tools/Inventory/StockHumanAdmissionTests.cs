using System.Security.Claims;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;
using Xunit.Abstractions;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class StockHumanAdmissionTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("availability", "service-account")]
    [InlineData("availability", "unnamed")]
    [InlineData("reserve", "service-account")]
    [InlineData("reserve", "unnamed")]
    [InlineData("get", "service-account")]
    [InlineData("get", "unnamed")]
    [InlineData("get_operation", "service-account")]
    [InlineData("get_operation", "unnamed")]
    [InlineData("release", "service-account")]
    [InlineData("release", "unnamed")]
    [InlineData("bind_order", "service-account")]
    [InlineData("bind_order", "unnamed")]
    [InlineData("confirm", "service-account")]
    [InlineData("confirm", "unnamed")]
    public async Task NonHumanPrincipal_IsRefusedBeforeAnyNativeRpcAsync(string action, string kind)
    {
        var f = new InventoryToolFixture();
        var identity = (ClaimsIdentity)f.Http.User.Identity!;
        var username = identity.FindFirst("preferred_username");
        username.Should().NotBeNull();
        identity.RemoveClaim(username!);
        if (kind == "service-account")
        {
            // A valid-shaped GUID subject, original bearer and privileged role
            // are deliberately retained; none of them makes a machine HUMAN.
            identity.AddClaim(new Claim("preferred_username", "service-account-mcp-gateway"));
        }

        identity.AddClaim(new Claim(ClaimTypes.Role, "mcp-admin"));
        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(action switch
        {
            "availability" => new ExactStockAvailabilityModel { StockScope = InventoryToolFixture.Scope(), AvailableQuantity = 1 },
            "get" => InventoryToolFixture.Reservation(),
            "reserve" => InventoryToolFixture.Operation("reserve", InventoryToolFixture.ReserveId),
            "confirm" => InventoryToolFixture.Operation("dispatch"),
            "bind_order" => InventoryToolFixture.Operation("bind_order"),
            _ => InventoryToolFixture.Operation(),
        });
        var invoke = async () => action == "availability"
            ? await f.QueryAsync()
            : await f.ReservationAsync(action, ManageReservationToolTests.Request(action));
        try
        {
            await invoke.Should().ThrowAsync<UnauthorizedAccessException>();
            f.Transport.Calls.Should().BeEmpty();
        }
        finally
        {
            output.WriteLine($"Controlled native calls={f.Transport.Calls.Count}; paths={string.Join(',', f.Transport.Calls.Select(x => x.Path))}");
        }
    }
}
