using MFDLabs.Pipeline;
using MFDLabs.Text.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Http.Client
{
    public class MachineIdHandler : PipelineHandler<IHttpRequest, IHttpResponse>
    {
        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            var machineId = GetMachineId();
            if (!machineId.IsNullOrWhiteSpace())
            {
                context.Input.Headers.AddOrUpdate(_MachineIdHeaderName, machineId);
            }
            base.Invoke(context);
        }

        public override Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            var machineId = GetMachineId();
            if (!machineId.IsNullOrWhiteSpace())
            {
                context.Input.Headers.AddOrUpdate(_MachineIdHeaderName, machineId);
            }
            return base.InvokeAsync(context, cancellationToken);
        }

        private string GetMachineId()
        {
            return Environment.MachineName;
        }

        private const string _MachineIdHeaderName = "Roblox-Machine-Id";
    }
}
