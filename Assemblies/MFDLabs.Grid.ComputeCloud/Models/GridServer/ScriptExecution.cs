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
    public class ScriptExecution
    {
        [XmlElement(Order = 0)]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        [XmlElement(Order = 1)]
        public string script
        {
            get
            {
                return this.scriptField;
            }
            set
            {
                this.scriptField = value;
            }
        }

        [XmlArray(Order = 2)]
        public LuaValue[] arguments
        {
            get
            {
                return this.argumentsField;
            }
            set
            {
                this.argumentsField = value;
            }
        }

        private string nameField;

        private string scriptField;

        private LuaValue[] argumentsField;
    }
}
