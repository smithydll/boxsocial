﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.GuestBook {
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
    internal class Templates {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Templates() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.GuestBook.Templates", typeof(Templates).Assembly);
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
        
        internal static byte[] user_guestbook_notification {
            get {
                object obj = ResourceManager.GetObject("user_guestbook_notification", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{L_GUESTBOOK}&lt;/h2&gt;
        ///	&lt;div id=&quot;profile-comments&quot; class=&quot;pane&quot;&gt;
        ///	&lt;!-- IF BREADCRUMBS --&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;div class=&quot;comment-pane&quot;&gt;
        ///		&lt;!-- INCLUDE pane.comments.html --&gt;
        ///	&lt;/div&gt;
        ///	&lt;/div&gt;
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewguestbook {
            get {
                return ResourceManager.GetString("viewguestbook", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div id=&quot;profile-comments&quot; class=&quot;pane&quot;&gt;
        ///	&lt;div class=&quot;comment-pane&quot;&gt;
        ///		&lt;!-- INCLUDE pane.comments.html --&gt;
        ///	&lt;/div&gt;
        ///&lt;/div&gt;.
        /// </summary>
        internal static string viewprofileguestbook {
            get {
                return ResourceManager.GetString("viewprofileguestbook", resourceCulture);
            }
        }
    }
}
