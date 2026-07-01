using DKH.StaffService.Contracts.Models.V1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Staff;

internal static class StaffMapper
{
    internal static object MapEmployee(EmployeeModel employee) => new
    {
        id = employee.Id?.Value,
        departmentId = employee.DepartmentId?.Value,
        managerId = employee.ManagerId?.Value,
        status = employee.Status.ToString(),
        hiredAt = Iso(employee.HiredAt),
        terminatedAt = Iso(employee.TerminatedAt),
    };

    internal static object MapDepartment(DepartmentModel department) => new
    {
        id = department.Id?.Value,
        name = department.Name,
        parentDepartmentId = department.ParentDepartmentId?.Value,
        headEmployeeId = department.HeadEmployeeId?.Value,
        createdAt = Iso(department.CreatedAt),
    };

    internal static object MapWorkingShift(WorkingShiftModel shift) => new
    {
        id = shift.Id?.Value,
        employeeId = shift.EmployeeId?.Value,
        locationContextId = shift.LocationContextId,
        locationContextName = shift.LocationContextName,
        module = shift.Module.ToString(),
        deviceId = shift.DeviceId,
        status = shift.Status.ToString(),
        openedAt = Iso(shift.OpenedAt),
        closedAt = Iso(shift.ClosedAt),
        closedByEmployeeId = shift.ClosedByEmployeeId?.Value,
        closeNote = shift.CloseNote,
    };

    internal static object MapCashierShift(CashierShiftModel shift) => new
    {
        id = shift.Id?.Value,
        employeeId = shift.EmployeeId?.Value,
        locationContextId = shift.LocationContextId,
        locationContextName = shift.LocationContextName,
        registerId = shift.RegisterId,
        terminalId = shift.TerminalId,
        openingCashMinor = shift.OpeningCashMinor,
        closingCashMinor = shift.ClosingCashMinor,
        currencyCode = shift.CurrencyCode,
        status = shift.Status.ToString(),
        openedAt = Iso(shift.OpenedAt),
        closedAt = Iso(shift.ClosedAt),
        closedByEmployeeId = shift.ClosedByEmployeeId?.Value,
        closeNote = shift.CloseNote,
    };

    internal static object MapOnboardingChecklist(OnboardingChecklistModel checklist) => new
    {
        id = checklist.Id?.Value,
        employeeId = checklist.EmployeeId?.Value,
        stepCount = checklist.StepCount,
        completedStepCount = checklist.CompletedStepCount,
        createdAt = Iso(checklist.CreatedAt),
        completedAt = Iso(checklist.CompletedAt),
        steps = checklist.Steps.Select(step => new
        {
            id = step.Id?.Value,
            title = step.Title,
            sortOrder = step.SortOrder,
            completedAt = Iso(step.CompletedAt),
            completedBy = step.CompletedBy?.Value,
        }),
    };

    internal static object MapDevicePresence(DevicePresenceModel presence) => new
    {
        id = presence.Id?.Value,
        employeeId = presence.EmployeeId?.Value,
        deviceId = presence.DeviceId,
        appKind = presence.AppKind.ToString(),
        module = presence.Module.ToString(),
        locationContextId = presence.LocationContextId,
        locationContextName = presence.LocationContextName,
        shiftId = presence.ShiftId?.Value,
        status = presence.Status.ToString(),
        lastSeenAt = Iso(presence.LastSeenAt),
        staleAt = Iso(presence.StaleAt),
        offlineAt = Iso(presence.OfflineAt),
        serverObservedAt = Iso(presence.ServerObservedAt),
    };

    internal static object MapPage(PaginationMetadata? metadata, int fallbackPage, int fallbackPageSize) => new
    {
        totalCount = metadata?.TotalCount ?? 0,
        totalPages = metadata?.TotalPages ?? 0,
        currentPage = metadata?.CurrentPage ?? fallbackPage,
        pageSize = metadata?.PageSize ?? fallbackPageSize,
        hasNextPage = metadata?.HasNextPage ?? false,
        hasPreviousPage = metadata?.HasPreviousPage ?? false,
    };

    private static string? Iso(Timestamp? timestamp)
        => timestamp?.ToDateTimeOffset().ToString("O");
}
