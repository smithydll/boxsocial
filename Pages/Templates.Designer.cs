﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Pages {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Pages.Templates", typeof(Templates).Assembly);
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
        ///   Looks up a localized string similar to &lt;h3&gt;Edit List&lt;/h3&gt;
        ///&lt;form action=&quot;/account/&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Edit List&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;List Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; value=&quot;{S_LIST_TITLE}&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;type&quot;&gt;List Type&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_LIST_TYPES}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;abstract&quot;&gt;List Abstract&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;textarea id=&quot;abstract&quot; name=&quot;abstract&quot; style=&quot;margin: 0px; width: 100%; height: 50px; border: solid 1px #666666;&quot; cols= [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_list_edit {
            get {
                return ResourceManager.GetString("account_list_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Lists&lt;/h3&gt;
        ///&lt;p&gt;Lists let you stored structured data such as your favourite bands, books, and movies.&lt;/p&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;List Title&lt;/th&gt;
        ///	&lt;th&gt;List Type&lt;/th&gt;
        ///	&lt;th&gt;List Items&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN list_list --&gt;
        ///&lt;!-- IF list_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{list_list.TITLE}&lt;/td&gt;
        ///	&lt;td&gt;{list_list.TYPE}&lt;/td&gt;
        ///	&lt;td&gt;{list_list.ITEMS}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{list_list.U_VIEW}&quot;&gt;Vi [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_lists {
            get {
                return ResourceManager.GetString("account_lists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Pages&lt;/h3&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Page Title&lt;/th&gt;
        ///	&lt;th&gt;Last Updated&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN page_list --&gt;
        ///&lt;!-- IF page_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{page_list.TITLE}&lt;/td&gt;
        ///	&lt;td&gt;{page_list.UPDATED}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{page_list.U_VIEW}&quot;&gt;View&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{page_list.U_EDIT}&quot;&gt;Edit&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{page_list.U_DELETE}&quot;&gt;Delete&lt;/a&gt;&lt;/td&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- END page_lis [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_pages_manage {
            get {
                return ResourceManager.GetString("account_pages_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Write Page&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;/account/&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Write Page&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; onchange=&quot;UpdateSlug();&quot; value=&quot;{S_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;slug&quot;&gt;Page Slug&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;label&gt;Parent: {S_PAGE_PARENT}&lt;/label&gt; &lt;label&gt;Slug: &lt;input type=&quot;text&quot; id=&quot;slug&quot; name=&quot;slug&quot; value=&quot;{S_SLUG}&quot; /&gt;&lt;/label&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;post&quot;&gt;Page Text&lt;/label&gt;&lt;/ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_write {
            get {
                return ResourceManager.GetString("account_write", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;div id=&quot;pane-profile&quot;&gt;
        ///	&lt;div id=&quot;pane-pages&quot; class=&quot;pane&quot;&gt;
        ///		&lt;h3&gt;Pages&lt;/h3&gt;
        ///		&lt;ul&gt;
        ///			{PAGE_LIST}
        ///		&lt;/ul&gt;
        ///	&lt;/div&gt;
        ///&lt;/div&gt;
        ///&lt;div id=&quot;profile&quot;&gt;
        ///&lt;div id=&quot;pane-page&quot; class=&quot;pane&quot;&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;div id=&quot;page-body&quot; class=&quot;normal-writing&quot;&gt;
        ///		&lt;h2&gt;{PAGE_TITLE}&lt;/h2&gt;
        ///		&lt;p&gt;{PAGE_BODY}&lt;/p&gt;
        ///	&lt;/div&gt;
        ///	&lt;hr /&gt;
        ///	&lt;p&gt;&lt;em&gt;Last Modified: {PAGE_LAST_MODIFIED}&lt;/em&gt; with &lt;em&gt;{PAGE_VIEWS} page views.&lt;/em&gt;&lt;/p&gt;
        ///	&lt;!-- IF PAGE_LICENSE --&gt;
        ///	&lt;p&gt;
        ///	&lt;!-- IF I_PAGE_LICENSE --&gt;
        ///		&lt;a [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewpage {
            get {
                return ResourceManager.GetString("viewpage", resourceCulture);
            }
        }
    }
}
