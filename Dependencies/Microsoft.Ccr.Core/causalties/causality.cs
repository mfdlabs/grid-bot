using System;
using System.Diagnostics;

namespace Microsoft.Ccr.Core
{
    [DebuggerDisplay("Causality: {Name}")]
    public sealed class Causality : ICausality
    {
        public Guid Guid { get; }
        public bool BreakOnReceive
        {
            get => false;
            internal set {}
        }
        public string Name { get; }
        public IPort ExceptionPort { get; }
        public IPort CoordinationPort { get; }

        public Causality(string name) 
            : this(name, new Port<Exception>())
        {
        }
        public Causality(Guid guid) 
            : this(null, guid, new Port<Exception>(), null)
        {
        }
        public Causality(string name, IPort exceptionPort) 
            : this(name, exceptionPort, null)
        {
        }
        public Causality(string name, IPort exceptionPort, IPort coordinationPort)
        {
            Name = name;
            ExceptionPort = exceptionPort;
            CoordinationPort = coordinationPort;
            Guid = Guid.NewGuid();
        }
        public Causality(string name, Guid guid, IPort exceptionPort, IPort coordinationPort)
        {
            Guid = guid;
            Name = name;
            ExceptionPort = exceptionPort;
            CoordinationPort = coordinationPort;
        }
    }
}
