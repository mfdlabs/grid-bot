﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MFDLabs.Grid.Bot.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MFDLabs.Grid.Bot.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to local a=...local b=a[&apos;isAdmin&apos;]if not b then warn(&quot;We are in a VM state, blocking specific methods is expected.&quot;)local setfenv=setfenv;local getfenv=getfenv;local setmetatable=setmetatable;local getmetatable=getmetatable;local type=type;local select=select;local tostring=tostring;local newproxy=newproxy;local print=print;local next=next;local c={}c.__metatable=&quot;This debug metatable is locked.&quot;local d=nil;local e={}function c:__index(f)if f:lower()==&quot;getservice&quot;then return function(...)local g={...}local h=g [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SafeLuaMode {
            get {
                return ResourceManager.GetString("SafeLuaMode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to --[[
        ///File Name: SafeLuaMode.lua
        ///Written By: Malte0621#4433 and Nikita Petko (extra)
        ///Description: Disables specific things in the datamodel, by virualizing the fenv
        ///Modifications:
        ///	21/11/2021 01:16 =&gt; Removed the game to script check because it was returning nil (we aren&apos;t running under a script so it&apos;s nil)
        ///--]]
        ///
        ///local args = ...
        ///local isAdmin = args[&apos;isAdmin&apos;] -- might be able to be hacked, but we&apos;ll see
        ///
        ///if (not isAdmin) then
        ///	warn(&quot;We are in a VM state, blocking specific methods is expected.&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SafeLuaMode_formatted {
            get {
                return ResourceManager.GetString("SafeLuaMode_formatted", resourceCulture);
            }
        }
    }
}
