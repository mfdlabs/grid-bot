﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("GridExtensions")]
[assembly: AssemblyDescription("Shared Extensions for the MFDLABS.GRID.BOT")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("GridExtensions")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2022. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Grid (R)")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: AssemblyVersion("1.0.*")]