using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;

namespace MFDLabs.Grid.ComputeCloud
{
    [DebuggerStepThrough]
    [GeneratedCode("System.ServiceModel", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MessageContract(WrapperName = "Execute", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class ExecuteRequest
    {
        public ExecuteRequest()
        {
        }

        public ExecuteRequest(string jobID, ScriptExecution script)
        {
            this.jobID = jobID;
            this.script = script;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        public string jobID;

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 1)]
        public ScriptExecution script;
    }
}
