﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Google.Analytics.Client")]
[assembly: AssemblyDescription("Service Client for interacting with Google Analytics")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Google.Analytics.Client")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2021. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Metrics (R)")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: AssemblyVersion("1.0.*")]