using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("Diagnostics")]
[assembly: AssemblyDescription("Stuff related for primarily processes!")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("Diagnostics")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2019. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Tech Team (R)")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]