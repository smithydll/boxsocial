﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1434
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.News {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.News.Templates", typeof(Templates).Assembly);
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
        internal static string account_news_article_write {
            get {
                return ResourceManager.GetString("account_news_article_write", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string account_news_manage {
            get {
                return ResourceManager.GetString("account_news_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;News&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///&lt;p&gt;{PAGINATION}&lt;/p&gt;
        ///&lt;!-- IF NEWS_COUNT --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN news_list --&gt;
        ///&lt;li&gt;
        ///	&lt;!-- IF news_list.TITLE --&gt;
        ///	&lt;h3&gt;&lt;a href=&quot;{news_list.U_ARTICLE}&quot;&gt;{news_list.TITLE}&lt;/a&gt;&lt;/h3&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///		&lt;p class=&quot;details&quot;&gt;&lt;strong&gt;{news_list.DATE}&lt;/strong&gt; &lt;em&gt;Posted by: &lt;a href=&quot;{news_list.U_POSTER}&quot;&gt;{news_list.USERNAME}&lt;/a&gt;&lt;/em&gt;&lt;/p&gt;
        ///	&lt;p&gt;{news_list.BODY}&lt;/p&gt;
        ///	&lt;p&gt;&lt;a href=&quot;{news_list.U_ARTICLE}#comments&quot;&gt;Comments ({news_list.COMMENTS})&lt;/a&gt;&lt;/p&gt;
        ///&lt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewnews {
            get {
                return ResourceManager.GetString("viewnews", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string viewnewsarticle {
            get {
                return ResourceManager.GetString("viewnewsarticle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 	&lt;div id=&quot;profile-gallery-pane&quot; class=&quot;pane&quot;&gt;
        ///	&lt;h3&gt;News&lt;/h3&gt;
        ///	&lt;!-- IF news_list --&gt;
        ///	&lt;ul&gt;
        ///	&lt;!-- BEGIN news_list --&gt;
        ///	  &lt;li&gt;{news_list.TITLE}&lt;/li&gt;
        ///	&lt;!-- END news_list --&gt;
        ///	&lt;/ul&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;/div&gt;.
        /// </summary>
        internal static string viewprofilenews {
            get {
                return ResourceManager.GetString("viewprofilenews", resourceCulture);
            }
        }
    }
}