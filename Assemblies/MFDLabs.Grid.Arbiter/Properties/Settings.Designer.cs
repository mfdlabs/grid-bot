﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MFDLabs.Grid.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("53640")]
        public int SingleInstancedArbiterRemoteServicePort {
            get {
                return ((int)(this["SingleInstancedArbiterRemoteServicePort"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool OpenServiceOnEndpointNotFoundException {
            get {
                return ((bool)(this["OpenServiceOnEndpointNotFoundException"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool HideProcessWindows {
            get {
                return ((bool)(this["HideProcessWindows"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool SingleInstancedGridServer {
            get {
                return ((bool)(this["SingleInstancedGridServer"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts {
            get {
                return ((bool)(this["GridServerArbiterInstanceThrowExceptionIfReachedMaxAttempts"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("00:15:00")]
        public global::System.TimeSpan DefaultLeasedGridServerInstanceLease {
            get {
                return ((global::System.TimeSpan)(this["DefaultLeasedGridServerInstanceLease"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("lms.simulpong.com")]
        public string WebServerLookupHost {
            get {
                return ((string)(this["WebServerLookupHost"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("robloxup")]
        public string WebServerHealthCheckExpectedResponseText {
            get {
                return ((string)(this["WebServerHealthCheckExpectedResponseText"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool WebServerBuildBeforeRun {
            get {
                return ((bool)(this["WebServerBuildBeforeRun"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Roblox\\WebSrv")]
        public string WebServerWorkspacePath {
            get {
                return ((string)(this["WebServerWorkspacePath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\ROBLOX Corporation\\Roblox")]
        public string GridServerRegistryKeyName {
            get {
                return ((string)(this["GridServerRegistryKeyName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("RccServicePath")]
        public string GridServerRegistryValueName {
            get {
                return ((string)(this["GridServerRegistryValueName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("RCCService.exe")]
        public string GridServerExecutableName {
            get {
                return ((string)(this["GridServerExecutableName"]));
            }
        }
    }
}