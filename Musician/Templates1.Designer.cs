﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4200
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
        internal static string account_gig_edit {
            get {
                return ResourceManager.GetString("account_gig_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Gigs&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_ADD_GIG}&quot;&gt;Add Gig&lt;/a&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;City&lt;/th&gt;
        ///	&lt;th&gt;Date&lt;/th&gt;
        ///	&lt;th&gt;Venue&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN gig_list --&gt;
        ///&lt;!-- IF gig_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{gig_list.CITY}&lt;/td&gt;
        ///	&lt;td&gt;{gig_list.DATE}&lt;/td&gt;
        ///	&lt;td&gt;{gig_list.VENUE}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{gig_list.U_EDIT}&quot;&gt;{L_EDIT}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{gig_list.U_DELETE}&quot;&gt;{L_DELETE}&lt;/a&gt;
        ///  &lt;/td&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- END  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_gigs_manage {
            get {
                return ResourceManager.GetString("account_gigs_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Member Profile&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Musician Profile&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;stage-name&quot;&gt;Stage Name&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_STAGENAME}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;biography&quot;&gt;Biography&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_BIOGRAPHY}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;submit&quot; name=&quot;save&quot; value=&quot;Save&quot; /&gt;&lt;/dd&gt;
        ///		&lt;/dl&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;module&quot; value=&quot;music&quot; /&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;sub&quot; value=&quot;my-profile&quot; /&gt;
        ///	&lt;/fieldset&gt;
        ///&lt;/form&gt;.
        /// </summary>
        internal static string account_member_profile {
            get {
                return ResourceManager.GetString("account_member_profile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Musician Profile&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Musician Profile&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;name&quot;&gt;Name&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_NAME}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;biography&quot;&gt;Biography&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_BIOGRAPHY}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;homepage&quot;&gt;Homepage&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_HOMEPAGE}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;submit&quot; name=&quot;save&quot; value=&quot;Save&quot; /&gt;&lt;/dd&gt;
        ///		&lt;/dl&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;module&quot; value=&quot;music&quot; /&gt;
        ///		&lt;input type=&quot;hi [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_profile {
            get {
                return ResourceManager.GetString("account_profile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Song&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{U_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Manage Song&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; value=&quot;{S_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;post&quot;&gt;Lyrics&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;textarea id=&quot;lyrics&quot; name=&quot;lyrics&quot; style=&quot;margin: 0px; width: 100%; height: 250px; border: solid 1px #666666;&quot; cols=&quot;70&quot; rows=&quot;15&quot;&gt;{S_LYRICS}&lt;/textarea&gt;
        ///				&lt;div style=&quot;background-image:  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_song_edit {
            get {
                return ResourceManager.GetString("account_song_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Songs&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_ADD_SONG}&quot;&gt;Add Song&lt;/a&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Title&lt;/th&gt;
        ///	&lt;th&gt;Recordings&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN song_list --&gt;
        ///&lt;!-- IF song_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{song_list.TITLE}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{song_list.U_RECORDINGS}&quot;&gt;{song_list.RECORDINGS}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{song_list.U_ADD_RECORDING}&quot;&gt;Add Recording&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{song_list.U_EDIT}&quot;&gt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_songs {
            get {
                return ResourceManager.GetString("account_songs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Edit Tour&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Edit Tour&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Tour Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_TITLE}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;year&quot;&gt;Tour Year&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_YEAR}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;submit&quot; name=&quot;save&quot; value=&quot;Save&quot; /&gt;&lt;/dd&gt;
        ///		&lt;/dl&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;module&quot; value=&quot;music&quot; /&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;sub&quot; value=&quot;tour&quot; /&gt;
        ///		&lt;!-- IF EDIT --&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;id&quot; value=&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_tour_edit {
            get {
                return ResourceManager.GetString("account_tour_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Tours&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_ADD_TOUR}&quot;&gt;Add Tour&lt;/a&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Title&lt;/th&gt;
        ///	&lt;th&gt;Year&lt;/th&gt;
        ///	&lt;th&gt;Gigs&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN tour_list --&gt;
        ///&lt;!-- IF tour_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{song_list.TITLE}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{tour_list.U_GIGS}&quot;&gt;{tour_list.GIGS}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{tour_list.U_ADD_GIG}&quot;&gt;Add Gig&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{tour_list.U_EDIT}&quot;&gt;{L_EDIT}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_tour_manage {
            get {
                return ResourceManager.GetString("account_tour_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Music&lt;/h2&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string music_default {
            get {
                return ResourceManager.GetString("music_default", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;ul&gt;
        ///	&lt;li&gt;&lt;a href=&quot;{U_MUSIC_ARTISTS}&quot;&gt;{L_ALL_ARTISTS}&lt;/a&gt;&lt;/li&gt;
        ///	&lt;li&gt;&lt;a href=&quot;{U_MUSIC_GENRES}&quot;&gt;{L_GENRES}&lt;/a&gt;&lt;/li&gt;
        ///&lt;/ul&gt;
        ///
        ///&lt;div id=&quot;artist-filter-panel&quot;&gt;
        ///  &lt;p&gt;
        ///    &lt;a href=&quot;{U_FILTER_ALL}&quot;&gt;All&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_A}&quot;&gt;A&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_B}&quot;&gt;B&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_C}&quot;&gt;C&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_D}&quot;&gt;D&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_E}&quot;&gt;E&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_F}&quot;&gt;F&lt;/a&gt;
        ///  [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string music_directory {
            get {
                return ResourceManager.GetString("music_directory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Fans&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///&lt;div id=&quot;member-filter-panel&quot;&gt;
        ///  &lt;p&gt;
        ///    &lt;a href=&quot;{U_FILTER_ALL}&quot;&gt;All&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_A}&quot;&gt;A&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_B}&quot;&gt;B&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_C}&quot;&gt;C&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_D}&quot;&gt;D&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_E}&quot;&gt;E&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_F}&quot;&gt;F&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_G}&quot;&gt;G&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_H}&quot;&gt;H&lt;/a&gt;
        ///    &lt;a href=&quot;{U_FILTER_BEGINS_I}&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewfans {
            get {
                return ResourceManager.GetString("viewfans", resourceCulture);
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
        ///&lt;!-- IF BIOGRAPHY --&gt;
        ///&lt;p&gt;{BIOGRAPHY}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF instrument_list --&gt;
        ///&lt;h3&gt;{L_INSTRUMENTS}&lt;/h3&gt;
        ///&lt;ul&gt;
        ///  &lt;!-- BEGIN instrument_list --&gt;
        ///  &lt;li&gt;{instrument_list.NAME}&lt;/li&gt;
        ///  &lt;!-- END instrument_list --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- ENDIF --&gt;
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