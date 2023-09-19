namespace Swagger.Utility;

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Static class for Swagger extensions.
/// </summary>
public static class SwaggerStartup
{
    private static string AppName => Environment.GetEnvironmentVariable("AppName");

    private const string V1StringValue = "v1";
    private const string ForwardedPrefixHeader = "X-Forwarded-Prefix";
    private const string DefaultSwaggerRouteTemplate = "swagger/{documentName}/swagger.json";
    private const string DefaultSwaggerJsonEndpoint = "/swagger/v1/swagger.json";

    /// <summary>
    /// Add swagger to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(
            c =>
            {
                c.SwaggerDoc(V1StringValue, new OpenApiInfo
                {
                    Title = AppName,
                    Version = V1StringValue
                });

                c.DescribeAllEnumsAsStrings();
                c.EnableAnnotations();
                c.UseReferencedDefinitionsForEnums();

                foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.xml"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    if (!File.Exists(Path.Combine(AppContext.BaseDirectory, $"{fileName}.dll"))) continue;

                    if (fileName == Assembly.GetEntryAssembly().GetName().Name)
                        c.IncludeXmlComments(file, true);
                    else
                        c.IncludeXmlComments(file);
                }
            }
        );

        services.ConfigureSwaggerGen(
            options =>
            {
                options.OperationFilter<OperationIdFilter>();
                options.OperationFilter<SwaggerRemoveCancellationTokenParameterFilter>();
            }
        );
    }

    /// <summary>
    /// Use swagger in the <see cref="IApplicationBuilder"/>
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/></param>
    public static void UseSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger(
            options =>
            {
                options.RouteTemplate = DefaultSwaggerRouteTemplate;
                options.PreSerializeFilters.Add(
                    (doc, request) =>
                    {
                        if (request.Headers.TryGetValue(ForwardedPrefixHeader, out var prefix))
                        {
                            var url = string.Format("{0}://{1}{2}", request.Scheme, request.Host.Value, prefix);

                            doc.Servers = new List<OpenApiServer>
                            {
                                new OpenApiServer
                                {
                                    Url = url
                                }
                            };
                        }
                    }
                );
            }
        );

        app.UseSwaggerUI(
            options =>
            {
                var prefix = string.IsNullOrWhiteSpace(options.RoutePrefix) 
                    ? "." 
                    : "..";

                options.SwaggerEndpoint(prefix + DefaultSwaggerJsonEndpoint, AppName);
            }
        );
    }
}
