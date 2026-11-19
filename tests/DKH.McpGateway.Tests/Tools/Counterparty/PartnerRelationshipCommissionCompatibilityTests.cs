using DKH.CounterpartyService.Contracts.Counterparty.Api.PartnerRelationship.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Models.v1;
using DKH.McpGateway.Application.Tools.Counterparty;
using Google.Protobuf;

namespace DKH.McpGateway.Tests.Tools.Counterparty;

public sealed class PartnerRelationshipCommissionCompatibilityTests
{
    private const string CounterpartyId = "11111111-1111-1111-1111-111111111111";
    private const string RelationshipId = "22222222-2222-2222-2222-222222222222";
    private const string DocumentId = "33333333-3333-3333-3333-333333333333";
    private readonly PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient _client =
        Substitute.For<PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient>();

    [Theory]
    [InlineData("0", null, null, null)]
    [InlineData(null, "0", null, null)]
    [InlineData(null, null, "USD", null)]
    [InlineData(null, null, null, "private commission note")]
    public async Task Activate_CommissionInput_ReturnsExplicitErrorWithoutRpcAsync(
        string? rate, string? fee, string? currency, string? note)
    {
        var result = await ActivatePartnerRelationshipTool.ExecuteAsync(
            ApiKeyContextMocks.FullAccess(), _client, CounterpartyId, "GOLD", DocumentId,
            commissionPercentRate: rate, commissionFlatFee: fee,
            commissionCurrency: currency, commissionNote: note);

        result.Should().Contain("Legacy commission terms are retired")
            .And.Contain("PartnerCommercialAgreement")
            .And.NotContain("private commission note");
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, null, null, null, null)]
    [InlineData(false, "0", null, null, null)]
    [InlineData(false, null, "0", null, null)]
    [InlineData(false, null, null, "USD", null)]
    [InlineData(false, null, null, null, "private commission note")]
    public async Task Update_CommissionInputOrClear_ReturnsExplicitErrorWithoutRpcAsync(
        bool change, string? rate, string? fee, string? currency, string? note)
    {
        var result = await UpdatePartnerRelationshipTermsTool.ExecuteAsync(
            ApiKeyContextMocks.FullAccess(), _client, RelationshipId,
            changeCommissionTerms: change, commissionPercentRate: rate, commissionFlatFee: fee,
            commissionCurrency: currency, commissionNote: note);

        result.Should().Contain("Legacy commission terms are retired")
            .And.Contain("PartnerCommercialAgreement")
            .And.NotContain("private commission note");
        _client.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" \t")]
    public async Task NonCommissionOperations_PreserveInputsAndCancellationWithoutLegacyWireFieldsAsync(string? empty)
    {
        using var cancellation = new CancellationTokenSource();
        ActivatePartnerRelationshipRequest? activation = null;
        UpdatePartnerRelationshipTermsRequest? update = null;
        _client.ActivatePartnerRelationshipAsync(
                Arg.Do<ActivatePartnerRelationshipRequest>(request =>
                    activation = ActivatePartnerRelationshipRequest.Parser.ParseFrom(request.ToByteArray())),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), cancellation.Token)
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PartnerRelationshipModel()));
        _client.UpdatePartnerRelationshipTermsAsync(
                Arg.Do<UpdatePartnerRelationshipTermsRequest>(request =>
                    update = UpdatePartnerRelationshipTermsRequest.Parser.ParseFrom(request.ToByteArray())),
                Arg.Any<Metadata>(), Arg.Any<DateTime?>(), cancellation.Token)
            .Returns(GrpcTestHelpers.CreateAsyncUnaryCall(new PartnerRelationshipModel()));

        var activated = await ActivatePartnerRelationshipTool.ExecuteAsync(
            ApiKeyContextMocks.FullAccess(), _client, CounterpartyId, "GOLD", DocumentId,
            commissionPercentRate: empty, commissionFlatFee: empty,
            commissionCurrency: empty, commissionNote: empty,
            isExclusive: true, exclusivityScope: "scope", exclusivityNote: "note",
            isVerifiedPartnerBadgeEligible: true, selfClaimedReviewsDisabled: true,
            cancellationToken: cancellation.Token);
        var updated = await UpdatePartnerRelationshipTermsTool.ExecuteAsync(
            ApiKeyContextMocks.FullAccess(), _client, RelationshipId, tier: "GOLD",
            commissionPercentRate: empty, commissionFlatFee: empty,
            commissionCurrency: empty, commissionNote: empty,
            changeExclusivityTerms: true, isExclusive: true,
            exclusivityScope: "scope", exclusivityNote: "note",
            isVerifiedPartnerBadgeEligible: false, selfClaimedReviewsDisabled: false,
            changeContractDocumentIds: true, contractDocumentIds: DocumentId,
            cancellationToken: cancellation.Token);

        activated.Should().NotContain("\"error\"");
        updated.Should().NotContain("\"error\"");
        activation.Should().NotBeNull();
        activation!.CounterpartyId.Value.Should().Be(CounterpartyId);
        activation.Tier.Should().Be(update!.Tier);
        activation.ContractDocumentIds.Select(id => id.Value).Should().Equal(DocumentId);
        activation.IsExclusive.Should().BeTrue();
        activation.ExclusivityScope.Should().Be("scope");
        activation.ExclusivityNote.Should().Be("note");
        activation.IsVerifiedPartnerBadgeEligible.Should().BeTrue();
        activation.SelfClaimedReviewsDisabled.Should().BeTrue();
        update.PartnerRelationshipId.Value.Should().Be(RelationshipId);
        update.ChangeExclusivityTerms.Should().BeTrue();
        update.IsExclusive.Should().BeTrue();
        update.ExclusivityScope.Should().Be("scope");
        update.ExclusivityNote.Should().Be("note");
        update.IsVerifiedPartnerBadgeEligible.Should().BeFalse();
        update.SelfClaimedReviewsDisabled.Should().BeFalse();
        update.ChangeContractDocumentIds.Should().BeTrue();
        update.ContractDocumentIds.Select(id => id.Value).Should().Equal(DocumentId);
        AssertNoLegacyCommissionFields(activation);
        AssertNoLegacyCommissionFields(update);
        _client.ReceivedCalls().Count().Should().Be(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WritePermission_IsRequiredBeforeCommissionValidationAsync(bool update)
    {
        Func<Task> action = update
            ? () => UpdatePartnerRelationshipTermsTool.ExecuteAsync(
                ApiKeyContextMocks.ReadOnly(), _client, RelationshipId, changeCommissionTerms: true)
            : () => ActivatePartnerRelationshipTool.ExecuteAsync(
                ApiKeyContextMocks.ReadOnly(), _client, CounterpartyId, "GOLD", DocumentId,
                commissionPercentRate: "1");

        await action.Should().ThrowAsync<UnauthorizedAccessException>();
        _client.ReceivedCalls().Should().BeEmpty();
    }

    private static void AssertNoLegacyCommissionFields(IMessage request)
    {
        foreach (var name in new[] { "commission_percent_rate", "commission_flat_fee", "commission_currency", "commission_note" })
        {
            request.Descriptor.FindFieldByName(name).Accessor.GetValue(request).Should().BeNull();
        }

        request.Descriptor.FindFieldByName("change_commission_terms")
            ?.Accessor.GetValue(request).Should().Be(false);
    }
}
