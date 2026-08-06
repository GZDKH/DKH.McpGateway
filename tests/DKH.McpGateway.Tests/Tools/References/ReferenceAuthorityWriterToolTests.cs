using DKH.McpGateway.Application.Tools.References;
using DKH.Platform.Grpc.Common.Types;
using Google.Protobuf.WellKnownTypes;
using CurrencyCrud = DKH.ReferenceService.Contracts.Api.CurrenciesCrud.V1;
using CurrencyMgmt = DKH.ReferenceService.Contracts.Reference.Api.CurrencyManagement.v2;
using CurrencyModel = DKH.ReferenceService.Contracts.Models.Currency.V1.Currency;
using QuantityCrud = DKH.ReferenceService.Contracts.Api.QuantityUnitsCrud.V1;
using QuantityMgmt = DKH.ReferenceService.Contracts.Reference.Api.QuantityUnitManagement.v2;
using QuantityModel = DKH.ReferenceService.Contracts.Models.QuantityUnit.V1.QuantityUnit;
using WeightCrud = DKH.ReferenceService.Contracts.Api.WeightsCrud.V1;
using WeightMgmt = DKH.ReferenceService.Contracts.Reference.Api.WeightManagement.v2;
using WeightModel = DKH.ReferenceService.Contracts.Models.Weight.V1.Weight;

namespace DKH.McpGateway.Tests.Tools.References;

public class ReferenceAuthorityVersionGuardTests
{
    private const string StableId = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public void ManagementReadContracts_UseV2WithoutMutationMethods()
    {
        CurrencyMgmt.CurrencyManagementService.Descriptor.Methods.Select(method => method.Name)
            .Should().Equal("Get", "List");
        QuantityMgmt.QuantityUnitManagementService.Descriptor.Methods.Select(method => method.Name)
            .Should().Equal("Get", "List");
        WeightMgmt.WeightManagementService.Descriptor.Methods.Select(method => method.Name)
            .Should().Equal("Get", "List");
    }

    [Theory]
    [InlineData(null, 7L, "stableId")]
    [InlineData("", 7L, "stableId")]
    [InlineData("not-a-guid", 7L, "stableId")]
    [InlineData(StableId, null, "positive expectedAuthorityVersion")]
    [InlineData(StableId, 0L, "positive expectedAuthorityVersion")]
    [InlineData(StableId, -1L, "positive expectedAuthorityVersion")]
    public void TryGetMutationIdentity_MissingOrInvalidIdentity_FailsClosed(
        string? stableId,
        long? version,
        string expectedError)
    {
        var valid = ReferenceAuthorityVersionGuard.TryGetMutationIdentity(
            stableId, version, out _, out _, out var error);

        valid.Should().BeFalse();
        error.Should().Contain(expectedError);
    }

    [Fact]
    public void TryGetMutationIdentity_ValidIdentity_NormalizesAndReturnsExactVersion()
    {
        var valid = ReferenceAuthorityVersionGuard.TryGetMutationIdentity(
            StableId.ToUpperInvariant(), 7, out var id, out var version, out var error);

        valid.Should().BeTrue();
        id.Value.Should().Be(StableId);
        version.Should().Be(7);
        error.Should().BeEmpty();
    }
}

public class ManageCurrencyAuthorityWriterToolTests
{
    private const string ObservedId = "11111111-1111-1111-1111-111111111111";
    private const string ReplacementId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient _management =
        Substitute.For<CurrencyMgmt.CurrencyManagementService.CurrencyManagementServiceClient>();
    private readonly CurrencyCrud.CurrenciesCrudService.CurrenciesCrudServiceClient _crud =
        Substitute.For<CurrencyCrud.CurrenciesCrudService.CurrenciesCrudServiceClient>();

    [Fact]
    public async Task Create_UsesCanonicalCrudAndReturnsStableIdentityAsync()
    {
        _crud.CreateCurrencyAsync(
                Arg.Any<CurrencyCrud.CreateCurrencyRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CurrencyCrud.CreateCurrencyResponse
            {
                Currency = Currency(ObservedId, 1),
            }));

        var result = await ExecuteAsync("create", json: /*lang=json,strict*/ "{\"code\":\"USD\"}");

        result.Should().Contain(ObservedId).And.Contain("authorityVersion").And.Contain("\"1\"");
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ForwardsCallerObservedIdAndExactVersionAsync()
    {
        CurrencyCrud.UpdateCurrencyRequest? captured = null;
        _crud.UpdateCurrencyAsync(
                Arg.Do<CurrencyCrud.UpdateCurrencyRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CurrencyCrud.UpdateCurrencyResponse
            {
                Currency = Currency(ObservedId, 8),
            }));

        var result = await ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"id\":{\"value\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\"},\"code\":\"USD\",\"expectedAuthorityVersion\":999}",
            stableId: ObservedId,
            expectedAuthorityVersion: 7);

        captured!.Id.Value.Should().Be(ObservedId);
        captured.ExpectedAuthorityVersion.Should().Be(7);
        result.Should().Contain("\"8\"");
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ObservedIdA_CannotRetargetToReplacementIdBWithSameCodeAndVersionAsync()
    {
        SetupManagementGet(ReplacementId, 7);
        CurrencyCrud.UpdateCurrencyRequest? captured = null;
        _crud.UpdateCurrencyAsync(
                Arg.Do<CurrencyCrud.UpdateCurrencyRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CurrencyCrud.UpdateCurrencyResponse
            {
                Currency = Currency(ObservedId, 8),
            }));

        _ = await ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"code\":\"USD\"}",
            stableId: ObservedId,
            expectedAuthorityVersion: 7);

        captured!.Id.Value.Should().Be(ObservedId).And.NotBe(ReplacementId);
        captured.ExpectedAuthorityVersion.Should().Be(7);
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, 7L, "stableId")]
    [InlineData(ObservedId, 0L, "positive expectedAuthorityVersion")]
    public async Task Update_IncompleteIdentity_DoesNotReadOrWriteAsync(
        string? stableId,
        long? expectedAuthorityVersion,
        string expectedError)
    {
        var result = await ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"code\":\"USD\"}",
            stableId: stableId,
            expectedAuthorityVersion: expectedAuthorityVersion);

        result.Should().Contain(expectedError);
        _management.ReceivedCalls().Should().BeEmpty();
        _crud.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_CrudConflict_PropagatesWithoutRetryAsync()
    {
        _crud.UpdateCurrencyAsync(
                Arg.Any<CurrencyCrud.UpdateCurrencyRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<CurrencyCrud.UpdateCurrencyResponse>(
                StatusCode.Aborted, "stale authority version"));

        var act = () => ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"code\":\"USD\"}",
            stableId: ObservedId,
            expectedAuthorityVersion: 7);

        await act.Should().ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.Aborted);
        _crud.ReceivedCalls().Count().Should().Be(1);
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ObservedIdA_CannotRetargetToReplacementIdBWithSameCodeAndVersionAsync()
    {
        SetupManagementGet(ReplacementId, 7);
        CurrencyCrud.DeleteCurrencyRequest? captured = null;
        _crud.DeleteCurrencyAsync(
                Arg.Do<CurrencyCrud.DeleteCurrencyRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

        var result = await ExecuteAsync(
            "delete", code: "USD", stableId: ObservedId, expectedAuthorityVersion: 7);

        result.Should().Contain("true");
        captured!.Id.Value.Should().Be(ObservedId).And.NotBe(ReplacementId);
        captured.ExpectedAuthorityVersion.Should().Be(7);
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ReturnsStableIdAndNativeVersionAsync()
    {
        SetupManagementGet(ObservedId, 7);

        var result = await ExecuteAsync("get", code: "USD");

        result.Should().Contain(ObservedId).And.Contain("authorityVersion").And.Contain("\"7\"");
    }

    private Task<string> ExecuteAsync(
        string action,
        string? json = null,
        string? code = null,
        string? stableId = null,
        long? expectedAuthorityVersion = null)
        => ManageCurrencyTool.ExecuteAsync(
            _auth, _management, _crud, action, json, code, stableId, expectedAuthorityVersion);

    private void SetupManagementGet(string id, long authorityVersion)
        => _management.GetAsync(
                Arg.Any<CurrencyMgmt.GetCurrencyRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new CurrencyMgmt.CurrencyModel
            {
                Id = new GuidValue(id),
                Code = "USD",
                AuthorityVersion = authorityVersion,
            }));

    private static CurrencyModel Currency(string id, long authorityVersion) => new()
    {
        Id = new GuidValue(id),
        Code = "USD",
        AuthorityVersion = authorityVersion,
    };
}

public class ManageQuantityUnitAuthorityWriterToolTests
{
    private const string StableId = "22222222-2222-2222-2222-222222222222";
    private const string ReplacementId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly QuantityMgmt.QuantityUnitManagementService.QuantityUnitManagementServiceClient _management =
        Substitute.For<QuantityMgmt.QuantityUnitManagementService.QuantityUnitManagementServiceClient>();
    private readonly QuantityCrud.QuantityUnitsCrudService.QuantityUnitsCrudServiceClient _crud =
        Substitute.For<QuantityCrud.QuantityUnitsCrudService.QuantityUnitsCrudServiceClient>();

    [Fact]
    public async Task Create_UsesCanonicalCrudAndReturnsStableIdentityAsync()
    {
        _crud.CreateQuantityUnitAsync(
                Arg.Any<QuantityCrud.CreateQuantityUnitRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new QuantityCrud.CreateQuantityUnitResponse
            {
                QuantityUnit = QuantityUnit(1),
            }));

        var result = await ExecuteAsync("create", json: /*lang=json,strict*/ "{\"code\":\"PIECE\"}");

        result.Should().Contain(StableId).And.Contain("\"1\"");
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ForwardsCallerObservedIdAndExactVersionAsync()
    {
        QuantityCrud.UpdateQuantityUnitRequest? captured = null;
        _crud.UpdateQuantityUnitAsync(
                Arg.Do<QuantityCrud.UpdateQuantityUnitRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new QuantityCrud.UpdateQuantityUnitResponse
            {
                QuantityUnit = QuantityUnit(12),
            }));

        var result = await ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"code\":\"PIECE\",\"expectedAuthorityVersion\":999}",
            stableId: StableId,
            expectedAuthorityVersion: 11);

        captured!.Id.Value.Should().Be(StableId);
        captured.ExpectedAuthorityVersion.Should().Be(11);
        result.Should().Contain("\"12\"");
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_MissingStableId_FailsClosedWithoutAnyGrpcCallAsync()
    {
        var result = await ExecuteAsync("delete", expectedAuthorityVersion: 11);

        result.Should().Contain("stableId");
        _management.ReceivedCalls().Should().BeEmpty();
        _crud.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ObservedIdA_CannotRetargetToReplacementIdBWithSameCodeAndVersionAsync()
    {
        _management.GetAsync(
                Arg.Any<QuantityMgmt.GetQuantityUnitRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new QuantityMgmt.QuantityUnitModel
            {
                Id = new GuidValue(ReplacementId),
                Code = "PIECE",
                AuthorityVersion = 11,
            }));
        QuantityCrud.DeleteQuantityUnitRequest? captured = null;
        _crud.DeleteQuantityUnitAsync(
                Arg.Do<QuantityCrud.DeleteQuantityUnitRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

        _ = await ExecuteAsync(
            "delete", code: "PIECE", stableId: StableId, expectedAuthorityVersion: 11);

        captured!.Id.Value.Should().Be(StableId).And.NotBe(ReplacementId);
        captured.ExpectedAuthorityVersion.Should().Be(11);
        _management.ReceivedCalls().Should().BeEmpty();
    }

    private Task<string> ExecuteAsync(
        string action,
        string? json = null,
        string? code = null,
        string? stableId = null,
        long? expectedAuthorityVersion = null)
        => ManageQuantityUnitTool.ExecuteAsync(
            _auth, _management, _crud, action, json, code, stableId, expectedAuthorityVersion);

    private static QuantityModel QuantityUnit(long authorityVersion) => new()
    {
        Id = new GuidValue(StableId),
        Code = "PIECE",
        AuthorityVersion = authorityVersion,
    };
}

public class ManageWeightAuthorityWriterToolTests
{
    private const string StableId = "33333333-3333-3333-3333-333333333333";
    private const string ReplacementId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
    private readonly IApiKeyContext _auth = ApiKeyContextMocks.FullAccess();
    private readonly WeightMgmt.WeightManagementService.WeightManagementServiceClient _management =
        Substitute.For<WeightMgmt.WeightManagementService.WeightManagementServiceClient>();
    private readonly WeightCrud.WeightsCrudService.WeightsCrudServiceClient _crud =
        Substitute.For<WeightCrud.WeightsCrudService.WeightsCrudServiceClient>();

    [Fact]
    public async Task Create_UsesCanonicalCrudAndReturnsStableIdentityAsync()
    {
        _crud.CreateWeightAsync(
                Arg.Any<WeightCrud.CreateWeightRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new WeightCrud.CreateWeightResponse
            {
                Weight = Weight(1),
            }));

        var result = await ExecuteAsync("create", json: /*lang=json,strict*/ "{\"code\":\"KG\"}");

        result.Should().Contain(StableId).And.Contain("\"1\"");
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ForwardsCallerObservedIdAndExactVersionAsync()
    {
        WeightCrud.UpdateWeightRequest? captured = null;
        _crud.UpdateWeightAsync(
                Arg.Do<WeightCrud.UpdateWeightRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new WeightCrud.UpdateWeightResponse
            {
                Weight = Weight(22),
            }));

        var result = await ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"code\":\"KG\",\"expectedAuthorityVersion\":999}",
            stableId: StableId,
            expectedAuthorityVersion: 21);

        captured!.Id.Value.Should().Be(StableId);
        captured.ExpectedAuthorityVersion.Should().Be(21);
        result.Should().Contain("\"22\"");
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Update_CrudConflict_PropagatesWithoutRetryAsync()
    {
        _crud.UpdateWeightAsync(
                Arg.Any<WeightCrud.UpdateWeightRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateFaultedAsyncUnaryCall<WeightCrud.UpdateWeightResponse>(
                StatusCode.Aborted, "stale authority version"));

        var act = () => ExecuteAsync(
            "update",
            json: /*lang=json,strict*/ "{\"code\":\"KG\"}",
            stableId: StableId,
            expectedAuthorityVersion: 21);

        await act.Should().ThrowAsync<RpcException>()
            .Where(exception => exception.StatusCode == StatusCode.Aborted);
        _crud.ReceivedCalls().Count().Should().Be(1);
        _management.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_MissingVersion_FailsClosedWithoutAnyGrpcCallAsync()
    {
        var result = await ExecuteAsync("delete", stableId: StableId);

        result.Should().Contain("positive expectedAuthorityVersion");
        _management.ReceivedCalls().Should().BeEmpty();
        _crud.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ObservedIdA_CannotRetargetToReplacementIdBWithSameCodeAndVersionAsync()
    {
        _management.GetAsync(
                Arg.Any<WeightMgmt.GetWeightRequest>(),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new WeightMgmt.WeightModel
            {
                Id = new GuidValue(ReplacementId),
                Code = "KG",
                AuthorityVersion = 21,
            }));
        WeightCrud.DeleteWeightRequest? captured = null;
        _crud.DeleteWeightAsync(
                Arg.Do<WeightCrud.DeleteWeightRequest>(request => captured = request.Clone()),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new Empty()));

        _ = await ExecuteAsync(
            "delete", code: "KG", stableId: StableId, expectedAuthorityVersion: 21);

        captured!.Id.Value.Should().Be(StableId).And.NotBe(ReplacementId);
        captured.ExpectedAuthorityVersion.Should().Be(21);
        _management.ReceivedCalls().Should().BeEmpty();
    }

    private Task<string> ExecuteAsync(
        string action,
        string? json = null,
        string? code = null,
        string? stableId = null,
        long? expectedAuthorityVersion = null)
        => ManageWeightTool.ExecuteAsync(
            _auth, _management, _crud, action, json, code, stableId, expectedAuthorityVersion);

    private static WeightModel Weight(long authorityVersion) => new()
    {
        Id = new GuidValue(StableId),
        Code = "KG",
        AuthorityVersion = authorityVersion,
    };
}
