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
    public class Job
    {
        [XmlElement(Order = 0)]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        [XmlElement(Order = 1)]
        public double expirationInSeconds
        {
            get
            {
                return this.expirationInSecondsField;
            }
            set
            {
                this.expirationInSecondsField = value;
            }
        }

        [XmlElement(Order = 2)]
        public int category
        {
            get
            {
                return this.categoryField;
            }
            set
            {
                this.categoryField = value;
            }
        }

        [XmlElement(Order = 3)]
        public double cores
        {
            get
            {
                return this.coresField;
            }
            set
            {
                this.coresField = value;
            }
        }

        private string idField;

        private double expirationInSecondsField;

        private int categoryField;

        private double coresField;
    }
}
