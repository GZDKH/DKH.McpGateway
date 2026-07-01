using DKH.CounterpartyService.Contracts.Counterparty.Api.CounterpartyCrud.v1;
using DKH.CounterpartyService.Contracts.Counterparty.Models.v1;
using DKH.McpGateway.Application.Tools.Common;
using DKH.McpGateway.Application.Tools.Counterparty;
using DKH.McpGateway.Application.Tools.Payment;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Counterparty;

public sealed class CounterpartyToolTests
{
    [Fact]
    public void CounterpartyToolSurface_DefinesEverySpecTool()
    {
        string[] expected =
        [
            "get_counterparty",
            "list_counterparties",
            "create_counterparty",
            "update_counterparty",
            "archive_counterparty",
            "batch_get_counterparty_basics",
            "set_counterparty_capabilities",
            "attach_counterparty_media",
            "detach_counterparty_media",
            "set_primary_counterparty_media",
            "list_counterparty_media",
            "attach_counterparty_document",
            "detach_counterparty_document",
            "verify_counterparty_document",
            "reject_counterparty_document",
            "list_counterparty_documents",
            "list_expiring_documents",
            "import_counterparty",
            "list_counterparty_audit_log",
            "grant_counterparty_access",
            "revoke_counterparty_access",
            "list_counterparty_acl",
            "submit_for_verification",
            "approve_verification",
            "reject_verification",
            "list_verification_attempts",
            "get_counterparty_business_relationship",
            "activate_partner_relationship",
            "suspend_partner_relationship",
            "reactivate_partner_relationship",
            "terminate_partner_relationship",
            "update_partner_relationship_terms",
            "get_partner_relationship",
            "list_partner_relationships_by_counterparty",
            "get_counterparty_balance",
            "get_counterparty_financial_dashboard",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Counterparty")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().Contain(expected);
    }

    [Fact]
    public void CounterpartyToolSurface_DoesNotExposeContactChannelTools()
    {
        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Counterparty")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToArray();

        actual.Should().NotContain(name => name.Contains("contact", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapCounterparty_OmitsPiiFieldsAndValues()
    {
        var model = new CounterpartyModel
        {
            Kind = CounterpartyKind.Company,
            LegalName = "Sensitive Legal Name LLC",
            TaxId = "TAX-SECRET",
            RegistrationNumber = "REG-SECRET",
            Email = "secret@example.test",
            Phone = "+10000000000",
            Address = new CounterpartyAddress
            {
                City = "Secret City",
                Country = "ZZ",
            },
            DisplayName = new LocalizedText { Values = { ["en"] = "Public Supplier" } },
        };

        var json = JsonSerializer.Serialize(CounterpartyMapper.MapCounterparty(model), McpJsonDefaults.Options);

        json.Should().Contain("Public Supplier");
        json.Should().NotContain("Sensitive Legal Name");
        json.Should().NotContain("TAX-SECRET");
        json.Should().NotContain("REG-SECRET");
        json.Should().NotContain("secret@example.test");
        json.Should().NotContain("+10000000000");
        json.Should().NotContain("Secret City");
        json.Should().NotContain("\"legalName\"");
        json.Should().NotContain("\"taxId\"");
        json.Should().NotContain("\"registrationNumber\"");
        json.Should().NotContain("\"email\"");
        json.Should().NotContain("\"phone\"");
        json.Should().NotContain("\"address\"");
    }

    [Fact]
    public void MapAuditEntry_FiltersPiiChangeKeys()
    {
        var entry = new CounterpartyAuditEntry
        {
            ChangedAtUtc = Timestamp.FromDateTime(DateTime.UtcNow),
            Operation = "Updated",
            Changes =
            {
                ["displayName"] = "public change",
                ["legalName"] = "secret legal name",
                ["tax_id"] = "secret tax id",
                ["address.city"] = "secret city",
            },
        };

        var json = JsonSerializer.Serialize(CounterpartyMapper.MapAuditEntry(entry), McpJsonDefaults.Options);

        json.Should().Contain("public change");
        json.Should().NotContain("secret legal name");
        json.Should().NotContain("secret tax id");
        json.Should().NotContain("secret city");
    }

    [Fact]
    public void CounterpartyToolInput_ParsesProtoEnumNamesAndKeepsOptionalLocalizedTextUnset()
    {
        CounterpartyToolInput.TryParseEnum<CounterpartyKind>("COUNTERPARTY_KIND_COMPANY", out var kind)
            .Should().BeTrue();
        kind.Should().Be(CounterpartyKind.Company);

        var text = CounterpartyToolInput.ParseLocalizedText(null, "displayName", required: false, out var error);

        error.Should().BeNull();
        text.Should().BeNull();
    }
}
