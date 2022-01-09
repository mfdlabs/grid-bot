﻿using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public interface ITask
    {
        object LinkedIterator { get; set; }
        Handler ArbiterCleanupHandler { get; set; }
        DispatcherQueue TaskQueue { get; set; }
        IPortElement this[int index] { get; set; }
        int PortElementCount { get; }

        ITask PartialClone();
        IEnumerator<ITask> Execute();
    }
}
