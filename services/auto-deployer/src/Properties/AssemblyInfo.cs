using System.Reflection;
using System.Runtime.InteropServices;

#if !DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: CompilationRelaxations(8)]
#endif
[assembly: AssemblyTitle("Grid.AutoDeployer")]
[assembly: AssemblyDescription("A windows service that polls Github Cloud or Github Enterprise for new releases on a repository.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("MFDLABS")]
[assembly: AssemblyProduct("Grid.AutoDeployer")]
[assembly: AssemblyCopyright("Copyright © MFDLABS 2022. All rights reserved.")]
[assembly: AssemblyTrademark("MFDLABS Grid Team (R)")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]