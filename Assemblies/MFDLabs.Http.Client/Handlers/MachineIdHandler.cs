﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Http.Client
{
    public class MachineIdHandler : PipelineHandler<IHttpRequest, IHttpResponse>
    {
        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            var machineId = GetMachineId();
            if (!machineId.IsNullOrWhiteSpace()) context.Input.Headers.AddOrUpdate(MachineIdHeaderName, machineId);
            base.Invoke(context);
        }
        public override Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            var machineId = GetMachineId();
            if (!machineId.IsNullOrWhiteSpace()) context.Input.Headers.AddOrUpdate(MachineIdHeaderName, machineId);
            return base.InvokeAsync(context, cancellationToken);
        }
        private static string GetMachineId() => Environment.MachineName;

        private const string MachineIdHeaderName = "Roblox-Machine-Id";
    }
}