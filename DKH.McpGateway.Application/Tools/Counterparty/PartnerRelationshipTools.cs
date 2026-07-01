using DKH.CounterpartyService.Contracts.Counterparty.Api.PartnerRelationship.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Models.v1;

namespace DKH.McpGateway.Application.Tools.Counterparty;

[McpServerToolType]
public static class ActivatePartnerRelationshipTool
{
    [McpServerTool(Name = "activate_partner_relationship"), Description("Activate a partner relationship for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("CounterpartyTier")] string tier,
        [Description("Verified contract document IDs as JSON array or CSV")] string contractDocumentIds,
        [Description("Commission percent rate (optional)")] string? commissionPercentRate = null,
        [Description("Commission flat fee (optional)")] string? commissionFlatFee = null,
        [Description("Commission currency (optional)")] string? commissionCurrency = null,
        [Description("Commission note (optional)")] string? commissionNote = null,
        [Description("Whether the relationship is exclusive")] bool isExclusive = false,
        [Description("Exclusivity scope (optional)")] string? exclusivityScope = null,
        [Description("Exclusivity note (optional)")] string? exclusivityNote = null,
        [Description("Verified partner badge eligibility flag")] bool isVerifiedPartnerBadgeEligible = false,
        [Description("Disable self-claimed reviews flag")] bool selfClaimedReviewsDisabled = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!CounterpartyToolInput.TryParseEnum<CounterpartyTier>(tier, out var parsedTier))
        {
            return McpProtoHelper.FormatError("tier is invalid");
        }

        var request = new ActivatePartnerRelationshipRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Tier = parsedTier,
            CommissionPercentRate = CounterpartyToolInput.OptionalString(commissionPercentRate),
            CommissionFlatFee = CounterpartyToolInput.OptionalString(commissionFlatFee),
            CommissionCurrency = CounterpartyToolInput.OptionalString(commissionCurrency),
            CommissionNote = CounterpartyToolInput.OptionalString(commissionNote),
            IsExclusive = isExclusive,
            ExclusivityScope = CounterpartyToolInput.OptionalString(exclusivityScope),
            ExclusivityNote = CounterpartyToolInput.OptionalString(exclusivityNote),
            IsVerifiedPartnerBadgeEligible = isVerifiedPartnerBadgeEligible,
            SelfClaimedReviewsDisabled = selfClaimedReviewsDisabled,
        };
        request.ContractDocumentIds.Add(CounterpartyToolInput.ParseGuidList(contractDocumentIds, nameof(contractDocumentIds), out var error));
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ActivatePartnerRelationshipAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapPartnerRelationship(response));
    }
}

[McpServerToolType]
public static class SuspendPartnerRelationshipTool
{
    [McpServerTool(Name = "suspend_partner_relationship"), Description("Suspend an active partner relationship.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Partner relationship ID")] string partnerRelationshipId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.SuspendPartnerRelationshipAsync(
            new SuspendPartnerRelationshipRequest { PartnerRelationshipId = CounterpartyToolInput.ToGuid(partnerRelationshipId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapPartnerRelationship(response));
    }
}

[McpServerToolType]
public static class ReactivatePartnerRelationshipTool
{
    [McpServerTool(Name = "reactivate_partner_relationship"), Description("Reactivate a suspended partner relationship.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Partner relationship ID")] string partnerRelationshipId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.ReactivatePartnerRelationshipAsync(
            new ReactivatePartnerRelationshipRequest { PartnerRelationshipId = CounterpartyToolInput.ToGuid(partnerRelationshipId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapPartnerRelationship(response));
    }
}

[McpServerToolType]
public static class TerminatePartnerRelationshipTool
{
    [McpServerTool(Name = "terminate_partner_relationship"), Description("Terminate a partner relationship.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Partner relationship ID")] string partnerRelationshipId,
        [Description("Termination reason (optional)")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var response = await client.TerminatePartnerRelationshipAsync(
            new TerminatePartnerRelationshipRequest
            {
                PartnerRelationshipId = CounterpartyToolInput.ToGuid(partnerRelationshipId),
                Reason = CounterpartyToolInput.OptionalString(reason),
            },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapPartnerRelationship(response));
    }
}

[McpServerToolType]
public static class UpdatePartnerRelationshipTermsTool
{
    [McpServerTool(Name = "update_partner_relationship_terms"), Description("Update mutable commercial terms on a partner relationship.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Partner relationship ID")] string partnerRelationshipId,
        [Description("CounterpartyTier (optional)")] string? tier = null,
        [Description("Replace commission terms")] bool changeCommissionTerms = false,
        [Description("Commission percent rate (optional)")] string? commissionPercentRate = null,
        [Description("Commission flat fee (optional)")] string? commissionFlatFee = null,
        [Description("Commission currency (optional)")] string? commissionCurrency = null,
        [Description("Commission note (optional)")] string? commissionNote = null,
        [Description("Replace exclusivity terms")] bool changeExclusivityTerms = false,
        [Description("Whether relationship is exclusive")] bool isExclusive = false,
        [Description("Exclusivity scope (optional)")] string? exclusivityScope = null,
        [Description("Exclusivity note (optional)")] string? exclusivityNote = null,
        [Description("Verified partner badge eligibility override (optional)")] bool? isVerifiedPartnerBadgeEligible = null,
        [Description("Self-claimed reviews disabled override (optional)")] bool? selfClaimedReviewsDisabled = null,
        [Description("Replace contract document IDs")] bool changeContractDocumentIds = false,
        [Description("Contract document IDs as JSON array or CSV")] string? contractDocumentIds = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        var request = new UpdatePartnerRelationshipTermsRequest
        {
            PartnerRelationshipId = CounterpartyToolInput.ToGuid(partnerRelationshipId),
            Tier = CounterpartyToolInput.ParseOptionalEnum<CounterpartyTier>(tier, out var error),
            ChangeCommissionTerms = changeCommissionTerms,
            CommissionPercentRate = CounterpartyToolInput.OptionalString(commissionPercentRate),
            CommissionFlatFee = CounterpartyToolInput.OptionalString(commissionFlatFee),
            CommissionCurrency = CounterpartyToolInput.OptionalString(commissionCurrency),
            CommissionNote = CounterpartyToolInput.OptionalString(commissionNote),
            ChangeExclusivityTerms = changeExclusivityTerms,
            IsExclusive = isExclusive,
            ExclusivityScope = CounterpartyToolInput.OptionalString(exclusivityScope),
            ExclusivityNote = CounterpartyToolInput.OptionalString(exclusivityNote),
            IsVerifiedPartnerBadgeEligible = isVerifiedPartnerBadgeEligible,
            SelfClaimedReviewsDisabled = selfClaimedReviewsDisabled,
            ChangeContractDocumentIds = changeContractDocumentIds,
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        request.ContractDocumentIds.Add(CounterpartyToolInput.ParseGuidList(contractDocumentIds, nameof(contractDocumentIds), out error));
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.UpdatePartnerRelationshipTermsAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapPartnerRelationship(response));
    }
}

[McpServerToolType]
public static class GetPartnerRelationshipTool
{
    [McpServerTool(Name = "get_partner_relationship"), Description("Get a partner relationship by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Partner relationship ID")] string partnerRelationshipId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.GetPartnerRelationshipAsync(
            new GetPartnerRelationshipRequest { PartnerRelationshipId = CounterpartyToolInput.ToGuid(partnerRelationshipId) },
            cancellationToken: cancellationToken);

        return CounterpartyCrudToolOutput.Json(CounterpartyMapper.MapPartnerRelationship(response));
    }
}

[McpServerToolType]
public static class ListPartnerRelationshipsByCounterpartyTool
{
    [McpServerTool(Name = "list_partner_relationships_by_counterparty"), Description("List partner relationship rows for a counterparty.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        PartnerRelationshipCrudService.PartnerRelationshipCrudServiceClient client,
        [Description("Counterparty ID")] string counterpartyId,
        [Description("PartnerRelationshipStatus filter (optional)")] string? status = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListPartnerRelationshipsByCounterpartyRequest
        {
            CounterpartyId = CounterpartyToolInput.ToGuid(counterpartyId),
            Status = CounterpartyToolInput.ParseOptionalEnum<PartnerRelationshipStatus>(status, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListPartnerRelationshipsByCounterpartyAsync(request, cancellationToken: cancellationToken);
        return CounterpartyCrudToolOutput.Json(new { items = response.Items.Select(CounterpartyMapper.MapPartnerRelationship) });
    }
}
