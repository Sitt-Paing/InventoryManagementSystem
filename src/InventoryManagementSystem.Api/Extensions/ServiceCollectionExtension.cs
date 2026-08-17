using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InventoryManagementSystem.Api.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddOpenApiDoc(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
            options.CreateSchemaReferenceId = OpenApiOptions.CreateDefaultSchemaReferenceId;
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Inventory Management API",
                    Description = "This is a Web API for Inventory Management System"
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter Bearer followed by your valid JWT token."
                };

                document.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
                    }
                ];

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
