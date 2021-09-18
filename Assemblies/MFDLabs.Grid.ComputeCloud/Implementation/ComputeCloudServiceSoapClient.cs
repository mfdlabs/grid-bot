using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace MFDLabs.Grid.ComputeCloud
{
    [DebuggerStepThrough]
    [GeneratedCode("System.ServiceModel", "4.0.0.0")]
    public class ComputeCloudServiceSoapClient : ClientBase<ComputeCloudServiceSoap>, ComputeCloudServiceSoap, IDisposable
    {
        public ComputeCloudServiceSoapClient()
        {
        }

        public ComputeCloudServiceSoapClient(string endpointConfigurationName) : base(endpointConfigurationName)
        {
        }

        public ComputeCloudServiceSoapClient(string endpointConfigurationName, string remoteAddress) : base(endpointConfigurationName, remoteAddress)
        {
        }

        public ComputeCloudServiceSoapClient(string endpointConfigurationName, EndpointAddress remoteAddress) : base(endpointConfigurationName, remoteAddress)
        {
        }

        public ComputeCloudServiceSoapClient(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress)
        {
        }

        public string HelloWorld()
        {
            return base.Channel.HelloWorld();
        }

        public Task<string> HelloWorldAsync()
        {
            return base.Channel.HelloWorldAsync();
        }

        public string GetVersion()
        {
            return base.Channel.GetVersion();
        }

        public Task<string> GetVersionAsync()
        {
            return base.Channel.GetVersionAsync();
        }

        public Status GetStatus()
        {
            return base.Channel.GetStatus();
        }

        public Task<Status> GetStatusAsync()
        {
            return base.Channel.GetStatusAsync();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        OpenJobResponse ComputeCloudServiceSoap.OpenJob(OpenJobRequest request)
        {
            return base.Channel.OpenJob(request);
        }

        public LuaValue[] OpenJob(Job job, ScriptExecution script)
        {
            return ((ComputeCloudServiceSoap)this).OpenJob(new OpenJobRequest
            {
                job = job,
                script = script
            }).OpenJobResult;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Task<OpenJobResponse> ComputeCloudServiceSoap.OpenJobAsync(OpenJobRequest request)
        {
            return base.Channel.OpenJobAsync(request);
        }

        public Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script)
        {
            return ((ComputeCloudServiceSoap)this).OpenJobAsync(new OpenJobRequest
            {
                job = job,
                script = script
            });
        }

        public LuaValue[] OpenJobEx(Job job, ScriptExecution script)
        {
            return base.Channel.OpenJobEx(job, script);
        }

        public Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script)
        {
            return base.Channel.OpenJobExAsync(job, script);
        }

        public double RenewLease(string jobID, double expirationInSeconds)
        {
            return base.Channel.RenewLease(jobID, expirationInSeconds);
        }

        public Task<double> RenewLeaseAsync(string jobID, double expirationInSeconds)
        {
            return base.Channel.RenewLeaseAsync(jobID, expirationInSeconds);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        ExecuteResponse ComputeCloudServiceSoap.Execute(ExecuteRequest request)
        {
            return base.Channel.Execute(request);
        }

        public LuaValue[] Execute(string jobID, ScriptExecution script)
        {
            return ((ComputeCloudServiceSoap)this).Execute(new ExecuteRequest
            {
                jobID = jobID,
                script = script
            }).ExecuteResult;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Task<ExecuteResponse> ComputeCloudServiceSoap.ExecuteAsync(ExecuteRequest request)
        {
            return base.Channel.ExecuteAsync(request);
        }

        public Task<ExecuteResponse> ExecuteAsync(string jobID, ScriptExecution script)
        {
            return ((ComputeCloudServiceSoap)this).ExecuteAsync(new ExecuteRequest
            {
                jobID = jobID,
                script = script
            });
        }

        public LuaValue[] ExecuteEx(string jobID, ScriptExecution script)
        {
            return base.Channel.ExecuteEx(jobID, script);
        }

        public Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script)
        {
            return base.Channel.ExecuteExAsync(jobID, script);
        }

        public void CloseJob(string jobID)
        {
            base.Channel.CloseJob(jobID);
        }

        public Task CloseJobAsync(string jobID)
        {
            return base.Channel.CloseJobAsync(jobID);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        BatchJobResponse ComputeCloudServiceSoap.BatchJob(BatchJobRequest request)
        {
            return base.Channel.BatchJob(request);
        }

        public LuaValue[] BatchJob(Job job, ScriptExecution script)
        {
            return ((ComputeCloudServiceSoap)this).BatchJob(new BatchJobRequest
            {
                job = job,
                script = script
            }).BatchJobResult;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Task<BatchJobResponse> ComputeCloudServiceSoap.BatchJobAsync(BatchJobRequest request)
        {
            return base.Channel.BatchJobAsync(request);
        }

        public Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script)
        {
            return ((ComputeCloudServiceSoap)this).BatchJobAsync(new BatchJobRequest
            {
                job = job,
                script = script
            });
        }

        public LuaValue[] BatchJobEx(Job job, ScriptExecution script)
        {
            return base.Channel.BatchJobEx(job, script);
        }

        public Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script)
        {
            return base.Channel.BatchJobExAsync(job, script);
        }

        public double GetExpiration(string jobID)
        {
            return base.Channel.GetExpiration(jobID);
        }

        public Task<double> GetExpirationAsync(string jobID)
        {
            return base.Channel.GetExpirationAsync(jobID);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        GetAllJobsResponse ComputeCloudServiceSoap.GetAllJobs(GetAllJobsRequest request)
        {
            return base.Channel.GetAllJobs(request);
        }

        public Job[] GetAllJobs()
        {
            GetAllJobsRequest request = new GetAllJobsRequest();
            return ((ComputeCloudServiceSoap)this).GetAllJobs(request).GetAllJobsResult;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Task<GetAllJobsResponse> ComputeCloudServiceSoap.GetAllJobsAsync(GetAllJobsRequest request)
        {
            return base.Channel.GetAllJobsAsync(request);
        }

        public Task<GetAllJobsResponse> GetAllJobsAsync()
        {
            GetAllJobsRequest request = new GetAllJobsRequest();
            return ((ComputeCloudServiceSoap)this).GetAllJobsAsync(request);
        }

        public Job[] GetAllJobsEx()
        {
            return base.Channel.GetAllJobsEx();
        }

        public Task<Job[]> GetAllJobsExAsync()
        {
            return base.Channel.GetAllJobsExAsync();
        }

        public int CloseExpiredJobs()
        {
            return base.Channel.CloseExpiredJobs();
        }

        public Task<int> CloseExpiredJobsAsync()
        {
            return base.Channel.CloseExpiredJobsAsync();
        }

        public int CloseAllJobs()
        {
            return base.Channel.CloseAllJobs();
        }

        public Task<int> CloseAllJobsAsync()
        {
            return base.Channel.CloseAllJobsAsync();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        DiagResponse ComputeCloudServiceSoap.Diag(DiagRequest request)
        {
            return base.Channel.Diag(request);
        }

        public LuaValue[] Diag(int type, string jobID)
        {
            return ((ComputeCloudServiceSoap)this).Diag(new DiagRequest
            {
                type = type,
                jobID = jobID
            }).DiagResult;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        Task<DiagResponse> ComputeCloudServiceSoap.DiagAsync(DiagRequest request)
        {
            return base.Channel.DiagAsync(request);
        }

        public Task<DiagResponse> DiagAsync(int type, string jobID)
        {
            return ((ComputeCloudServiceSoap)this).DiagAsync(new DiagRequest
            {
                type = type,
                jobID = jobID
            });
        }

        public LuaValue[] DiagEx(int type, string jobID)
        {
            return base.Channel.DiagEx(type, jobID);
        }

        public Task<LuaValue[]> DiagExAsync(int type, string jobID)
        {
            return base.Channel.DiagExAsync(type, jobID);
        }
    }
}
