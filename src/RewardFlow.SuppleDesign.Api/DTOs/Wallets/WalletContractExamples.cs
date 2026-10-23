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

        if (context.Type == typeof(PointsRequest) &&
            schema.Properties.TryGetValue("points", out var points))
        {
            points.Example = new OpenApiLong(100);
        }
    }
}
