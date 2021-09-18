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
    public class LuaValue
    {
        [XmlElement(Order = 0)]
        public LuaType type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        [XmlElement(Order = 1)]
        public string value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }

        [XmlArray(Order = 2)]
        public LuaValue[] table
        {
            get
            {
                return this.tableField;
            }
            set
            {
                this.tableField = value;
            }
        }

        private LuaType typeField;

        private string valueField;

        private LuaValue[] tableField;
    }
}
