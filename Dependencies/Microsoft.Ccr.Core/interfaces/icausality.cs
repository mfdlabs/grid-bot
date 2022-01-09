using System;

namespace Microsoft.Ccr.Core
{
    // Token: 0x02000005 RID: 5
    public interface ICausality
    {

        Guid Guid { get; }

        string Name { get; }

        IPort ExceptionPort { get; }

        IPort CoordinationPort { get; }
    }
}
