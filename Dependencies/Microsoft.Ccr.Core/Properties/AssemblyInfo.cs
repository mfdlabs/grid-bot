using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif

[assembly: ComVisible(false)]
[assembly: AssemblyCompany("Microsoft Corporation, and MFDLABS")]
[assembly: CLSCompliant(true)]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: AssemblyCopyright("Copyright (c) Microsoft Corporation. All rights reserved. Copyright (c) MFDLABS 2003 - 2021. All rights reserved.")]
[assembly: AssemblyProduct("Microsoft (R) Robotics")]
[assembly: AssemblyTrademark("Microsoft Corporation (R)")]
[assembly: AssemblyVersion("2.0.*")]