using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Logging")]
[assembly: AssemblyDescription("A few singletons to help in the aid of logging development.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Logging")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2019. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Who Owns This Sector? (R)")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]