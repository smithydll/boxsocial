﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4200
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Gallery.Properties {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Gallery.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to var newTag = new Array(0,0,0);
        ///
        ///function CreateUserTagNearPointer(id, event)
        ///{
        ///	var photo = ge(&quot;photo-640&quot;);
        ///	photox = (event.offsetX) ? event.offsetX : event.pageX-document.getElementById(&quot;photo-640&quot;).offsetLeft;
        ///	photoy = (event.offsetY) ? event.offsetY : event.pageY-document.getElementById(&quot;photo-640&quot;).offsetTop;
        ///	
        ///	newTag[0] = photox;
        ///	newTag[1] = photoy;
        ///	
        ///	//PostToAccount(UserTagCreated, &quot;gallery&quot;, &quot;tag&quot;, id, null);
        ///	
        ///	var tags = ge(&quot;user-tags&quot;);
        ///	
        ///	var nli = document.createElement(&apos;li&apos; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string script {
            get {
                return ResourceManager.GetString("script", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ul#gallery-list {
        ///	list-style: none;
        ///	padding: 5px;
        ///	margin: 0px;
        ///}
        ///
        ///ul#gallery-list li {
        ///	background-color: #CCCCCC;
        ///	margin-bottom: 1px;
        ///}
        ///
        ///ul#gallery-list li dt {
        ///	float: left;
        ///	width: 180px;
        ///	padding: 3px;
        ///	text-align: center;
        ///}
        ///
        ///ul#gallery-list li dd {
        ///	margin-left: 180px;
        ///	padding: 3px;
        ///}
        ///
        ///ul#gallery-list li h3 {
        ///	padding: 0px;
        ///	margin: 0px;
        ///}
        ///
        ///ul#gallery-list li dt img {
        ///	padding: 3px;
        ///	background-color: White;
        ///	border: solid 1px black;
        ///	font-size: 0px;
        ///}
        ///
        ///ul#photo- [rest of string was truncated]&quot;;.
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