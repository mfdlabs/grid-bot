using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;

namespace MFDLabs.Grid.ComputeCloud
{
    [DebuggerStepThrough]
    [GeneratedCode("System.ServiceModel", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MessageContract(WrapperName = "Diag", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class DiagRequest
    {
        public DiagRequest()
        {
        }

        public DiagRequest(int type, string jobID)
        {
            this.type = type;
            this.jobID = jobID;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        public int type;

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 1)]
        public string jobID;
    }
}
