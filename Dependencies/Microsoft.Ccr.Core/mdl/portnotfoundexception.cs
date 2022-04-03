﻿using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Microsoft.Ccr.Core
{
    [Serializable]
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
    public class PortNotFoundException : Exception
    {
        public IPort Port => _port;
        public object ObjectPosted => _objectPosted;

        public PortNotFoundException() { }
        protected PortNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        { }
        public PortNotFoundException(string message) 
            : base(message)
        { }
        public PortNotFoundException(string message, Exception innerException) 
            : base(message, innerException)
        { }
        public PortNotFoundException(IPort port, object posted, string message) : base(message)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _objectPosted = posted ?? throw new ArgumentNullException(nameof(posted));
        }
        public PortNotFoundException(IPort port, object posted) 
            : this(port, posted, posted != null ? $"Type not expected: {posted.GetType().FullName}" : "Unknown type not expected")
        { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context) => base.GetObjectData(info, context);

        private readonly IPort _port;
        private readonly object _objectPosted;
    }
}