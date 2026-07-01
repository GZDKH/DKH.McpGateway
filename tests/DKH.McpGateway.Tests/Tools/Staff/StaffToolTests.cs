using DKH.McpGateway.Application.Tools.Common;
using DKH.McpGateway.Application.Tools.Payment;
using DKH.McpGateway.Application.Tools.Staff;
using DKH.Platform.Grpc.Common.Types;
using DKH.StaffService.Contracts.Models.V1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Tests.Tools.Staff;

public sealed class StaffToolTests
{
    [Fact]
    public void StaffToolSurface_DefinesEverySpecTool()
    {
        string[] expected =
        [
            "get_employee",
            "list_employees",
            "get_employee_by_keycloak_user_id",
            "get_department",
            "list_departments",
            "get_onboarding_checklist",
            "get_employee_onboarding_checklist",
            "open_working_shift",
            "close_working_shift",
            "get_current_working_shift",
            "list_active_working_shifts",
            "open_cashier_shift",
            "close_cashier_shift",
            "get_current_cashier_shift",
            "list_active_cashier_shifts",
            "list_device_presences",
        ];

        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Staff")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        actual.Should().Contain(expected);
    }

    [Fact]
    public void StaffToolSurface_DoesNotExposeHeartbeatIngestion()
    {
        var actual = typeof(GetPaymentTool).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "DKH.McpGateway.Application.Tools.Staff")
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Where(a => a.GetType().Name == "McpServerToolAttribute")
            .Select(a => (string?)a.GetType().GetProperty("Name")?.GetValue(a))
            .OfType<string>()
            .ToArray();

        actual.Should().NotContain(name => name.Contains("heartbeat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapEmployee_OmitsPersonalIdentityFieldsAndValues()
    {
        var model = new EmployeeModel
        {
            Id = new GuidValue("employee-1"),
            KeycloakUserId = new GuidValue("keycloak-user-secret"),
            FullName = "Sensitive Employee",
            Email = "employee@example.test",
            DepartmentId = new GuidValue("department-1"),
            ManagerId = new GuidValue("manager-1"),
            Status = EmploymentStatus.Active,
            HiredAt = Timestamp.FromDateTime(DateTime.UtcNow),
        };

        var json = JsonSerializer.Serialize(StaffMapper.MapEmployee(model), McpJsonDefaults.Options);

        json.Should().Contain("employee-1");
        json.Should().Contain("department-1");
        json.Should().NotContain("Sensitive Employee");
        json.Should().NotContain("employee@example.test");
        json.Should().NotContain("keycloak-user-secret");
        json.Should().NotContain("\"fullName\"");
        json.Should().NotContain("\"email\"");
        json.Should().NotContain("\"keycloakUserId\"");
    }
}
