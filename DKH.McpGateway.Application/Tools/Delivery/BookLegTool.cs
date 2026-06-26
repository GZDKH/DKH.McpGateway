using DKH.DeliveryService.Contracts.Delivery.Api.Dispatch.v1;
using DKH.DeliveryService.Contracts.Delivery.Models.v1;

namespace DKH.McpGateway.Application.Tools.Delivery;

[McpServerToolType]
public static class BookLegTool
{
    [McpServerTool(Name = "book_leg"), Description(
        "Book a delivery leg with a carrier. " +
        "Provides confirmation number and optional departure window. " +
        "Returns the booking with ID, status, and timestamps.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        DispatchService.DispatchServiceClient client,
        [Description("Leg ID (GUID)")] string legId,
        [Description("Carrier counterparty ID (GUID)")] string carrierId,
        [Description("Carrier confirmation number")] string confirmationNumber,
        [Description("Departure window start (ISO 8601, optional)")] string? departureWindowStart = null,
        [Description("Departure window end (ISO 8601, optional)")] string? departureWindowEnd = null,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);

        if (string.IsNullOrWhiteSpace(legId))
        {
            return McpProtoHelper.FormatError("legId is required");
        }

        if (string.IsNullOrWhiteSpace(carrierId))
        {
            return McpProtoHelper.FormatError("carrierId is required");
        }

        if (string.IsNullOrWhiteSpace(confirmationNumber))
        {
            return McpProtoHelper.FormatError("confirmationNumber is required");
        }

        var request = new BookLegRequest
        {
            LegId = new GuidValue(legId),
            CarrierId = new GuidValue(carrierId),
            ConfirmationNumber = confirmationNumber,
        };

        if (!string.IsNullOrWhiteSpace(departureWindowStart) && DateTime.TryParse(departureWindowStart, out var depStart))
        {
            request.DepartureWindowStart = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(depStart.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(departureWindowEnd) && DateTime.TryParse(departureWindowEnd, out var depEnd))
        {
            request.DepartureWindowEnd = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(depEnd.ToUniversalTime());
        }

        var booking = await client.BookLegAsync(request, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(MapBooking(booking), McpJsonDefaults.Options);
    }

    internal static object MapBooking(BookingModel b) => new
    {
        id = b.Id?.Value,
        legId = b.LegId?.Value,
        carrierId = b.CarrierId?.Value,
        confirmationNumber = b.ConfirmationNumber,
        departureWindowStart = b.DepartureWindowStart?.ToDateTimeOffset().ToString("O"),
        departureWindowEnd = b.DepartureWindowEnd?.ToDateTimeOffset().ToString("O"),
        status = b.Status.ToString(),
        confirmedAt = b.ConfirmedAt?.ToDateTimeOffset().ToString("O"),
    };
}
