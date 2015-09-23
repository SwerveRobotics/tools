﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Org.SwerveRobotics.Tools.BotBug.Service.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Org.SwerveRobotics.Tools.BotBug.Service.Properties.Resources", typeof(Resources).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to last connected to {0}.
        /// </summary>
        public static string LastConnectedToMessage {
            get {
                return ResourceManager.GetString("LastConnectedToMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to no remembered connection.
        /// </summary>
        public static string NoLastConnectedToMessage {
            get {
                return ResourceManager.GetString("NoLastConnectedToMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BotBug is monitoring connections.
        /// </summary>
        public static string NotifyArmed {
            get {
                return ResourceManager.GetString("NotifyArmed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connected to phone {0} at {1}.
        /// </summary>
        public static string NotifyConnected {
            get {
                return ResourceManager.GetString("NotifyConnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to connected to phone {0} at {1}.
        /// </summary>
        public static string NotifyConnectedFail {
            get {
                return ResourceManager.GetString("NotifyConnectedFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BotBut is not monitoring connections.
        /// </summary>
        public static string NotifyDisarmed {
            get {
                return ResourceManager.GetString("NotifyDisarmed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Phone {0} has no IP address.
        /// </summary>
        public static string NotifyNoIPAddress {
            get {
                return ResourceManager.GetString("NotifyNoIPAddress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Phone {0} at {1} cannot be reached on the network from this computer.
        /// </summary>
        public static string NotifyNotPingable {
            get {
                return ResourceManager.GetString("NotifyNotPingable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reconnected to phone {0} at {1}.
        /// </summary>
        public static string NotifyReconnected {
            get {
                return ResourceManager.GetString("NotifyReconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to reconnect to phone {0} at {1}.
        /// </summary>
        public static string NotifyReconnectedFail {
            get {
                return ResourceManager.GetString("NotifyReconnectedFail", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Phone {0} at {1} has WIFI turned off.
        /// </summary>
        public static string NotifyWifiOff {
            get {
                return ResourceManager.GetString("NotifyWifiOff", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Swerve Tools BotBug has started.
        /// </summary>
        public static string StartingMessage {
            get {
                return ResourceManager.GetString("StartingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Swerve Tools BotBug is stopping.
        /// </summary>
        public static string StoppingMessage {
            get {
                return ResourceManager.GetString("StoppingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to armed.
        /// </summary>
        public static string WordArmed {
            get {
                return ResourceManager.GetString("WordArmed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to disarmed.
        /// </summary>
        public static string WordDisarmed {
            get {
                return ResourceManager.GetString("WordDisarmed", resourceCulture);
            }
        }
    }
}
