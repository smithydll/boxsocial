﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4200
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Calendar.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Calendar.Properties.Resources", typeof(Resources).Assembly);
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
        
        internal static System.Drawing.Bitmap icon {
            get {
                object obj = ResourceManager.GetObject("icon", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to function MarkTaskComplete(id)
        ///{
        ///	var texts = PostToAccount(null, &quot;calendar&quot;, &quot;task-complete&quot;, id, null).responseText;
        ///	
        ///	return false;
        ///}
        ///.
        /// </summary>
        internal static string script {
            get {
                return ResourceManager.GetString("script", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to div.month 
        ///{
        ///}
        ///
        ///div.month div.week
        ///{
        ///	height: 77px;
        ///}
        ///
        ///div.month div.week-head
        ///{
        ///	height: 22px;
        ///}
        ///
        ///div.month div.week-head div.day
        ///{
        ///	float: left;
        ///	width: 14%;
        ///	height: 20px;
        ///	font-weight: bold;
        ///	text-align: center;
        ///	border: solid 1px #F7F7F7;
        ///}
        ///
        ///div.month div.week div.day
        ///{
        ///	float: left;
        ///	width: 14%;
        ///	height: 75px;
        ///	text-align: left;
        ///	border: solid 1px #F7F7F7;
        ///	background-color: #FFFFFF;
        ///}
        ///
        ///div.month div.week div.day ul
        ///{
        ///	list-style: none;
        ///	padding: 0px;
        ///	margin: 0 2 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string style {
            get {
                return ResourceManager.GetString("style", resourceCulture);
            }
        }
        
        internal static byte[] svgIcon {
            get {
                object obj = ResourceManager.GetObject("svgIcon", resourceCulture);
                return ((byte[])(obj));
            }
        }
    }
}