﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Profile {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Profile.Templates", typeof(Templates).Assembly);
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
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Family&lt;/h3&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Name&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN family_list --&gt;
        ///&lt;!-- IF family_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{family_list.NAME}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{family_list.U_DELETE}&quot;&gt;Remove&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{family_list.U_BLOCK}&quot;&gt;Block&lt;/a&gt;&lt;/td&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- END family_list --&gt;
        ///&lt;/table&gt;.
        /// </summary>
        internal static string account_family_manage {
            get {
                return ResourceManager.GetString("account_family_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Invite a Friend to {SITE_TITLE}&lt;/h3&gt;
        ///&lt;p&gt;Invite a friend to join {SITE_TITLE}, just enter their e-mail address below:&lt;/p&gt;
        ///&lt;form action=&quot;{S_INVITE_FRIEND}&quot; method=&quot;post&quot;&gt;
        ///&lt;fieldset&gt;
        ///		&lt;legend&gt;Invite a Friend to {SITE_TITLE}&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;name&quot;&gt;Name:&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;name&quot; name=&quot;name&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;email&quot;&gt;E-mail:&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;email&quot; name=&quot;email&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;submit&quot; name=&quot;send&quot; v [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_friend_invite {
            get {
                return ResourceManager.GetString("account_friend_invite", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Friends&lt;/h3&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Name&lt;/th&gt;
        ///	&lt;th&gt;Order&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN friend_list --&gt;
        ///&lt;!-- IF friend_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{friend_list.U_PROFILE}&quot;&gt;{friend_list.NAME}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;{friend_list.ORDER}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{friend_list.U_PROMOTE}&quot;&gt;U&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{friend_list.U_DEMOTE}&quot;&gt;D&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{friend_list.U_D [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_friends_manage {
            get {
                return ResourceManager.GetString("account_friends_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Style&lt;/h3&gt;
        ///&lt;p&gt;Create a custom style. To see your style change to your profile, you must first turn on &apos;Show Custom Styles&apos; in your account preferences.&lt;/p&gt;
        ///&lt;form action=&quot;/account/&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Style&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;css-style&quot;&gt;Advanced CSS&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;textarea id=&quot;css-style&quot; name=&quot;css-style&quot; style=&quot;margin: 0px; width: 100%; height: 250px; border: solid 1px #666666;&quot; cols=&quot;70&quot; rows=&quot;15&quot;&gt;{STYLE}&lt;/textarea&gt;
        ///			&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;in [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_style {
            get {
                return ResourceManager.GetString("account_style", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///&lt;h2&gt;Status Feed&lt;/h2&gt;
        ///&lt;!-- IF BREADCRUMBS --&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///&lt;ul&gt;
        ///  &lt;!-- BEGIN status_messages --&gt;
        ///  &lt;li&gt;
        ///    &lt;a href=&quot;/{USER_NAME}&quot;&gt;{USER_DISPLAY_NAME}&lt;/a&gt;
        ///    {status_messages.STATUS_MESSAGE} &lt;em&gt;{status_messages.STATUS_UPDATED}&lt;/em&gt;
        ///  &lt;/li&gt;
        ///  &lt;!-- END status_messages --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewstatusfeed {
            get {
                return ResourceManager.GetString("viewstatusfeed", resourceCulture);
            }
        }
    }
}
