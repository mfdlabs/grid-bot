using System;
using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Discord.RbxUsers.Client")]
[assembly: AssemblyDescription("A HTTP Client for resolving the IUser instances by discord ID.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Discord.RbxUsers.Client")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2021. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Roblox (R)")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: AssemblyVersion("1.0.*")]