using DKH.EngagementService.Contracts.Engagement.Models.V1;
using DKH.EngagementService.Contracts.Engagement.Services.V1;
using EngagementGrpc = DKH.EngagementService.Contracts.Engagement.Services.V1.EngagementService;

namespace DKH.McpGateway.Application.Tools.Engagement;

[McpServerToolType]
public static class CreateServiceTemplateTool
{
    [McpServerTool(Name = "create_service_template"), Description("Create an engagement service template.")]
    public static async Task<string> ExecuteAsync(
        IApiKeyContext apiKeyContext,
        EngagementGrpc.EngagementServiceClient client,
        string serviceType,
        string code,
        string name,
        [Description("Template schema JSON object")] string? schemaJson = null,
        string? description = null,
        int displayOrder = 0,
        CancellationToken cancellationToken = default)
    {
        apiKeyContext.EnsurePermission(McpPermissions.Write);
        if (!EngagementToolInput.Required(code, nameof(code), out var error) ||
            !EngagementToolInput.Required(name, nameof(name), out error))
        {
            return McpProtoHelper.FormatError(error);
        }

        var parsedServiceType = EngagementToolInput.ParseRequiredEnum<ServiceType>(serviceType, nameof(serviceType), out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var schema = EngagementToolInput.ParseSchema(schemaJson, out error);
        if (error is not null)
        {
            return McpProtoHelper.FormatError(error);
        }

        var response = await client.CreateServiceTemplateAsync(
            new CreateServiceTemplateRequest
            {
                ServiceType = parsedServiceType,
                Code = code.Trim(),
                Name = name.Trim(),
                Description = EngagementToolInput.OptionalString(description),
                DisplayOrder = displayOrder,
                Schema = schema,
            },
            cancellationToken: cancellationToken);
        return EngagementToolOutput.Json(EngagementMapper.MapTemplate(response));
    }
}
