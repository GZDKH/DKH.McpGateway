using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.McpGateway.Application.Tools.Common;
using DKH.McpGateway.Application.Tools.Engagement;
using DKH.McpGateway.Application.Tools.Payment;
using DKH.Platform.Grpc.Common.Types;

namespace DKH.McpGateway.Tests.Tools.Engagement;

public sealed class EngagementToolTests
{
    [Fact]
    public void EngagementToolSurface_DefinesEverySpecTool()
    {
        string[] expected =
        [
            "create_service_request",
            "assign_provider",
            "start_service_request",
            "complete_service_request",
            "cancel_service_request",
            "get_service_request",
            "list_service_requests",
            "list_assigned_requests",
            "create_service_template",
            "get_service_template",
            "publish_service_template",
            "list_service_templates",
            "create_service_report",
            "save_service_report_answers",
            "submit_service_report",
            "get_service_report",
            "review_service_report",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Engagement")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().Contain(expected);
    }

    [Fact]
    public void EngagementToolSurface_DoesNotExposeProfileService()
    {
        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Engagement")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToArray();

        actual.Should().NotContain(name => name.Contains("profile", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapRequest_OmitsRequesterAndProviderIdentityValues()
    {
        var model = new ServiceRequestModel
        {
            Id = new GuidValue("request-1"),
            ServiceType = ServiceType.Inspection,
            Requester = new ServiceRequesterModel
            {
                KeycloakUserId = new GuidValue("requester-keycloak-secret"),
                SourceId = new GuidValue("requester-source-secret"),
                Source = RequesterSource.Internal,
            },
            Provider = new ServiceProviderModel
            {
                KeycloakUserId = new GuidValue("provider-keycloak-secret"),
                SourceId = new GuidValue("provider-source-secret"),
                Source = ProviderSource.External,
            },
        };

        var json = JsonSerializer.Serialize(EngagementMapper.MapRequest(model), McpJsonDefaults.Options);

        json.Should().Contain("request-1");
        json.Should().Contain("Internal");
        json.Should().Contain("External");
        json.Should().NotContain("requester-keycloak-secret");
        json.Should().NotContain("requester-source-secret");
        json.Should().NotContain("provider-keycloak-secret");
        json.Should().NotContain("provider-source-secret");
        json.Should().NotContain("keycloakUserId");
        json.Should().NotContain("sourceId");
    }
}
