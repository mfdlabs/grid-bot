using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;

namespace MFDLabs.Grid.ComputeCloud
{
    [DebuggerStepThrough]
    [GeneratedCode("System.ServiceModel", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MessageContract(WrapperName = "OpenJob", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class OpenJobRequest
    {
        public OpenJobRequest()
        {
        }

        public OpenJobRequest(Job job, ScriptExecution script)
        {
            this.job = job;
            this.script = script;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        public Job job;

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 1)]
        public ScriptExecution script;
    }
}
