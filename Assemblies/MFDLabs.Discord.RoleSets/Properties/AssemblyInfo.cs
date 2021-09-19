using System;
using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("MFDLabs.Discord.RoleSets")]
[assembly: AssemblyDescription("Role Set helpers for discord.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("MFDLabs.Discord.RoleSets")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2019. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Who Owns This Sector? (R)")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: AssemblyVersion("1.0.*")]