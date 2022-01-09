using System;
using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public interface IPortSet : IPort
    {
        ICollection<IPort> Ports { get; }
        IPort this[Type portItemType] { get; }
        Port<object> SharedPort { get; }
        PortSetMode Mode { get; set; }

        T Test<T>();
    }
}
