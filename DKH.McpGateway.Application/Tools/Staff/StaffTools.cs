using DKH.StaffService.Contracts.Models.V1;
using DKH.StaffService.Contracts.Services.V1;
using Grpc.Core;
using StaffGrpc = DKH.StaffService.Contracts.Services.V1.Staff;

namespace DKH.McpGateway.Application.Tools.Staff;

[McpServerToolType]
public static class GetEmployeeTool
{
    [McpServerTool(Name = "get_employee"), Description("Get an employee by ID. Personal name, email, and Keycloak user ID are omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Employee ID")] string employeeId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(employeeId, nameof(employeeId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetEmployeeAsync(
            new GetEmployeeRequest { Id = StaffToolInput.ToGuid(employeeId) },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapEmployee(response));
    }
}

[McpServerToolType]
public static class ListEmployeesTool
{
    [McpServerTool(Name = "list_employees"), Description("List employees. Personal names, emails, and Keycloak user IDs are omitted.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Department ID filter (optional)")] string? departmentId = null,
        [Description("Manager employee ID filter (optional)")] string? managerId = null,
        [Description("EmploymentStatus filter (optional)")] string? status = null,
        [Description("Page number")] int page = 1,
        [Description("Page size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListEmployeesRequest
        {
            Pagination = StaffToolInput.Page(page, pageSize),
            DepartmentId = StaffToolInput.ToGuidOrNull(departmentId),
            ManagerId = StaffToolInput.ToGuidOrNull(managerId),
            Status = StaffToolInput.ParseOptionalEnum<EmploymentStatus>(status, out var error),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListEmployeesAsync(request, cancellationToken: cancellationToken);
        return StaffToolOutput.Json(new
        {
            items = response.Items.Select(StaffMapper.MapEmployee),
            metadata = StaffMapper.MapPage(response.Metadata, request.Pagination.Page, request.Pagination.PageSize),
        });
    }
}

[McpServerToolType]
public static class GetEmployeeByKeycloakUserIdTool
{
    [McpServerTool(Name = "get_employee_by_keycloak_user_id"), Description("Get an employee by Keycloak user ID. The lookup key is not echoed.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Keycloak user ID lookup key")] string keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(keycloakUserId, nameof(keycloakUserId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        try
        {
            var response = await client.GetEmployeeByKeycloakUserIdAsync(
                new GetEmployeeByKeycloakUserIdRequest { KeycloakUserId = StaffToolInput.ToGuid(keycloakUserId) },
                cancellationToken: cancellationToken);

            return StaffToolOutput.Json(StaffMapper.MapEmployee(response));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return McpProtoHelper.FormatError("no employee for keycloak user");
        }
    }
}

[McpServerToolType]
public static class GetDepartmentTool
{
    [McpServerTool(Name = "get_department"), Description("Get a staff department by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Department ID")] string departmentId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(departmentId, nameof(departmentId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetDepartmentAsync(
            new GetDepartmentRequest { Id = StaffToolInput.ToGuid(departmentId) },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapDepartment(response));
    }
}

[McpServerToolType]
public static class ListDepartmentsTool
{
    [McpServerTool(Name = "list_departments"), Description("List staff departments.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Parent department ID filter (optional)")] string? parentDepartmentId = null,
        [Description("Include all departments instead of only roots")] bool includeAll = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListDepartmentsAsync(
            new ListDepartmentsRequest
            {
                ParentDepartmentId = StaffToolInput.ToGuidOrNull(parentDepartmentId),
                IncludeAll = includeAll,
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(new { items = response.Items.Select(StaffMapper.MapDepartment) });
    }
}

[McpServerToolType]
public static class GetOnboardingChecklistTool
{
    [McpServerTool(Name = "get_onboarding_checklist"), Description("Get an onboarding checklist by ID.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Checklist ID")] string checklistId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(checklistId, nameof(checklistId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetOnboardingChecklistAsync(
            new GetOnboardingChecklistRequest { Id = StaffToolInput.ToGuid(checklistId) },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapOnboardingChecklist(response));
    }
}

[McpServerToolType]
public static class GetEmployeeOnboardingChecklistTool
{
    [McpServerTool(Name = "get_employee_onboarding_checklist"), Description("Get the onboarding checklist for an employee.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Employee ID")] string employeeId,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(employeeId, nameof(employeeId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetEmployeeOnboardingChecklistAsync(
            new GetEmployeeOnboardingChecklistRequest { EmployeeId = StaffToolInput.ToGuid(employeeId) },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapOnboardingChecklist(response));
    }
}

[McpServerToolType]
public static class OpenWorkingShiftTool
{
    [McpServerTool(Name = "open_working_shift"), Description("Open a working shift for an employee.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Employee ID")] string employeeId,
        [Description("WorkingShiftModule")] string module,
        [Description("Location context ID (optional)")] string? locationContextId = null,
        [Description("Location context name (optional)")] string? locationContextName = null,
        [Description("Device ID (optional)")] string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!StaffToolInput.Required(employeeId, nameof(employeeId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var parsedModule = StaffToolInput.ParseRequiredEnum<WorkingShiftModule>(module, nameof(module), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.OpenWorkingShiftAsync(
            new OpenWorkingShiftRequest
            {
                EmployeeId = StaffToolInput.ToGuid(employeeId),
                LocationContextId = StaffToolInput.OptionalString(locationContextId),
                LocationContextName = StaffToolInput.OptionalString(locationContextName),
                Module = parsedModule,
                DeviceId = StaffToolInput.OptionalString(deviceId),
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapWorkingShift(response));
    }
}

[McpServerToolType]
public static class CloseWorkingShiftTool
{
    [McpServerTool(Name = "close_working_shift"), Description("Close a working shift.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Working shift ID")] string shiftId,
        [Description("Employee ID closing the shift")] string closedByEmployeeId,
        [Description("Close note (optional)")] string? closeNote = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!StaffToolInput.Required(shiftId, nameof(shiftId), out var error) ||
            !StaffToolInput.Required(closedByEmployeeId, nameof(closedByEmployeeId), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CloseWorkingShiftAsync(
            new CloseWorkingShiftRequest
            {
                ShiftId = StaffToolInput.ToGuid(shiftId),
                ClosedByEmployeeId = StaffToolInput.ToGuid(closedByEmployeeId),
                CloseNote = StaffToolInput.OptionalString(closeNote),
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapWorkingShift(response));
    }
}

[McpServerToolType]
public static class GetCurrentWorkingShiftTool
{
    [McpServerTool(Name = "get_current_working_shift"), Description("Get the current working shift for an employee and module.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Employee ID")] string employeeId,
        [Description("WorkingShiftModule")] string module,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(employeeId, nameof(employeeId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var parsedModule = StaffToolInput.ParseRequiredEnum<WorkingShiftModule>(module, nameof(module), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetCurrentWorkingShiftAsync(
            new GetCurrentWorkingShiftRequest
            {
                EmployeeId = StaffToolInput.ToGuid(employeeId),
                Module = parsedModule,
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapWorkingShift(response));
    }
}

[McpServerToolType]
public static class ListActiveWorkingShiftsTool
{
    [McpServerTool(Name = "list_active_working_shifts"), Description("List active working shifts.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("WorkingShiftModule filter (optional)")] string? module = null,
        [Description("Location context ID filter (optional)")] string? locationContextId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListActiveWorkingShiftsRequest
        {
            Module = StaffToolInput.ParseOptionalEnum<WorkingShiftModule>(module, out var error),
            LocationContextId = StaffToolInput.OptionalString(locationContextId),
        };
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.ListActiveWorkingShiftsAsync(request, cancellationToken: cancellationToken);
        return StaffToolOutput.Json(new { items = response.Items.Select(StaffMapper.MapWorkingShift) });
    }
}

[McpServerToolType]
public static class OpenCashierShiftTool
{
    [McpServerTool(Name = "open_cashier_shift"), Description("Open a cashier shift for an employee.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Employee ID")] string employeeId,
        [Description("Location context ID (optional)")] string? locationContextId = null,
        [Description("Location context name (optional)")] string? locationContextName = null,
        [Description("Register ID (optional)")] string? registerId = null,
        [Description("Terminal ID (optional)")] string? terminalId = null,
        [Description("Opening cash in minor units")] long openingCashMinor = 0,
        [Description("Currency code")] string? currencyCode = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!StaffToolInput.Required(employeeId, nameof(employeeId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.OpenCashierShiftAsync(
            new OpenCashierShiftRequest
            {
                EmployeeId = StaffToolInput.ToGuid(employeeId),
                LocationContextId = StaffToolInput.OptionalString(locationContextId),
                LocationContextName = StaffToolInput.OptionalString(locationContextName),
                RegisterId = StaffToolInput.OptionalString(registerId),
                TerminalId = StaffToolInput.OptionalString(terminalId),
                OpeningCashMinor = openingCashMinor,
                CurrencyCode = StaffToolInput.OptionalString(currencyCode),
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapCashierShift(response));
    }
}

[McpServerToolType]
public static class CloseCashierShiftTool
{
    [McpServerTool(Name = "close_cashier_shift"), Description("Close a cashier shift.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Cashier shift ID")] string shiftId,
        [Description("Employee ID closing the shift")] string closedByEmployeeId,
        [Description("Closing cash in minor units")] long closingCashMinor = 0,
        [Description("Close note (optional)")] string? closeNote = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (!StaffToolInput.Required(shiftId, nameof(shiftId), out var error) ||
            !StaffToolInput.Required(closedByEmployeeId, nameof(closedByEmployeeId), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CloseCashierShiftAsync(
            new CloseCashierShiftRequest
            {
                ShiftId = StaffToolInput.ToGuid(shiftId),
                ClosedByEmployeeId = StaffToolInput.ToGuid(closedByEmployeeId),
                ClosingCashMinor = closingCashMinor,
                CloseNote = StaffToolInput.OptionalString(closeNote),
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapCashierShift(response));
    }
}

[McpServerToolType]
public static class GetCurrentCashierShiftTool
{
    [McpServerTool(Name = "get_current_cashier_shift"), Description("Get the current cashier shift for an employee.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Employee ID")] string employeeId,
        [Description("Register ID (optional)")] string? registerId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        if (!StaffToolInput.Required(employeeId, nameof(employeeId), out var error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.GetCurrentCashierShiftAsync(
            new GetCurrentCashierShiftRequest
            {
                EmployeeId = StaffToolInput.ToGuid(employeeId),
                RegisterId = StaffToolInput.OptionalString(registerId),
            },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(StaffMapper.MapCashierShift(response));
    }
}

[McpServerToolType]
public static class ListActiveCashierShiftsTool
{
    [McpServerTool(Name = "list_active_cashier_shifts"), Description("List active cashier shifts.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("Location context ID filter (optional)")] string? locationContextId = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var response = await client.ListActiveCashierShiftsAsync(
            new ListActiveCashierShiftsRequest { LocationContextId = StaffToolInput.OptionalString(locationContextId) },
            cancellationToken: cancellationToken);

        return StaffToolOutput.Json(new { items = response.Items.Select(StaffMapper.MapCashierShift) });
    }
}

[McpServerToolType]
public static class ListDevicePresencesTool
{
    [McpServerTool(Name = "list_device_presences"), Description("List device presence records.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        StaffGrpc.StaffClient client,
        [Description("PresenceAppKind filter (optional)")] string? appKind = null,
        [Description("WorkingShiftModule filter (optional)")] string? module = null,
        [Description("Location context ID filter (optional)")] string? locationContextId = null,
        [Description("Include offline presence rows")] bool includeOffline = false,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Read);

        var request = new ListDevicePresencesRequest
        {
            AppKind = StaffToolInput.ParseOptionalEnum<PresenceAppKind>(appKind, out var error),
            Module = StaffToolInput.ParseOptionalEnum<WorkingShiftModule>(module, out var moduleError),
            LocationContextId = StaffToolInput.OptionalString(locationContextId),
            IncludeOffline = includeOffline,
        };
        if (error is not null || moduleError is not null)
        {
            return McpProtoHelper.FormatError(error ?? moduleError ?? "filter value is invalid");
        }

        var response = await client.ListDevicePresencesAsync(request, cancellationToken: cancellationToken);
        return StaffToolOutput.Json(new { items = response.Items.Select(StaffMapper.MapDevicePresence) });
    }
}

internal static class StaffToolOutput
{
    internal static string Json(object value) => JsonSerializer.Serialize(value, McpJsonDefaults.Options);
}
