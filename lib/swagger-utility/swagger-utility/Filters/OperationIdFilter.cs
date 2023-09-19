namespace Swagger.Utility;

using System;
using System.Reflection;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Annotations;

/// <summary>
/// <see cref="IOperationFilter"/> for <see cref="OpenApiOperation.OperationId"/>
/// </summary>
public class OperationIdFilter : IOperationFilter
{
    /// <inheritdoc cref="IOperationFilter.Apply(OpenApiOperation, OperationFilterContext)"/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        try
        {
            if (
                context.MethodInfo.GetCustomAttribute(typeof(SwaggerOperationAttribute)) 
                is not SwaggerOperationAttribute attribute 
                || string.IsNullOrEmpty(attribute.OperationId))
            {
                var name = context.MethodInfo.ReflectedType.Name;
                name = name.Replace("Controller", "");

                operation.OperationId = name + "_" + context.MethodInfo.Name;
            }
        }
        catch
        {
        }
    }
}
