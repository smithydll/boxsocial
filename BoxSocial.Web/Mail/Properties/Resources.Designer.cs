﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Mail.Properties {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Mail.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to 
        ///var sendingMessage = false;
        ///function SendMessage(id, text) {
        ///    if (text != &apos;&apos;) {
        ///        sendingMessage = true;
        ///        PostToPage(SubmitedMessage, &quot;account&quot;, null, { ajax: &quot;true&quot;, id: id, module: &quot;mail&quot;, sub: &quot;compose&quot;, mode: &quot;reply&quot;, message: text, &apos;newest-id&apos;: nid, save: &apos;true&apos; });
        ///    }
        ///    return false;
        ///}
        ///
        ///function SubmitedMessage(r, e) {
        ///    $(&apos;#posts&apos;).append(r[&apos;template&apos;]);
        ///    $(&apos;#posts&apos;).scrollTop($(&apos;#posts&apos;).prop(&quot;scrollHeight&quot;));
        ///    $(&apos;.comment-textarea&apos;).val(&apos;&apos;);
        ///    if (r[&apos;ne [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string script {
            get {
                return ResourceManager.GetString("script", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ul#inbox-messages dd div {
        ///	border-left: 1px solid #CCCCCC;
        ///}
        ///
        ///ul#inbox-messages li#inbox-head dd div {
        ///	border: none !important;
        ///}
        ///
        ///ul#inbox-messages {
        ///	list-style: none;
        ///	padding: 0px;
        ///	margin: 5px;
        ///	border: solid 2px #333333;
        ///}
        ///
        ///ul#inbox-messages li {
        ///	display: block;
        ///	list-style-type: none;
        ///	margin: 0;
        ///	border-bottom: 1px solid #CCCCCC;
        ///}
        ///
        ///ul#inbox-messages li div {
        ///	margin: 5px;
        ///}
        ///
        ///ul#inbox-messages li#inbox-head {
        ///	font-weight: bold;
        ///	color: #FFFFFF;
        ///	background-color: #33 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string style {
            get {
                return ResourceManager.GetString("style", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] svgIcon {
            get {
                object obj = ResourceManager.GetObject("svgIcon", resourceCulture);
                return ((byte[])(obj));
            }
        }
    }
}
