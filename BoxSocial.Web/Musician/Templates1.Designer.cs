﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
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
        internal static string account_discography_album_edit {
            get {
                return ResourceManager.GetString("account_discography_album_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Edit Gig&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_ACCOUNT}&quot; method=&quot;post&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Edit Gig&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Gig Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_TITLE}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;tour&quot;&gt;Gig Tour&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_TOURS}&lt;/dd&gt;
        ///      &lt;dt&gt;Gig Date&lt;/dt&gt;
        ///			&lt;dd&gt;{S_DATE} {S_TIMEZONE}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;city&quot;&gt;Gig City&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_CITY}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;venue&quot;&gt;Gig Venue&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_VENUE}&lt;/dd&gt;
        ///      &lt;dt&gt;&lt;label for=&quot;all-ages&quot;&gt;{L_ALL_AGES}&lt;/label [rest of string was truncated]&quot;;.
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
        ///&lt;!-- IF gig_list.$_INDEX_EVEN --&gt;
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
        ///&lt;!-- EN [rest of string was truncated]&quot;;.
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
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Group Members&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_ADD_MEMBER}&quot;&gt;Add Member&lt;/a&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Title&lt;/th&gt;
        ///	&lt;th&gt;Joined&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///  &lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN member_list --&gt;
        ///&lt;!-- IF member_list.$_INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{member_list.DISPLAY_NAME}&lt;/td&gt;
        ///	&lt;td&gt;{member_list.DATE_JOINED}&lt;/td&gt;
        ///	&lt;td&gt;
        ///    &lt;!-- IF member_list.U_EDIT --&gt;
        ///    &lt;a href=&quot;{member_list.U_EDIT}&quot;&gt;{L_EDIT}&lt;/a&gt;
        ///    &lt;!-- ELSE --&gt;
        ///    {L_EDIT}
        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_members_manage {
            get {
                return ResourceManager.GetString("account_members_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Musician Memberships&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_REGISTER_MUSICIAN}&quot;&gt;Register Musician&lt;/a&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;{L_FANS}&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- IF musician_list --&gt;
        ///&lt;!-- BEGIN musician_list --&gt;
        ///&lt;!-- IF musician_list.$_INDEX_EVEN --&gt;
        ///  &lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{musician_list.U_MUSICIAN}&quot;&gt;{musician_list.DISPLAY_NAME}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;{musician_list.FANS}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{musician_list.U_MANAGE [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_my_musicians {
            get {
                return ResourceManager.GetString("account_my_musicians", resourceCulture);
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
        ///      &lt;dt&gt;&lt;label for=&quot;genre&quot;&gt;Genre&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_GENRE}&lt;/dd&gt;
        ///      &lt;dt&gt;&lt;label for=&quot;musician-type&quot;&gt;Act&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_MUSICIAN_TYPE}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;biography&quot;&gt;Biography&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_BIOGRAPHY}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;homepage&quot;&gt;Homepage&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_HOMEPAGE}&lt;/dd&gt;
        /// </summary>
        internal static string account_profile {
            get {
                return ResourceManager.GetString("account_profile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Recordings&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_ADD_RECORDING}&quot;&gt;Add Recording&lt;/a&gt;.
        /// </summary>
        internal static string account_recordings {
            get {
                return ResourceManager.GetString("account_recordings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Releases&lt;/h3&gt;
        ///
        ///&lt;a href=&quot;{U_ADD_DEMO}&quot;&gt;Add Demo&lt;/a&gt;
        ///&lt;a href=&quot;{U_ADD_EP}&quot;&gt;Add EP&lt;/a&gt;
        ///&lt;a href=&quot;{U_ADD_ALBUM}&quot;&gt;Add Album&lt;/a&gt;
        ///&lt;a href=&quot;{U_ADD_SINGLE}&quot;&gt;Add Single&lt;/a&gt;
        ///&lt;a href=&quot;{U_ADD_DVD}&quot;&gt;Add DVD&lt;/a&gt;
        ///&lt;a href=&quot;{U_ADD_COMPILATION}&quot;&gt;Add Compilation&lt;/a&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Title&lt;/th&gt;
        ///	&lt;th&gt;Tracks&lt;/th&gt;
        ///	&lt;th&gt;Views&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN album_list --&gt;
        ///&lt;!-- IF album_list.$_INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF - [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_releases {
            get {
                return ResourceManager.GetString("account_releases", resourceCulture);
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
        ///&lt;!-- IF song_list.$_INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{song_list.TITLE}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{song_list.U_RECORDINGS}&quot;&gt;{song_list.RECORDINGS}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{song_list.U_ADD_RECORDING}&quot;&gt;Add Recording&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{song_list.U_EDIT} [rest of string was truncated]&quot;;.
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
        ///      &lt;dt&gt;&lt;label for=&quot;abstract&quot;&gt;Tour Description&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;{S_ABSTRACT}&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;submit&quot; name=&quot;save&quot; value=&quot;Save&quot; /&gt;&lt;/dd&gt;
        ///		&lt;/dl&gt;
        ///		&lt;input type=&quot;hidden&quot; name=&quot;module&quot; value=&quot;music&quot; /&gt;
        ///		&lt;input type=&quot;hidden&quot; [rest of string was truncated]&quot;;.
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
        ///  &lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN tour_list --&gt;
        ///&lt;!-- IF tour_list.$_INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;tr class=&quot;odd&quot;&gt;
        ///&lt;!-- ENDIF --&gt;
        ///	&lt;td&gt;{tour_list.TITLE}&lt;/td&gt;
        ///  &lt;td&gt;{tour_list.YEAR}&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{tour_list.U_GIGS}&quot;&gt;{tour_list.GIGS}&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{tour_list.U_ADD_GIG}&quot;&gt;Add Gig&lt;/a&gt;&lt;/td&gt;
        ///	&lt;td&gt;&lt;a href=&quot;{t [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_tour_manage {
            get {
                return ResourceManager.GetString("account_tour_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Chart&lt;/h2&gt;
        ///
        ///&lt;!-- IF TOP_ARTISTS --&gt;
        ///&lt;h3&gt;Top 10 Artists This Week&lt;/h3&gt;
        ///
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN musician_list --&gt;
        ///	&lt;li&gt;&lt;/li&gt;
        ///&lt;!-- END musician_list --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string chart_default {
            get {
                return ResourceManager.GetString("chart_default", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Music&lt;/h2&gt;
        ///
        ///&lt;p&gt;&lt;a href=&quot;{U_REGISTER_MUSICIAN}&quot;&gt;Register Musician&lt;/a&gt;&lt;/p&gt;
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
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;ul&gt;
        ///	&lt;li&gt;&lt;a href=&quot;{U_MUSIC_ARTISTS}&quot;&gt;{L_ALL_ARTISTS}&lt;/a&gt;&lt;/li&gt;
        ///	&lt;li&gt;&lt;a href=&quot;{U_MUSIC_GENRES}&quot;&gt;{L_GENRES}&lt;/a&gt;&lt;/li&gt;
        ///&lt;/ul&gt;
        ///
        ///	&lt;!-- IF PAGINATION --&gt;
        ///	&lt;p&gt;&lt;strong&gt;Go to page:&lt;/strong&gt; {PAGINATION}&lt;/p&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN genre_list --&gt;
        ///  &lt;li&gt;
        ///    &lt;p&gt;&lt;a href=&quot;{genre_list.U_GENRE}&quot;&gt;{genre_list.DISPLAY_NAME}&lt;/a&gt;&lt;/p&gt;
        ///    &lt;ul&gt;
        ///&lt;!-- BEGIN subgenre_list --&gt;
        ///      &lt;li&gt;&lt;a href=&quot;{genre_list.subgenre_list.U_SUBGENRE}&quot;&gt;{genre_list.subge [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string music_directory_genres {
            get {
                return ResourceManager.GetString("music_directory_genres", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- IF U_MUSICIAN_ACCOUNT --&gt;
        ///&lt;a href=&quot;{U_MUSICIAN_ACCOUNT}&quot;&gt;Musician Administration Panel&lt;/a&gt;
        ///&lt;!-- ENDIF --&gt;.
        /// </summary>
        internal static string musician_footer {
            get {
                return ResourceManager.GetString("musician_footer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;{L_DISCOGRAPHY}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- IF album_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN album_list --&gt;
        ///&lt;li&gt;&lt;/li&gt;
        ///&lt;!-- END album_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF single_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN single_list --&gt;
        ///&lt;li&gt;&lt;/li&gt;
        ///&lt;!-- END single_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF ep_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN ep_list --&gt;
        ///&lt;li&gt;&lt;/li&gt;
        ///&lt;!-- END ep_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF demo_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN demo_list --&gt;
        ///&lt;li&gt;&lt;/li&gt;
        ///&lt;!-- END demo_ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewdiscography {
            get {
                return ResourceManager.GetString("viewdiscography", resourceCulture);
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
        ///  &lt;dt&gt;Tickets&lt;/dt&gt;
        ///  &lt;!-- IF U_TICKETS --&gt;
        ///  &lt;dd&gt;&lt;a href=&quot;{U_TICKETS}&quot;&gt;Purchase online [external link]&lt;/a&gt;&lt;/dd&gt;
        ///  &lt;!-- ENDIF --&gt;
        ///  &lt;!-- IF IS_TICKETS_AT_DOOR --&gt;
        ///  &lt;dd&gt;Tickets avaliable at the door&lt;/dd&gt;
        ///  &lt;!-- ENDIF --&gt;
        ///&lt;/dl&gt;
        ///
        ///&lt;p&gt;{ABSTRACT}&lt;/p&gt;
        ///
        ///&lt;div class=&quot;comment-pane&quot;&gt;
        ///  &lt;!-- INCLUDE pane.co [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewgig {
            get {
                return ResourceManager.GetString("viewgig", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;{L_GIGS}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- IF gig_list --&gt;
        ///&lt;ul&gt;
        ///  &lt;!-- BEGIN gig_list --&gt;
        ///  &lt;li&gt;&lt;a href=&quot;{gig_list.U_GIG}&quot;&gt;{gig_list.CITY}, {gig_list.DATE}&lt;/a&gt;&lt;/li&gt;
        ///  &lt;!-- END gig_list --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewgigs {
            get {
                return ResourceManager.GetString("viewgigs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;&lt;a href=&quot;{U_MUSICIAN}&quot;&gt;{MUSICIAN_DISPLAY_NAME}&lt;/a&gt;&lt;/h2&gt;
        ///
        ///&lt;div id=&quot;pane-profile&quot;&gt;
        ///  &lt;div class=&quot;pane&quot;&gt;
        ///  &lt;h3&gt;&lt;a href=&quot;{U_MUSICIAN}&quot;&gt;{MUSICIAN_DISPLAY_NAME}&lt;/a&gt;&lt;/h3&gt;
        ///  &lt;dl&gt;
        ///    &lt;dt&gt;{L_JOINED}:&lt;/dt&gt;
        ///    &lt;dd&gt;{DATE_JOINED}&lt;/dd&gt;
        ///  &lt;/dl&gt;
        ///  &lt;!-- IF U_BECOME_FAN --&gt;
        ///			&lt;p&gt;&lt;span id=&quot;fan-musiciain&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_BECOME_FAN}&quot;&gt;Become a Fan&lt;/a&gt;&lt;/span&gt;&lt;/p&gt;
        ///			&lt;!-- ENDIF --&gt;
        ///  &lt;!-- IF U_MUSICIAN_ACCOUNT --&gt;
        ///			&lt;p&gt;&lt;span id=&quot;account-musician&quot; class=&quot;post [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewmusician {
            get {
                return ResourceManager.GetString("viewmusician", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;&lt;a href=&quot;{U_MEMBER}&quot;&gt;{MEMBER_DISPLAY_NAME}&lt;/a&gt;&lt;/h2&gt;
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
        internal static string viewmusician_member {
            get {
                return ResourceManager.GetString("viewmusician_member", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;{L_MUSICIAN_MEMBERS}&lt;/h2&gt;
        ///
        ///&lt;!-- IF member_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN member_list --&gt;
        ///	&lt;li&gt;{member_list.STAGE_NAME}&lt;/li&gt;
        ///&lt;!-- END member_list --&gt;
        ///&lt;/ul&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewmusician_members {
            get {
                return ResourceManager.GetString("viewmusician_members", resourceCulture);
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
        ///&lt;h2&gt;{TOUR_TITLE} ({TOUR_YEAR})&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- IF TOUR_ABSTRACT --&gt;
        ///&lt;p&gt;{TOUR_ABSTRACT}&lt;/p&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;!-- IF gig_list --&gt;
        ///&lt;ul&gt;
        ///  &lt;!-- BEGIN gig_list --&gt;
        ///  &lt;li&gt;&lt;a href=&quot;{gig_list.U_GIG}&quot;&gt;{gig_list.CITY}, {gig_list.DATE}&lt;/a&gt;&lt;/li&gt;
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
        ///&lt;h2&gt;{L_TOURS}&lt;/h2&gt;
        ///&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///
        ///&lt;!-- IF tour_list --&gt;
        ///&lt;ul&gt;
        ///&lt;!-- BEGIN tour_list --&gt;
        ///  &lt;li&gt;&lt;a href=&quot;{tour_list.U_TOUR}&quot;&gt;{tour_list.TITLE}&lt;/a&gt; ({tour_list.YEAR})&lt;/li&gt;
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