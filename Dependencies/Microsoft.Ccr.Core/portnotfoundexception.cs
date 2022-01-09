using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

#pragma warning disable CS0618
#pragma warning disable SYSLIB0003

namespace Microsoft.Ccr.Core
{
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
    [Serializable]
    public class PortNotFoundException : Exception
    {
        public IPort Port
        {
            get
            {
                return _port;
            }
        }

        public object ObjectPosted
        {
            get
            {
                return _objectPosted;
            }
        }

        public PortNotFoundException()
        {
        }

        protected PortNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public PortNotFoundException(string message) : base(message)
        {
        }

        public PortNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public PortNotFoundException(IPort port, object posted, string message) : base(message)
        {
            _port = port ?? throw new ArgumentNullException("port");
            _objectPosted = posted ?? throw new ArgumentNullException("posted");
        }

        public PortNotFoundException(IPort port, object posted) : this(port, posted, (posted != null) ? ("Type not expected: " + posted.GetType().FullName) : "Unknown type not expected")
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        private readonly IPort _port;

        private readonly object _objectPosted;
    }
}
