﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Diner_Smash.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.6.0.0")]
    internal sealed partial class DinerSmash : global::System.Configuration.ApplicationSettingsBase {
        
        private static DinerSmash defaultInstance = ((DinerSmash)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new DinerSmash())));
        
        public static DinerSmash Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1024")]
        public int GraphicsWidth {
            get {
                return ((int)(this["GraphicsWidth"]));
            }
            set {
                this["GraphicsWidth"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("768")]
        public int GraphicsHeight {
            get {
                return ((int)(this["GraphicsHeight"]));
            }
            set {
                this["GraphicsHeight"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int WindowMode {
            get {
                return ((int)(this["WindowMode"]));
            }
            set {
                this["WindowMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EnableShadows {
            get {
                return ((bool)(this["EnableShadows"]));
            }
            set {
                this["EnableShadows"] = value;
            }
        }
    }
}
