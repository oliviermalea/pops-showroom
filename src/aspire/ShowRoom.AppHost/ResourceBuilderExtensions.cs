using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;

namespace ShowRoom.AppHost;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Adds an explicit, clickable Scalar API-docs link (<c>/scalar/v1</c>) to the resource in the Aspire
    /// dashboard's Endpoints column, resolved at runtime against the resource's HTTPS endpoint. Unlike
    /// <see cref="WithScalar{T}"/> (a dashboard command/button), this surfaces the doc URL directly instead
    /// of the bare <c>host:port</c>.
    /// </summary>
    internal static IResourceBuilder<T> WithScalarUrl<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints
    {
        return builder.WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation
        {
            Url = "/scalar/v1",
            DisplayText = "Scalar API",
        });
    }


    internal static IResourceBuilder<T> WithSwaggerUI<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints
    {
        return builder.WithOpenApiDocs("swagger-ui-docs", "Swagger API Documentation", "swagger");
    }

    internal static IResourceBuilder<T> WithScalar<T>(this IResourceBuilder<T> builder)
    where T : IResourceWithEndpoints
    {
        return builder.WithOpenApiDocs("scalar-docs", "Scalar API Documentation", "scalar/v1");
    }

    internal static IResourceBuilder<T> WithRedoc<T>(this IResourceBuilder<T> builder)
    where T : IResourceWithEndpoints
    {
        return builder.WithOpenApiDocs("redoc-docs", "ReDoc API Documentation", "api-docs");
    }

    private static IResourceBuilder<T> WithOpenApiDocs<T>(this IResourceBuilder<T> builder, 
        string name, 
        string displayName,
        string openApiUiPath)
        where T : IResourceWithEndpoints
    {
        return builder.WithCommand(
    name,
    displayName,
    executeCommand: async _ =>
    {
        try
        {
            var endpoint = builder.GetEndpoint("https");
            var url = $"{endpoint.Url}/{openApiUiPath}";
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });

            return new ExecuteCommandResult { Success = true };
        }
        catch (Exception e)
        {
            return new ExecuteCommandResult
            {
                Success = false,
                Message = e.ToString()
            };
        }
    });
    }
}
