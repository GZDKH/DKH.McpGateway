using DKH.EngagementService.Contracts.Engagement.Models.V1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.Engagement;

internal static class EngagementMapper
{
    internal static object MapRequest(ServiceRequestModel request) => new
    {
        id = request.Id?.Value,
        serviceType = request.ServiceType.ToString(),
        status = request.Status.ToString(),
        requester = request.Requester is null ? null : new { source = request.Requester.Source.ToString() },
        provider = request.Provider is null ? null : new { source = request.Provider.Source.ToString() },
        subject = request.Subject is null ? null : MapSubject(request.Subject),
        price = request.Price is null ? null : new { amount = request.Price.Amount, currency = request.Price.Currency },
        deadline = Iso(request.Deadline),
        rejectionReason = request.RejectionReason,
        reservedEscrowId = request.ReservedEscrowId?.Value,
        capturedPaymentId = request.CapturedPaymentId?.Value,
        escrowStatus = request.EscrowStatus.ToString(),
        createdAt = Iso(request.CreatedAt),
        updatedAt = Iso(request.UpdatedAt),
    };

    internal static object MapSubject(EngagementSubjectModel subject) => subject.SubjectCase switch
    {
        EngagementSubjectModel.SubjectOneofCase.ProductRef => new { kind = "product", productRef = subject.ProductRef?.Value },
        EngagementSubjectModel.SubjectOneofCase.Freeform => new
        {
            kind = "freeform",
            description = subject.Freeform?.Description,
            location = subject.Freeform?.Location,
        },
        _ => new { kind = "none" },
    };

    internal static object MapTemplate(ServiceTemplateModel template) => new
    {
        id = template.Id?.Value,
        serviceType = template.ServiceType.ToString(),
        code = template.Code,
        name = template.Name,
        description = template.Description,
        version = template.Version,
        isPublished = template.IsPublished,
        displayOrder = template.DisplayOrder,
        schema = template.Schema is null ? null : MapSchema(template.Schema),
        createdAt = Iso(template.CreatedAt),
        updatedAt = Iso(template.UpdatedAt),
    };

    internal static object MapReport(ServiceReportModel report) => new
    {
        id = report.Id?.Value,
        serviceRequestId = report.ServiceRequestId?.Value,
        templateId = report.TemplateId?.Value,
        templateVersion = report.TemplateVersion,
        status = report.Status.ToString(),
        reviewNote = report.ReviewNote,
        answers = report.Answers.Select(answer => new
        {
            fieldKey = answer.FieldKey,
            value = answer.Value,
            values = answer.Values.ToArray(),
            attachmentIds = answer.AttachmentIds.Select(id => id.Value),
        }),
        createdAt = Iso(report.CreatedAt),
        updatedAt = Iso(report.UpdatedAt),
    };

    internal static object MapPage<T>(IEnumerable<T> items, int totalCount, int page, int pageSize, Func<T, object> map)
        => new { items = items.Select(map), totalCount, page, pageSize };

    private static object MapSchema(FormSchemaModel schema) => new
    {
        sections = schema.Sections.Select(section => new
        {
            key = section.Key,
            title = section.Title,
            order = section.Order,
            fields = section.Fields.Select(field => new
            {
                key = field.Key,
                label = field.Label,
                type = field.Type.ToString(),
                required = field.Required,
                order = field.Order,
                options = field.Options.ToArray(),
                minValue = field.MinValue,
                maxValue = field.MaxValue,
                helpText = field.HelpText,
            }),
        }),
    };

    private static string? Iso(Timestamp? timestamp)
        => timestamp?.ToDateTimeOffset().ToString("O");
}
