using System;
using System.CodeDom.Compiler;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MFDLabs.Grid.ComputeCloud
{
    [GeneratedCode("System.ServiceModel", "4.0.0.0")]
    [ServiceContract(Namespace = "http://roblox.com/", ConfigurationName = "RCCServiceSoap")]
    public interface ComputeCloudServiceSoap : IDisposable
    {
        [OperationContract(Action = "http://roblox.com/HelloWorld", ReplyAction = "*")]
        [XmlSerializerFormat]
        string HelloWorld();

        [OperationContract(Action = "http://roblox.com/HelloWorld", ReplyAction = "*")]
        Task<string> HelloWorldAsync();

        [OperationContract(Action = "http://roblox.com/GetVersion", ReplyAction = "*")]
        [XmlSerializerFormat]
        string GetVersion();

        [OperationContract(Action = "http://roblox.com/GetVersion", ReplyAction = "*")]
        Task<string> GetVersionAsync();

        [OperationContract(Action = "http://roblox.com/GetStatus", ReplyAction = "*")]
        [XmlSerializerFormat]
        Status GetStatus();

        [OperationContract(Action = "http://roblox.com/GetStatus", ReplyAction = "*")]
        Task<Status> GetStatusAsync();

        [OperationContract(Action = "http://roblox.com/OpenJob", ReplyAction = "*")]
        [XmlSerializerFormat]
        OpenJobResponse OpenJob(OpenJobRequest request);

        [OperationContract(Action = "http://roblox.com/OpenJob", ReplyAction = "*")]
        Task<OpenJobResponse> OpenJobAsync(OpenJobRequest request);

        [OperationContract(Action = "http://roblox.com/OpenJobEx", ReplyAction = "*")]
        [XmlSerializerFormat]
        LuaValue[] OpenJobEx(Job job, ScriptExecution script);

        [OperationContract(Action = "http://roblox.com/OpenJobEx", ReplyAction = "*")]
        Task<LuaValue[]> OpenJobExAsync(Job job, ScriptExecution script);

        [OperationContract(Action = "http://roblox.com/RenewLease", ReplyAction = "*")]
        [XmlSerializerFormat]
        double RenewLease(string jobID, double expirationInSeconds);

        [OperationContract(Action = "http://roblox.com/RenewLease", ReplyAction = "*")]
        Task<double> RenewLeaseAsync(string jobID, double expirationInSeconds);

        [OperationContract(Action = "http://roblox.com/Execute", ReplyAction = "*")]
        [XmlSerializerFormat]
        ExecuteResponse Execute(ExecuteRequest request);

        [OperationContract(Action = "http://roblox.com/Execute", ReplyAction = "*")]
        Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request);

        [OperationContract(Action = "http://roblox.com/ExecuteEx", ReplyAction = "*")]
        [XmlSerializerFormat]
        LuaValue[] ExecuteEx(string jobID, ScriptExecution script);

        [OperationContract(Action = "http://roblox.com/ExecuteEx", ReplyAction = "*")]
        Task<LuaValue[]> ExecuteExAsync(string jobID, ScriptExecution script);

        [OperationContract(Action = "http://roblox.com/CloseJob", ReplyAction = "*")]
        [XmlSerializerFormat]
        void CloseJob(string jobID);

        [OperationContract(Action = "http://roblox.com/CloseJob", ReplyAction = "*")]
        Task CloseJobAsync(string jobID);

        [OperationContract(Action = "http://roblox.com/BatchJob", ReplyAction = "*")]
        [XmlSerializerFormat]
        BatchJobResponse BatchJob(BatchJobRequest request);

        [OperationContract(Action = "http://roblox.com/BatchJob", ReplyAction = "*")]
        Task<BatchJobResponse> BatchJobAsync(BatchJobRequest request);

        [OperationContract(Action = "http://roblox.com/BatchJobEx", ReplyAction = "*")]
        [XmlSerializerFormat]
        LuaValue[] BatchJobEx(Job job, ScriptExecution script);

        [OperationContract(Action = "http://roblox.com/BatchJobEx", ReplyAction = "*")]
        Task<LuaValue[]> BatchJobExAsync(Job job, ScriptExecution script);

        [OperationContract(Action = "http://roblox.com/GetExpiration", ReplyAction = "*")]
        [XmlSerializerFormat]
        double GetExpiration(string jobID);

        [OperationContract(Action = "http://roblox.com/GetExpiration", ReplyAction = "*")]
        Task<double> GetExpirationAsync(string jobID);

        [OperationContract(Action = "http://roblox.com/GetAllJobs", ReplyAction = "*")]
        [XmlSerializerFormat]
        GetAllJobsResponse GetAllJobs(GetAllJobsRequest request);

        [OperationContract(Action = "http://roblox.com/GetAllJobs", ReplyAction = "*")]
        Task<GetAllJobsResponse> GetAllJobsAsync(GetAllJobsRequest request);

        [OperationContract(Action = "http://roblox.com/GetAllJobsEx", ReplyAction = "*")]
        [XmlSerializerFormat]
        Job[] GetAllJobsEx();

        [OperationContract(Action = "http://roblox.com/GetAllJobsEx", ReplyAction = "*")]
        Task<Job[]> GetAllJobsExAsync();

        [OperationContract(Action = "http://roblox.com/CloseExpiredJobs", ReplyAction = "*")]
        [XmlSerializerFormat]
        int CloseExpiredJobs();

        [OperationContract(Action = "http://roblox.com/CloseExpiredJobs", ReplyAction = "*")]
        Task<int> CloseExpiredJobsAsync();

        [OperationContract(Action = "http://roblox.com/CloseAllJobs", ReplyAction = "*")]
        [XmlSerializerFormat]
        int CloseAllJobs();

        [OperationContract(Action = "http://roblox.com/CloseAllJobs", ReplyAction = "*")]
        Task<int> CloseAllJobsAsync();

        [OperationContract(Action = "http://roblox.com/Diag", ReplyAction = "*")]
        [XmlSerializerFormat]
        DiagResponse Diag(DiagRequest request);

        [OperationContract(Action = "http://roblox.com/Diag", ReplyAction = "*")]
        Task<DiagResponse> DiagAsync(DiagRequest request);

        [OperationContract(Action = "http://roblox.com/DiagEx", ReplyAction = "*")]
        [XmlSerializerFormat]
        LuaValue[] DiagEx(int type, string jobID);

        [OperationContract(Action = "http://roblox.com/DiagEx", ReplyAction = "*")]
        Task<LuaValue[]> DiagExAsync(int type, string jobID);
    }
}
