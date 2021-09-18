using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using System.Xml.Serialization;

namespace MFDLabs.Grid.ComputeCloud
{
    [DebuggerStepThrough]
    [GeneratedCode("System.ServiceModel", "4.0.0.0")]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MessageContract(WrapperName = "ExecuteResponse", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class ExecuteResponse
    {
        public ExecuteResponse()
        {
        }

        public ExecuteResponse(LuaValue[] ExecuteResult)
        {
            this.ExecuteResult = ExecuteResult;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        [XmlElement("ExecuteResult", IsNullable = true)]
        public LuaValue[] ExecuteResult;
    }
}
