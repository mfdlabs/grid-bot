﻿using System;
using System.Diagnostics;

namespace Microsoft.Ccr.Core
{
    [DebuggerDisplay("Causality: {_name}")]
    public sealed class Causality : ICausality
    {
        public Guid Guid
        {
            get
            {
                return _guid;
            }
            set
            {
                _guid = value;
            }
        }

        public bool BreakOnReceive
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public IPort ExceptionPort
        {
            get
            {
                return _exceptionPort;
            }
        }

        public IPort CoordinationPort
        {
            get
            {
                return _coordinationPort;
            }
        }

        public Causality(string name) : this(name, new Port<Exception>())
        {
        }

        public Causality(Guid guid) : this(null, guid, new Port<Exception>(), null)
        {
        }

        public Causality(string name, IPort exceptionPort) : this(name, exceptionPort, null)
        {
        }

        public Causality(string name, IPort exceptionPort, IPort coordinationPort)
        {
            _name = name;
            _exceptionPort = exceptionPort;
            _coordinationPort = coordinationPort;
            _guid = Guid.NewGuid();
        }

        public Causality(string name, Guid guid, IPort exceptionPort, IPort coordinationPort)
        {
            _guid = guid;
            _name = name;
            _exceptionPort = exceptionPort;
            _coordinationPort = coordinationPort;
        }

        private Guid _guid;

        private readonly string _name;

        private readonly IPort _exceptionPort;

        private readonly IPort _coordinationPort;
    }
}
