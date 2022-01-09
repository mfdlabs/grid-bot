﻿using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    public delegate IEnumerator<ITask> IteratorHandler();
    public delegate IEnumerator<ITask> IteratorHandler<in T0>(T0 parameter0);
    public delegate IEnumerator<ITask> IteratorHandler<in T0, in T1>(T0 parameter0, T1 parameter1);
    public delegate IEnumerator<ITask> IteratorHandler<in T0, in T1, in T2>(T0 parameter0, T1 parameter1, T2 parameter2);
}
