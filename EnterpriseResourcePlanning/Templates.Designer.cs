﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.EnterpriseResourcePlanning {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.EnterpriseResourcePlanning.Templates", typeof(Templates).Assembly);
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
        internal static string account_erp_document_templates_add {
            get {
                return ResourceManager.GetString("account_erp_document_templates_add", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string account_erp_document_templates_manage {
            get {
                return ResourceManager.GetString("account_erp_document_templates_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Enterprise Resource Planning Permissions&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;/account/&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Enterprise Resource Planning Permissions&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label&gt;Permissions&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				{S_ERP_PERMS}
        ///			&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;submit&quot; name=&quot;save&quot; value=&quot;Save&quot; /&gt;&lt;/dd&gt;
        ///		&lt;/dl&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;module&quot; value=&quot;erp&quot; /&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;sub&quot; value=&quot;permissions&quot; /&gt;
        ///	&lt;/fieldset&gt;
        ///&lt;/form&gt;.
        /// </summary>
        internal static string account_erp_permissions {
            get {
                return ResourceManager.GetString("account_erp_permissions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Enterprise Resource Planning Settings&lt;/h3&gt;.
        /// </summary>
        internal static string account_erp_settings {
            get {
                return ResourceManager.GetString("account_erp_settings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;h2&gt;{DOCUMENT_NUMBER} {L_REV} {DOCUMENT_REVISION} - {DOCUMENT_TITLE}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- BEGIN revision_list --&gt;
        ///
        ///&lt;!-- END revision_list --&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewdocument {
            get {
                return ResourceManager.GetString("viewdocument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;h2&gt;Documents&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;p&gt;{PAGINATION}&lt;/p&gt;
        ///
        ///&lt;!-- IF document_list --&gt;
        ///&lt;table&gt;
        ///	&lt;tr&gt;
        ///	&lt;/tr&gt;
        ///&lt;!-- BEGIN document_list --&gt;
        ///&lt;!-- END document_list --&gt;
        ///&lt;/table&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;p&gt;There are no documents.&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;p&gt;{PAGINATION}&lt;/p&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewdocuments {
            get {
                return ResourceManager.GetString("viewdocuments", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;h2&gt;{PROJECT_TITLE}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewproject {
            get {
                return ResourceManager.GetString("viewproject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;h2&gt;{VENDOR_TITLE}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewvendor {
            get {
                return ResourceManager.GetString("viewvendor", resourceCulture);
            }
        }
    }
}
