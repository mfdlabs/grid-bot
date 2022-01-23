using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using MFDLabs.Grid.ComputeCloud;

namespace MFDLabs.Grid.Bot.Utility
{
    // This cannot follow the Singleton base pattern as it has a base class.
    // We could make a custom soap client that extends singleton base, but that is dumb.
    public sealed class SoapUtility : ComputeCloudServiceSoapClient
    {
        public static SoapUtility Singleton
        {
            get
            {
                if (_httpBinding == null)
                    throw new ApplicationException("The http binding was null, please call SetBinding()");
                return new SoapUtility(
                    _httpBinding,
                    new EndpointAddress("http://127.0.0.1:" + global::MFDLabs.Grid.Bot.Properties.Settings.Default
                        .SoapUtilityRemoteServicePort)
                );
            }
        }

        public static void SetBinding(Binding b) => _httpBinding = b;

        private static Binding _httpBinding;

        public SoapUtility(string endpointConfigurationName)
            : base(endpointConfigurationName)
        {
        }

        public SoapUtility(string endpointConfigurationName, string remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        public SoapUtility(string endpointConfigurationName, EndpointAddress remoteAddress)
            : base(endpointConfigurationName, remoteAddress)
        {
        }

        private SoapUtility(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }

        public new string HelloWorld()
        {
            try
            {
                return base.HelloWorld();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException) 
                    throw;

                SystemUtility.OpenGridServerSafe();
                return HelloWorld();
            }
        }

        public new async Task<string> HelloWorldAsync()
        {
            try
            {
                return await base.HelloWorldAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await HelloWorldAsync();
            }
        }

        public new string GetVersion()
        {
            try
            {
                return base.GetVersion();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return GetVersion();
            }
        }

        public new async Task<string> GetVersionAsync()
        {
            try
            {
                return await base.GetVersionAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await GetVersionAsync();
            }
        }

        public new Status GetStatus()
        {
            try
            {
                return base.GetStatus();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return GetStatus();
            }
        }

        public new async Task<Status> GetStatusAsync()
        {
            try
            {
                return await base.GetStatusAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await GetStatusAsync();
            }
        }

        public new LuaValue[] OpenJob(Job job, ScriptExecution script)
        {
            try
            {
                return base.OpenJob(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return OpenJob(job, script);
            }
        }

        public new async Task<OpenJobResponse> OpenJobAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.OpenJobAsync(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await OpenJobAsync(job, script);
            }
        }

        public new LuaValue[] OpenJobEx(Job job, ScriptExecution script)
        {
            try
            {
                return base.OpenJobEx(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return OpenJobEx(job, script);
            }
        }

        public new async Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.OpenJobExAsync(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await OpenJobExAsync(job, script);
            }
        }

        public new double RenewLease(string jobId, double expirationInSeconds)
        {
            try
            {
                return base.RenewLease(jobId, expirationInSeconds);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return RenewLease(jobId, expirationInSeconds);
            }
        }

        public new async Task<double> RenewLeaseAsync(string jobId, double expirationInSeconds)
        {
            try
            {
                return await base.RenewLeaseAsync(jobId, expirationInSeconds);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await RenewLeaseAsync(jobId, expirationInSeconds);
            }
        }

        public new LuaValue[] Execute(string jobId, ScriptExecution script)
        {
            try
            {
                return base.Execute(jobId, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return Execute(jobId, script);
            }
        }

        public new async Task<ExecuteResponse> ExecuteAsync(string jobId, ScriptExecution script)
        {
            try
            {
                return await base.ExecuteAsync(jobId, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await ExecuteAsync(jobId, script);
            }
        }

        public new LuaValue[] ExecuteEx(string jobId, ScriptExecution script)
        {
            try
            {
                return base.ExecuteEx(jobId, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return ExecuteEx(jobId, script);
            }
        }

        public new async Task<LuaValue[]> ExecuteExAsync(string jobId, ScriptExecution script)
        {
            try
            {
                return await base.ExecuteExAsync(jobId, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await ExecuteExAsync(jobId, script);
            }
        }

        public new void CloseJob(string jobId)
        {
            try
            {
                base.CloseJob(jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                CloseJob(jobId);
            }
        }

        public new async Task CloseJobAsync(string jobId)
        {
            try
            {
                await base.CloseJobAsync(jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                await CloseJobAsync(jobId);
            }
        }

        public new LuaValue[] BatchJob(Job job, ScriptExecution script)
        {
            try
            {
                return base.BatchJob(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return BatchJob(job, script);
            }
        }

        public new async Task<BatchJobResponse> BatchJobAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.BatchJobAsync(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await BatchJobAsync(job, script);
            }
        }

        public new LuaValue[] BatchJobEx(Job job, ScriptExecution script)
        {
            try
            {
                return base.BatchJobEx(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return BatchJobEx(job, script);
            }
        }

        public new async Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script)
        {
            try
            {
                return await base.BatchJobExAsync(job, script);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await BatchJobExAsync(job, script);
            }
        }

        public new double GetExpiration(string jobId)
        {
            try
            {
                return base.GetExpiration(jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return GetExpiration(jobId);
            }
        }

        public new async Task<double> GetExpirationAsync(string jobId)
        {
            try
            {
                return await base.GetExpirationAsync(jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await GetExpirationAsync(jobId);
            }
        }

        public new Job[] GetAllJobs()
        {
            try
            {
                return base.GetAllJobs();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return GetAllJobs();
            }
        }

        public new async Task<GetAllJobsResponse> GetAllJobsAsync()
        {
            try
            {
                return await base.GetAllJobsAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await GetAllJobsAsync();
            }
        }

        public new Job[] GetAllJobsEx()
        {
            try
            {
                return base.GetAllJobsEx();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return GetAllJobsEx();
            }
        }

        public new async Task<Job[]> GetAllJobsExAsync()
        {
            try
            {
                return await base.GetAllJobsExAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await GetAllJobsExAsync();
            }
        }

        public new int CloseExpiredJobs()
        {
            try
            {
                return base.CloseExpiredJobs();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return CloseExpiredJobs();
            }
        }

        public new async Task<int> CloseExpiredJobsAsync()
        {
            try
            {
                return await base.CloseExpiredJobsAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await CloseExpiredJobsAsync();
            }
        }

        public new int CloseAllJobs()
        {
            try
            {
                return base.CloseAllJobs();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return CloseAllJobs();
            }
        }

        public new async Task<int> CloseAllJobsAsync()
        {
            try
            {
                return await base.CloseAllJobsAsync();
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await CloseAllJobsAsync();
            }
        }

        public new LuaValue[] Diag(int type, string jobId)
        {
            try
            {
                return base.Diag(type, jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return Diag(type, jobId);
            }
        }

        public new async Task<DiagResponse> DiagAsync(int type, string jobId)
        {
            try
            {
                return await base.DiagAsync(type, jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await DiagAsync(type, jobId);
            }
        }

        public new LuaValue[] DiagEx(int type, string jobId)
        {
            try
            {
                return base.DiagEx(type, jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return DiagEx(type, jobId);
            }
        }

        public new async Task<LuaValue[]> DiagExAsync(int type, string jobId)
        {
            try
            {
                return await base.DiagExAsync(type, jobId);
            }
            catch (EndpointNotFoundException)
            {
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.OpenServiceOnEndpointNotFoundException)
                    throw;

                SystemUtility.OpenGridServerSafe();
                return await DiagExAsync(type, jobId);
            }
        }
    }
}