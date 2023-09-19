namespace Grid.Arbiter.Service;

using System;
using System.Threading.Tasks;

using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

using V1;
using ComputeCloud;
using Text.Extensions;

/// <summary>
/// Implementation for <see cref="SoapGatewayAPI.SoapGatewayAPIBase"/>
/// </summary>
public class SoapGatewayApi : SoapGatewayAPI.SoapGatewayAPIBase
{
    private readonly IGridServerArbiter _Arbiter;

    /// <summary>
    /// Construct a new instance of <see cref="SoapGatewayApi"/>
    /// </summary>
    /// <param name="arbiter">The <see cref="IGridServerArbiter"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="arbiter"/> cannot be null.</exception>
    public SoapGatewayApi(IGridServerArbiter arbiter)
    {
        _Arbiter = arbiter ?? throw new ArgumentNullException(nameof(arbiter));
    }

    /// <inheritdoc cref="SoapGatewayAPI.SoapGatewayAPIBase.HelloWorld(EmptyGatewayRequest, ServerCallContext)"/>
    public override async Task<HelloWorldGatewayResponse> HelloWorld(EmptyGatewayRequest request, ServerCallContext context)
    {
        if (request.InstanceId.IsNullOrEmpty())
            throw new RpcException(new(StatusCode.InvalidArgument, "The instance ID cannot be null empty!"));

        var instance = _Arbiter.GetInstance(request.InstanceId);
        if (instance == null)
            throw new RpcException(new(StatusCode.NotFound, $"Unknown instance with ID [{request.InstanceId}]"));

        var result = await instance.HelloWorldAsync();

        return new()
        {
            Instance = GetInstanceModel(instance),
            SoapResponse = new() { Message = result }
        };
    }

    private static GridServerInstance GetInstanceModel(IGridServerInstance instance)
    {
        var model = new GridServerInstance
        {
            Id = instance.Name,
            Available = instance.IsAvailable,
            Open = instance.IsOpened,
            Poolable = instance.IsPoolable,
            Persistent = instance.Persistent
        };

        if (instance is ILeasedGridServerInstance leasedInstance)
        {
            model.Leased = true;
            model.Expiration = Timestamp.FromDateTime(leasedInstance.Expiration);
            model.Lease = Duration.FromTimeSpan(leasedInstance.Lease);
            model.HasLease = leasedInstance.HasLease;
            model.Expired = leasedInstance.IsExpired;
        }

        return model;
    }
}
