﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3074
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Forum {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Forum.Templates", typeof(Templates).Assembly);
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
        ///   Looks up a localized string similar to &lt;!-- IF EDIT --&gt;
        ///&lt;h3&gt;{L_EDIT_FORUM}&lt;/h3&gt;
        ///&lt;p&gt;Manage forums.&lt;/p&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;h3&gt;{L_CREATE_NEW_FORUM}&lt;/h3&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF ERROR --&gt;
        ///&lt;p class=&quot;error&quot;&gt;{ERROR}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///  &lt;fieldset&gt;
        ///    &lt;!-- IF EDIT --&gt;
        ///    &lt;legend&gt;{L_EDIT_FORUM}&lt;/legend&gt;
        ///    &lt;!-- ELSE --&gt;
        ///    &lt;legend&gt;{L_CREATE_NEW_FORUM}&lt;/legend&gt;
        ///    &lt;!-- ENDIF --&gt;
        ///    &lt;dl&gt;
        ///      &lt;dt&gt;
        ///        &lt;label for=&quot;type&quot;&gt;{L_FORUM_TYPE}&lt;/label&gt;
        ///      &lt;/dt&gt;
        ///      &lt;dd&gt;
        ///        {S_FORUM_TYP [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_edit {
            get {
                return ResourceManager.GetString("account_forum_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;{L_MANAGE_FORUMS}&lt;/h3&gt;
        ///&lt;p&gt;{L_MANAGE_FORUMS_PROSE}&lt;/p&gt;
        ///
        ///&lt;div id=&quot;new-stuff&quot;&gt;
        ///	&lt;!-- IF U_CREATE_FORUM --&gt;
        ///	&lt;span id=&quot;new-forum&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_CREATE_FORUM}&quot;&gt;{L_CREATE_NEW_FORUM}&lt;/a&gt;&lt;/span&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///&lt;/div&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///  &lt;tr&gt;
        ///    &lt;th&gt;{L_FORUM_CATEGORY}&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///  &lt;/tr&gt;
        ///  &lt;!-- BEGIN forum_list --&gt;
        ///  &lt;!-- IF forum_list.INDEX_EVEN --&gt;
        ///  &lt;tr class=&quot;even&quot;&gt;
        ///    &lt;!-- ELSE --&gt;
        ///    &lt;tr cl [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_manage {
            get {
                return ResourceManager.GetString("account_forum_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;{L_FORUM_MEMBER}&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;{L_FORUM_MEMBER}&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;{L_USERNAME}&lt;/dt&gt;
        ///			&lt;dd&gt;{S_USERNAME}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;rank&quot;&gt;{L_RANK}&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_RANK}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;signature&quot;&gt;{L_SIGNATURE}&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;textarea id=&quot;signature&quot; name=&quot;signature&quot; style=&quot;margin: 0px; width: 100%; height: 250px; border: solid 1px #666666;&quot; cols=&quot;70&quot; rows=&quot;15&quot;&gt;{S_SIGNATURE}&lt;/textarea&gt;
        ///				&lt;div style=&quot;backgroun [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_member_edit {
            get {
                return ResourceManager.GetString("account_forum_member_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;{L_FORUM_MEMBERS}&lt;/h3&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///  &lt;tr&gt;
        ///    &lt;th&gt;{L_RANK}&lt;/th&gt;
        ///    &lt;th&gt;{L_SPECIAL}&lt;/th&gt;
        ///    &lt;th&gt;{L_MINIMUM_POSTS}&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///  &lt;/tr&gt;
        ///  &lt;!-- BEGIN members --&gt;
        ///  &lt;!-- IF rank_list.INDEX_EVEN --&gt;
        ///  &lt;tr class=&quot;even&quot;&gt;
        ///    &lt;!-- ELSE --&gt;
        ///    &lt;tr class=&quot;odd&quot;&gt;
        ///      &lt;!-- ENDIF --&gt;
        ///      &lt;td&gt;{rank_list.RANK}&lt;/td&gt;
        ///      &lt;td&gt;{rank_list.SPECIAL}&lt;/td&gt;
        ///      &lt;td&gt;{rank_list.MINIMUM_POSTS}&lt;/td&gt;
        ///      &lt;td&gt;
        ///        &lt;a href=&quot;{rank_list.U_EDIT}&quot;&gt;{L_EDIT}&lt;/a&gt;
        ///      [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_member_manage {
            get {
                return ResourceManager.GetString("account_forum_member_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;{L_FORUM_RANK}&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;{L_FORUM_RANK}&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;rank-title&quot;&gt;{L_RANK_TITLE}&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;rank-title&quot; name=&quot;rank-title&quot; value=&quot;{S_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;min-posts&quot;&gt;{L_MINIMUM_POSTS}&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;min-posts&quot; name=&quot;min-posts&quot; value=&quot;{S_MINIMUM_POSTS}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///      &lt;dt&gt;&lt;label for=&quot;special&quot;&gt;{L_SPECIAL_ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_rank_edit {
            get {
                return ResourceManager.GetString("account_forum_rank_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;{L_FORUM_RANKS}&lt;/h3&gt;
        ///
        ///&lt;div id=&quot;new-stuff&quot;&gt;
        ///	&lt;span id=&quot;new-rank&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_RANK}&quot;&gt;{L_NEW_RANK}&lt;/a&gt;&lt;/span&gt;
        ///&lt;/div&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///  &lt;tr&gt;
        ///    &lt;th&gt;{L_RANK}&lt;/th&gt;
        ///    &lt;th&gt;{L_SPECIAL}&lt;/th&gt;
        ///    &lt;th&gt;{L_MINIMUM_POSTS}&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///    &lt;th&gt;&lt;/th&gt;
        ///  &lt;/tr&gt;
        ///  &lt;!-- BEGIN rank_list --&gt;
        ///  &lt;!-- IF rank_list.INDEX_EVEN --&gt;
        ///  &lt;tr class=&quot;even&quot;&gt;
        ///    &lt;!-- ELSE --&gt;
        ///    &lt;tr class=&quot;odd&quot;&gt;
        ///      &lt;!-- ENDIF --&gt;
        ///      &lt;td&gt;{rank_list.RANK}&lt;/td&gt;
        ///      &lt;td&gt;{rank_list.SPEC [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_ranks {
            get {
                return ResourceManager.GetString("account_forum_ranks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;{L_FORUM_SETTINGS}&lt;/h3&gt;
        ///
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;{L_FORUM_SETTINGS}&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;topics-per-page&quot;&gt;{L_TOPICS_PER_PAGE}&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;topics-per-page&quot; name=&quot;topics-per-page&quot; value=&quot;{S_TOPICS_PER_PAGE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///      &lt;dt&gt;&lt;label for=&quot;posts-per-page&quot;&gt;{L_POSTS_PER_PAGE}&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;posts-per-page&quot; name=&quot;posts-per-page&quot; value=&quot;{S_POSTS_PER_PAGE}&quot; style=&quot;width [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_forum_settings {
            get {
                return ResourceManager.GetString("account_forum_settings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;div&gt;
        ///  &lt;a href=&quot;{U_FORUM_INDEX}&quot;&gt;{L_FORUM_INDEX}&lt;/a&gt;
        ///  &lt;a href=&quot;{U_FAQ}&quot;&gt;{L_FORUM_HELP}&lt;/a&gt;
        ///&lt;!-- IF LOGGED_IN --&gt;
        ///  &lt;!-- IF U_JOIN --&gt;
        ///  &lt;a href=&quot;{U_JOIN}&quot;&gt;{L_JOIN_GROUP}&lt;/a&gt;
        ///  &lt;!-- ELSE --&gt;
        ///  &lt;a href=&quot;{U_UCP}&quot;&gt;{L_FORUM_PROFILE}&lt;/a&gt;
        ///  &lt;a href=&quot;{U_MEMBERS}&quot;&gt;{L_GROUP_MEMBERS}&lt;/a&gt;
        ///  &lt;!-- ENDIF --&gt;
        ///&lt;!-- ELSE --&gt;
        ///  &lt;a href=&quot;{U_REGISTER}&quot;&gt;{L_REGISTER}&lt;/a&gt;
        ///&lt;!-- ENDIF --&gt;
        ///&lt;/div&gt;.
        /// </summary>
        internal static string forum_header {
            get {
                return ResourceManager.GetString("forum_header", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string memberlist {
            get {
                return ResourceManager.GetString("memberlist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;!-- INCLUDE forum_header --&gt;
        ///&lt;h2&gt;Post&lt;/h2&gt;
        ///
        ///&lt;!-- IF ERROR --&gt;
        ///&lt;p class=&quot;error&quot;&gt;{ERROR}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF PREVIEW --&gt;
        ///&lt;p&gt;{PREVIEW}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;form action=&quot;{S_POST}&quot; method=&quot;post&quot;&gt;
        ///  &lt;fieldset&gt;
        ///    &lt;legend&gt;{L_POST}&lt;/legend&gt;
        ///    &lt;dl&gt;
        ///      &lt;dt&gt;
        ///        &lt;label for=&quot;subject&quot;&gt;{L_POST_SUBJECT}&lt;/label&gt;
        ///      &lt;/dt&gt;
        ///      &lt;dd&gt;
        ///        &lt;input type=&quot;text&quot; id=&quot;subject&quot; name=&quot;subject&quot; value=&quot;{S_SUBJECT}&quot; style=&quot;width: 100%;&quot; /&gt;
        ///      &lt;/dd&gt;
        ///      [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string post {
            get {
                return ResourceManager.GetString("post", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;!-- INCLUDE forum_header --&gt;
        ///&lt;h2&gt;User Control Panel&lt;/h2&gt;
        ///
        ///&lt;!-- IF ERROR --&gt;
        ///&lt;p class=&quot;error&quot;&gt;{ERROR}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;form action=&quot;{S_POST}&quot; method=&quot;post&quot;&gt;
        ///  &lt;fieldset&gt;
        ///    &lt;legend&gt;User Control Panel&lt;/legend&gt;
        ///    &lt;dl&gt;
        ///      &lt;dt&gt;
        ///        &lt;label for=&quot;signature&quot;&gt;{L_SIGNATURE}&lt;/label&gt;
        ///      &lt;/dt&gt;
        ///      &lt;dd&gt;
        ///        &lt;textarea id=&quot;signature&quot; name=&quot;signature&quot; style=&quot;margin: 0px; width: 100%; height: 250px; border: solid 1px #666666;&quot; cols=&quot;70&quot; rows=&quot;15&quot;&gt;{S_SIG [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string ucp {
            get {
                return ResourceManager.GetString("ucp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;!-- INCLUDE forum_header --&gt;
        ///	&lt;h2&gt;{FORUM_TITLE}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	
        ///	&lt;!-- IF RULES --&gt;
        ///	&lt;div id=&quot;forum-rules&quot;&gt;
        ///		&lt;p&gt;&lt;strong&gt;Forum Rules&lt;/strong&gt;&lt;/p&gt;
        ///		&lt;p&gt;{RULES}&lt;/p&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	
        ///	&lt;!-- IF U_NEW_TOPIC --&gt;
        ///	&lt;div class=&quot;new-stuff&quot;&gt;
        ///		&lt;span class=&quot;new-topic post-button&quot;&gt;&lt;a href=&quot;{U_NEW_TOPIC}&quot;&gt;{L_NEW_TOPIC}&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///
        ///  &lt;!-- IF FORUMS --&gt;
        ///  &lt;!-- BEGIN forum_list --&gt;
        ///  &lt;!-- IF forum_list.IS_CATEGORY --&gt;
        ///  &lt;div  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewforum {
            get {
                return ResourceManager.GetString("viewforum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 	&lt;div id=&quot;profile-forum-pane&quot; class=&quot;pane&quot;&gt;
        ///		&lt;h3&gt;Forum&lt;/h3&gt;
        ///		&lt;a href=&quot;{U_FORUM}&quot;&gt;{L_GO_TO_FORUM}&lt;/a&gt;
        ///	&lt;/div&gt;.
        /// </summary>
        internal static string viewprofileforum {
            get {
                return ResourceManager.GetString("viewprofileforum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;!-- INCLUDE forum_header --&gt;
        ///	&lt;h2&gt;{TOPIC_TITLE}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_NEW_REPLY --&gt;
        ///	&lt;div class=&quot;new-stuff&quot;&gt;
        ///    &lt;span class=&quot;new-topic post-button&quot;&gt;&lt;a href=&quot;{U_NEW_TOPIC}&quot;&gt;{L_NEW_TOPIC}&lt;/a&gt;&lt;/span&gt;
        ///		&lt;span class=&quot;new-reply post-button&quot;&gt;&lt;a href=&quot;{U_NEW_REPLY}&quot;&gt;{L_POST_REPLY}&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///
        ///&lt;p&gt;{PAGINATION}&lt;/p&gt;
        ///&lt;!-- IF POSTS --&gt;
        ///&lt;ul id=&quot;posts&quot;&gt;
        ///  &lt;!-- BEGIN post_list --&gt;
        ///  &lt;li&gt;
        ///  	&lt;h3 id=&quot;p{post_list.ID}&quot;&gt;&lt;a href=&quot;{post_lis [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewtopic {
            get {
                return ResourceManager.GetString("viewtopic", resourceCulture);
            }
        }
    }
}
