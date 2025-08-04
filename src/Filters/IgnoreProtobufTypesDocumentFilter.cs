using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TransitGtfsApi.Filters;

public class IgnoreProtobufTypesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        string[] protobufNamespaces = new[] { "TransitRealtime" };
        List<string> schemasToRemove = context.SchemaRepository.Schemas
            .Where(s => protobufNamespaces.Any(ns => s.Key.StartsWith(ns)))
            .Select(s => s.Key)
            .ToList();

        foreach (string schemaKey in schemasToRemove)
        {
            context.SchemaRepository.Schemas.Remove(schemaKey);
        }
    }
}