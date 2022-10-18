using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Wcf")]
[assembly: AssemblyDescription("Windows Communication Foundation related items like ServiceHostApp and ServiceHostInstaller")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Wcf")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2022. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Windows Developers (R)")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]