﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4016
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Musician {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Musician.Templates", typeof(Templates).Assembly);
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
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string account_discography {
            get {
                return ResourceManager.GetString("account_discography", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string account_gigs_manage {
            get {
                return ResourceManager.GetString("account_gigs_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string account_profile {
            get {
                return ResourceManager.GetString("account_profile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string account_tour_manage {
            get {
                return ResourceManager.GetString("account_tour_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Gig {CITY} {YEAR}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;dl&gt;
        ///  &lt;dt&gt;City&lt;/dt&gt;
        ///  &lt;dd&gt;{CITY}&lt;/dd&gt;
        ///  &lt;dt&gt;Venue&lt;/dt&gt;
        ///  &lt;dd&gt;{VENUE}&lt;/dd&gt;
        ///  &lt;dt&gt;Time&lt;/dt&gt;
        ///  &lt;dd&gt;{TIME}&lt;/dd&gt;
        ///&lt;/dl&gt;
        ///
        ///&lt;p&gt;{ABSTRACT}&lt;/p&gt;
        ///
        ///&lt;div class=&quot;comment-pane&quot;&gt;
        ///  &lt;!-- INCLUDE pane.comments.html --&gt;
        ///&lt;/div&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewgig {
            get {
                return ResourceManager.GetString("viewgig", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Musician&lt;/h2&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewmusician {
            get {
                return ResourceManager.GetString("viewmusician", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Song&lt;/h2&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewsong {
            get {
                return ResourceManager.GetString("viewsong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Tour&lt;/h2&gt;
        ///
        ///&lt;!-- IF gig_list --&gt;
        ///&lt;ul&gt;
        ///  &lt;!-- BEGIN gig_list --&gt;
        ///  &lt;li&gt;&lt;/li&gt;
        ///  &lt;!-- END gig_list --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewtour {
            get {
                return ResourceManager.GetString("viewtour", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Tours&lt;/h2&gt;
        ///
        ///&lt;!-- IF tour_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN tour_list --&gt;
        ///  &lt;li&gt;&lt;/li&gt;
        ///&lt;!-- END tour_list --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewtours {
            get {
                return ResourceManager.GetString("viewtours", resourceCulture);
            }
        }
    }
}
