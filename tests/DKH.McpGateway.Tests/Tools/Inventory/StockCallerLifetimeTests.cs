using System.Security.Claims;
using DKH.InventoryService.Contracts.Inventory.Models.Reservation.v2;

namespace DKH.McpGateway.Tests.Tools.Inventory;

public sealed class StockCallerLifetimeTests
{
    [Theory]
    [InlineData("principal")]
    [InlineData("claims")]
    [InlineData("issuer")]
    [InlineData("claim_property")]
    [InlineData("bearer")]
    [InlineData("workspace")]
    [InlineData("api_key")]
    public async Task SameActorChangedFrame_AfterAwaitCannotDiscloseBodyAsync(string change)
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = async (_, _) =>
        {
            await Task.Yield();
            Change(f, change);
            return Availability();
        };
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<UnauthorizedAccessException>();
        f.Transport.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData("bearer")]
    [InlineData("claims")]
    [InlineData("workspace")]
    public async Task ChangedFrame_AfterNativeErrorCannotDisclosePreviousErrorAsync(string change)
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = async (_, _) =>
        {
            await Task.Yield();
            Change(f, change);
            throw new RpcException(new Status(StatusCode.NotFound, "previous frame protected outcome"));
        };
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ChangedBearerDuringCallDisposal_CannotDiscloseBodyAsync()
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = (_, _) => Task.FromResult<object>(Availability());
        f.Transport.OnDispose = () => Change(f, "bearer");
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UnchangedAuthorizedNativeError_PreservesOriginalExceptionAsync()
    {
        var f = new InventoryToolFixture();
        var original = new RpcException(new Status(StatusCode.PermissionDenied, "bounded native refusal"));
        f.Transport.RespondAsync = (_, _) => Task.FromException<object>(original);
        var error = await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<RpcException>();
        error.Which.Should().BeSameAs(original);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OriginalCallerCancellationOrAbort_AfterAwaitIsNotSuccessAsync(bool requestAbort)
    {
        var f = new InventoryToolFixture();
        using var cancellation = new CancellationTokenSource();
        if (requestAbort)
        {
            f.Http.RequestAborted = cancellation.Token;
        }

        f.Transport.RespondAsync = async (_, _) =>
        {
            await Task.Yield();
            cancellation.Cancel();
            return Availability();
        };
        await ((Func<Task>)(async () => await f.QueryAsync(cancellationToken: requestAbort ? default : cancellation.Token))).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task OriginalExpiryDuringAwait_IsNotRenewedAsync()
    {
        var f = new InventoryToolFixture();
        var identity = (ClaimsIdentity)f.Http.User.Identity!;
        identity.RemoveClaim(identity.FindFirst("exp")!);
        identity.AddClaim(new Claim("exp", DateTimeOffset.UtcNow.AddSeconds(2).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture)));
        f.Transport.RespondAsync = async (_, options) =>
        {
            var delay = options.Deadline!.Value - DateTime.UtcNow + TimeSpan.FromMilliseconds(25);
            await Task.Delay(delay);
            return Availability();
        };
        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData("transport")]
    [InlineData("principal")]
    [InlineData("bearer")]
    [InlineData("expired")]
    [InlineData("workspace")]
    public async Task MissingOriginalHumanContext_NeverUsesHistoricalOrSyntheticIdentityAsync(string missing)
    {
        var f = new InventoryToolFixture();
        switch (missing)
        {
            case "transport":
                f.Accessor.HttpContext = null;
                break;
            case "principal":
                f.Http.User = new ClaimsPrincipal();
                break;
            case "bearer":
                f.Http.Request.Headers.Remove("Authorization");
                break;
            case "workspace":
                f.Http.Request.Headers.Remove("X-Workspace-Id");
                break;
            case "expired":
                var identity = (ClaimsIdentity)f.Http.User.Identity!;
                identity.RemoveClaim(identity.FindFirst("exp")!);
                identity.AddClaim(new Claim("exp", "1"));
                break;
        }

        await ((Func<Task>)(async () => await f.QueryAsync())).Should().ThrowAsync<UnauthorizedAccessException>();
        f.Transport.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task NativeMutationResponse_IsFencedBySameOriginalCallerAsync()
    {
        var f = new InventoryToolFixture();
        f.Transport.RespondAsync = async (_, _) =>
        {
            await Task.Yield();
            Change(f, "claims");
            return InventoryToolFixture.Operation();
        };
        await ((Func<Task>)(async () => await f.ReservationAsync("release", ManageReservationToolTests.Request("release")))).Should().ThrowAsync<UnauthorizedAccessException>();
        f.Transport.Calls.Should().ContainSingle();
    }

    private static ExactStockAvailabilityModel Availability() => new() { StockScope = InventoryToolFixture.Scope(), AvailableQuantity = 3 };

    private static void Change(InventoryToolFixture f, string change)
    {
        var identity = (ClaimsIdentity)f.Http.User.Identity!;
        switch (change)
        {
            case "principal":
                f.Http.User = InventoryToolFixture.Principal();
                break;
            case "claims":
                identity.AddClaim(new Claim("role", "changed"));
                break;
            case "issuer":
                identity.RemoveClaim(identity.FindFirst("iss")!);
                identity.AddClaim(new Claim("iss", "https://foreign.example.test"));
                break;
            case "claim_property":
                identity.FindFirst("sub")!.Properties.Add("changed", "true");
                break;
            case "bearer":
                f.Http.Request.Headers.Authorization = "Bearer changed-same-actor-token";
                break;
            case "workspace":
                f.Http.Request.Headers["X-Workspace-Id"] = Guid.NewGuid().ToString();
                break;
            case "api_key":
                f.Auth.ApiKeyId.Returns("different-key");
                break;
        }
    }
}
