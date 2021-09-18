using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Text")]
[assembly: AssemblyDescription("A few singletons to help with text manip etc.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Text")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2019. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Who Owns This Sector? (R)")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]