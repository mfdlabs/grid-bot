using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Concurrency")]
[assembly: AssemblyDescription("MFDLABS Concurrency assemblies for Microsoft.Ccr.Core.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Concurrency")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2017. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Robotics (R)")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]