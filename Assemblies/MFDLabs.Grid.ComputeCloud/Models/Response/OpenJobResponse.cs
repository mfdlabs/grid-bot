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
    [MessageContract(WrapperName = "OpenJobResponse", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class OpenJobResponse
    {
        public OpenJobResponse()
        {
        }

        public OpenJobResponse(LuaValue[] OpenJobResult)
        {
            this.OpenJobResult = OpenJobResult;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        [XmlElement("OpenJobResult")]
        public LuaValue[] OpenJobResult;
    }
}
