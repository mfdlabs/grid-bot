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
    [MessageContract(WrapperName = "BatchJobResponse", WrapperNamespace = "http://roblox.com/", IsWrapped = true)]
    public class BatchJobResponse
    {
        public BatchJobResponse()
        {
        }

        public BatchJobResponse(LuaValue[] BatchJobResult)
        {
            this.BatchJobResult = BatchJobResult;
        }

        [MessageBodyMember(Namespace = "http://roblox.com/", Order = 0)]
        [XmlElement("BatchJobResult", IsNullable = true)]
        public LuaValue[] BatchJobResult;
    }
}
