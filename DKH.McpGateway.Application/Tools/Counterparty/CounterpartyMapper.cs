using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Models.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Services.v1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Counterparty;

internal static class CounterpartyMapper
{
    private static readonly string[] PiiChangeKeys =
    [
        "legalname",
        "legal_name",
        "taxid",
        "tax_id",
        "registrationnumber",
        "registration_number",
        "email",
        "phone",
        "address",
    ];

    internal static object MapCounterparty(CounterpartyModel c) => new
    {
        id = c.Id?.Value,
        kind = c.Kind.ToString(),
        displayName = MapLocalizedText(c.DisplayName),
        roles = c.Roles.Select(r => r.ToString()),
        status = c.Status.ToString(),
        verificationLevel = c.VerificationLevel.ToString(),
        dataSource = c.DataSource.ToString(),
        visibilityMode = c.VisibilityMode.ToString(),
        storefrontScope = c.StorefrontScope.Select(v => v.Value),
        tags = c.Tags.ToArray(),
        description = MapLocalizedText(c.Description),
        parentCounterpartyId = c.ParentCounterpartyId?.Value,
        createdAt = Iso(c.CreatedAt),
        updatedAt = Iso(c.UpdatedAt),
        capabilities = c.Capabilities is null ? null : MapCapabilities(c.Capabilities),
        avatarAttachmentId = c.AvatarAttachmentId?.Value,
        documentAttachmentIds = c.DocumentAttachmentIds.Select(v => v.Value),
    };

    internal static object MapBasics(CounterpartyBasicsModel b) => new
    {
        id = b.Id?.Value,
        kind = b.Kind.ToString(),
        displayName = MapLocalizedText(b.DisplayName),
        roles = b.Roles.Select(r => r.ToString()),
        status = b.Status.ToString(),
        verificationLevel = b.VerificationLevel.ToString(),
        avatarAttachmentId = b.AvatarAttachmentId?.Value,
    };

    internal static object MapMedia(CounterpartyMediaModel m) => new
    {
        id = m.Id?.Value,
        counterpartyId = m.CounterpartyId?.Value,
        kind = m.Kind.ToString(),
        storageFileRef = m.StorageFileRef,
        displayOrder = m.DisplayOrder,
        isPrimary = m.IsPrimary,
        altText = MapLocalizedText(m.AltText),
        createdAt = Iso(m.CreatedAt),
        updatedAt = Iso(m.UpdatedAt),
    };

    internal static object MapDocument(CounterpartyDocumentModel d) => new
    {
        id = d.Id?.Value,
        counterpartyId = d.CounterpartyId?.Value,
        kind = d.Kind.ToString(),
        storageFileRef = d.StorageFileRef,
        number = d.Number,
        issuedAt = Iso(d.IssuedAt),
        validUntil = Iso(d.ValidUntil),
        issuingAuthority = d.IssuingAuthority,
        verificationStatus = d.VerificationStatus.ToString(),
        verifiedByUserId = d.VerifiedByUserId?.Value,
        verifiedAt = Iso(d.VerifiedAt),
        notes = MapLocalizedText(d.Notes),
        createdAt = Iso(d.CreatedAt),
        updatedAt = Iso(d.UpdatedAt),
    };

    internal static object MapAcl(CounterpartyAclEntry e) => new
    {
        id = e.Id?.Value,
        counterpartyId = e.CounterpartyId?.Value,
        userId = e.UserId?.Value,
        permissionLevel = e.PermissionLevel.ToString(),
        createdAt = Iso(e.CreatedAt),
        updatedAt = Iso(e.UpdatedAt),
    };

    internal static object MapVerificationAttempt(VerificationAttemptModel a) => new
    {
        id = a.Id?.Value,
        counterpartyId = a.CounterpartyId?.Value,
        requestedLevel = a.RequestedLevel.ToString(),
        status = a.Status.ToString(),
        documentMediaRefs = a.DocumentMediaRefs.ToArray(),
        note = a.Note,
        actorUserId = a.ActorUserId?.Value,
        decidedAt = Iso(a.DecidedAt),
        createdAt = Iso(a.CreatedAt),
        updatedAt = Iso(a.UpdatedAt),
    };

    internal static object MapAuditEntry(CounterpartyAuditEntry e) => new
    {
        changedAtUtc = Iso(e.ChangedAtUtc),
        changedByUserId = e.ChangedByUserId,
        operation = e.Operation,
        changes = e.Changes
            .Where(kv => !ContainsPiiKey(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value),
    };

    internal static object MapPartnerRelationship(PartnerRelationshipModel r) => new
    {
        id = r.Id?.Value,
        counterpartyId = r.CounterpartyId?.Value,
        status = r.Status.ToString(),
        tier = r.Tier.ToString(),
        isVerifiedPartnerBadgeEligible = r.IsVerifiedPartnerBadgeEligible,
        selfClaimedReviewsDisabled = r.SelfClaimedReviewsDisabled,
        commissionTerms = r.CommissionTerms is null
            ? null
            : new
            {
                percentRate = r.CommissionTerms.PercentRate,
                flatFee = r.CommissionTerms.FlatFee,
                currency = r.CommissionTerms.Currency,
                note = r.CommissionTerms.Note,
            },
        exclusivityTerms = r.ExclusivityTerms is null
            ? null
            : new
            {
                isExclusive = r.ExclusivityTerms.IsExclusive,
                scope = r.ExclusivityTerms.Scope,
                note = r.ExclusivityTerms.Note,
            },
        contractDocumentIds = r.ContractDocumentIds.Select(v => v.Value),
        activatedAt = Iso(r.ActivatedAt),
        suspendedAt = Iso(r.SuspendedAt),
        terminatedAt = Iso(r.TerminatedAt),
        terminationReason = r.TerminationReason,
        createdAt = Iso(r.CreatedAt),
        updatedAt = Iso(r.UpdatedAt),
    };

    internal static object MapBusinessRelationship(CounterpartyBusinessRelationshipSnapshot s) => new
    {
        counterpartyId = s.CounterpartyId?.Value,
        stage = s.Stage.ToString(),
        partnerRelationshipStatus = s.PartnerRelationshipStatus.ToString(),
        activePartnerRelationshipId = s.ActivePartnerRelationshipId?.Value,
        lastOrderAtUtc = Iso(s.LastOrderAtUtc),
        daysSinceLastOrder = s.DaysSinceLastOrder,
        fulfilledOrdersCount = s.FulfilledOrdersCount,
        sampleOrdersCount = s.SampleOrdersCount,
    };

    internal static object MapBalance(CounterpartyApBalanceModel b) => new
    {
        counterpartyId = b.CounterpartyId?.Value,
        currency = b.Currency,
        committedAmount = b.CommittedAmount,
        paidAmount = b.PaidAmount,
        balanceAmount = b.BalanceAmount,
        lastUpdatedAt = Iso(b.LastUpdatedAt),
    };

    internal static object MapFinancialDashboard(CounterpartyFinancialDashboardModel d) => new
    {
        counterpartyId = d.CounterpartyId?.Value,
        balances = d.Balances.Select(MapBalance),
        reconciliationActs = d.ReconciliationActs.Select(MapReconciliationAct),
        activeDebtLimitBreaches = d.ActiveDebtLimitBreaches.Select(MapDebtLimitBreach),
    };

    internal static object MapPage(PaginationMetadata? metadata, int fallbackPage, int fallbackPageSize) => new
    {
        totalCount = metadata?.TotalCount ?? 0,
        page = metadata?.CurrentPage ?? fallbackPage,
        pageSize = metadata?.PageSize ?? fallbackPageSize,
    };

    internal static Dictionary<string, string> MapLocalizedText(LocalizedText? text)
        => text?.Values.ToDictionary(kv => kv.Key, kv => kv.Value) ?? [];

    private static object MapCapabilities(CounterpartyCapabilitiesModel c) => new
    {
        transportModes = c.TransportModes.Select(m => m.ToString()),
        serviceZoneIds = c.ServiceZoneIds.Select(v => v.Value),
        maxWeightKg = c.MaxWeightKg,
        maxDimensions = c.MaxDimensions is null
            ? null
            : new
            {
                lengthCm = c.MaxDimensions.LengthCm,
                widthCm = c.MaxDimensions.WidthCm,
                heightCm = c.MaxDimensions.HeightCm,
            },
        supportsHazmat = c.SupportsHazmat,
        supportsColdChain = c.SupportsColdChain,
        supportsCod = c.SupportsCod,
        supportsInsurance = c.SupportsInsurance,
        supportsTracking = c.SupportsTracking,
        customCapabilities = c.CustomCapabilities.ToArray(),
    };

    private static object MapReconciliationAct(CounterpartyFinancialReconciliationActSummaryModel a) => new
    {
        id = a.Id?.Value,
        counterpartyRole = a.CounterpartyRole.ToString(),
        status = a.Status.ToString(),
        periodStart = a.PeriodStart,
        periodEnd = a.PeriodEnd,
        currency = a.Currency,
        openingBalanceAmount = a.OpeningBalanceAmount,
        accruedAmount = a.AccruedAmount,
        settledAmount = a.SettledAmount,
        closingBalanceAmount = a.ClosingBalanceAmount,
        draftedAt = Iso(a.DraftedAt),
        sentAt = Iso(a.SentAt),
        signedAt = Iso(a.SignedAt),
        disputedAt = Iso(a.DisputedAt),
        closedAt = Iso(a.ClosedAt),
    };

    private static object MapDebtLimitBreach(CounterpartyFinancialDebtLimitBreachSummaryModel b) => new
    {
        id = b.Id?.Value,
        debtLimitId = b.DebtLimitId?.Value,
        counterpartyRole = b.CounterpartyRole.ToString(),
        currency = b.Currency,
        limitAmount = b.LimitAmount,
        balanceAmount = b.BalanceAmount,
        breachedAt = Iso(b.BreachedAt),
        lastObservedAt = Iso(b.LastObservedAt),
    };

    private static string? Iso(Timestamp? timestamp)
        => timestamp?.ToDateTimeOffset().ToString("O");

    private static bool ContainsPiiKey(string key)
        => PiiChangeKeys.Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));
}
