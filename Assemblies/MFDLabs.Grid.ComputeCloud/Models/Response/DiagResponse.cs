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
    [MessageContract(WrapperName = "DiagResponse", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class DiagResponse
    {
        public DiagResponse()
        {
        }

        public DiagResponse(LuaValue[] DiagResult)
        {
            this.DiagResult = DiagResult;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        [XmlElement("DiagResult", IsNullable = true)]
        public LuaValue[] DiagResult;
    }
}
