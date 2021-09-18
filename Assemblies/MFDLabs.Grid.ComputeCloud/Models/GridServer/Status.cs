using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace MFDLabs.Grid.ComputeCloud
{
    [GeneratedCode("svcutil", "4.6.1055.0")]
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [XmlType(Namespace = "http://roblox.com/")]
    public class Status
    {
        [XmlElement(Order = 0)]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        [XmlElement(Order = 1)]
        public int environmentCount
        {
            get
            {
                return this.environmentCountField;
            }
            set
            {
                this.environmentCountField = value;
            }
        }

        private string versionField;

        private int environmentCountField;
    }
}
