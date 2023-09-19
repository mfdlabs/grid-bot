namespace Swagger.Utility;

using System.Linq;
using System.Threading;

using Microsoft.OpenApi.Models;
using Microsoft.Win32.SafeHandles;

using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// <see cref="IOperationFilter"/> to remove <see cref="CancellationToken"/>, <see cref="WaitHandle"/> and <see cref="SafeWaitHandle"/>s from schema.
/// </summary>
public class SwaggerRemoveCancellationTokenParameterFilter : IOperationFilter
{
    /// <inheritdoc cref="IOperationFilter.Apply(OpenApiOperation, OperationFilterContext)"/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        (from desc in context.ApiDescription.ParameterDescriptions
         where desc.ModelMetadata.ContainerType == typeof(CancellationToken) 
         || desc.ModelMetadata.ContainerType == typeof(WaitHandle) 
         || desc.ModelMetadata.ContainerType == typeof(SafeWaitHandle)
         select desc)
         .ToList()
         .ForEach(desc => operation.Parameters?.Remove(operation.Parameters.Single(p => p.Name == desc.Name)));
    }
}
