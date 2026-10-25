using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RewardFlow.SuppleDesign.Api.DTOs.Wallets;

/// <summary>Keeps generated OpenAPI examples meaningful instead of deriving random text from validation rules.</summary>
public sealed class WalletContractExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(OpenWalletRequest) &&
            schema.Properties.TryGetValue("name", out var name))
        {
            name.Example = new OpenApiString("Primary rewards");
        }

        if (context.Type == typeof(RewardMovementRequest) || context.Type == typeof(RewardTransferRequest))
        {
            if (schema.Properties.TryGetValue("operationId", out var operationId))
                operationId.Example = new OpenApiString("4fdbddf1-ef8a-49f7-82d5-6164a7434023");
            if (schema.Properties.TryGetValue("points", out var points))
                points.Example = new OpenApiLong(100);
            if (schema.Properties.TryGetValue("reason", out var reason))
                reason.Example = new OpenApiString("Purchase reward");
            if (schema.Properties.TryGetValue("destinationWalletId", out var destination))
                destination.Example = new OpenApiString("291edc91-c627-43c6-876f-d5f102e91db0");
        }
    }
}
