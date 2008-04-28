﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Gallery {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Gallery.Templates", typeof(Templates).Assembly);
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
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Galleries&lt;/h3&gt;
        ///
        ///&lt;div id=&quot;new-stuff&quot;&gt;
        ///	&lt;!-- IF U_UPLOAD_PHOTO --&gt;
        ///	&lt;span id=&quot;new-photo&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_UPLOAD_PHOTO}&quot;&gt;Upload Photo&lt;/a&gt;&lt;/span&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;span id=&quot;new-gallery&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_GALLERY}&quot;&gt;New Gallery&lt;/a&gt;&lt;/span&gt;
        ///&lt;/div&gt;
        ///
        ///&lt;table style=&quot;width: 100%&quot;&gt;
        ///&lt;tr&gt;
        ///	&lt;th&gt;Name&lt;/th&gt;
        ///	&lt;th&gt;Items&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///	&lt;th&gt;&lt;/th&gt;
        ///&lt;/tr&gt;
        ///&lt;!-- BEGIN gallery_list --&gt;
        ///&lt;!-- IF gallery_list.INDEX_EVEN --&gt;
        ///&lt;tr class=&quot;even&quot;&gt;
        ///&lt;!-- ELSE [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_galleries {
            get {
                return ResourceManager.GetString("account_galleries", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- IF EDIT --&gt;
        ///&lt;h3&gt;Edit Gallery&lt;/h3&gt;
        ///&lt;!-- ELSE --&gt;
        ///&lt;h3&gt;Add New Gallery&lt;/h3&gt;
        ///&lt;!-- ENDIF --&gt;
        ///
        ///&lt;form action=&quot;/account/&quot; method=&quot;post&quot; enctype=&quot;multipart/form-data&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;!-- IF EDIT --&gt;
        ///		&lt;legend&gt;Edit Gallery&lt;/legend&gt;
        ///		&lt;!-- ELSE --&gt;
        ///		&lt;legend&gt;Add New Gallery&lt;/legend&gt;
        ///		&lt;!-- ENDIF --&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; value=&quot;{S_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;description&quot;&gt;Description&lt;/label&gt;&lt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_galleries_add {
            get {
                return ResourceManager.GetString("account_galleries_add", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Upload Photo&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_FORM_ACTION}&quot; method=&quot;post&quot; enctype=&quot;multipart/form-data&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Upload Photo&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; value=&quot;{S_PHOTO_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;!-- TODO: show picture thumbnail here --&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;description&quot;&gt;Description&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;textarea id=&quot;description&quot; name=&quot;description&quot; style=&quot;margin: 0px; width: 100%; height: 5 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_galleries_photo_edit {
            get {
                return ResourceManager.GetString("account_galleries_photo_edit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Upload Photo&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_FORM_ACTION}&quot; method=&quot;post&quot; enctype=&quot;multipart/form-data&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Upload Photo&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; value=&quot;{S_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;photo-file&quot;&gt;Select File&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;file&quot; id=&quot;photo-file&quot; name=&quot;photo-file&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;description&quot;&gt;Description&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;textarea id=&quot;d [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_galleries_upload {
            get {
                return ResourceManager.GetString("account_galleries_upload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///
        ///&lt;h2&gt;Upload Photo&lt;/h2&gt;
        ///
        ///&lt;form action=&quot;{S_FORM_ACTION}&quot; method=&quot;post&quot; enctype=&quot;multipart/form-data&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;Upload Photo&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;title&quot;&gt;Title&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;title&quot; name=&quot;title&quot; value=&quot;{S_TITLE}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;photo-file&quot;&gt;Select File&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;file&quot; id=&quot;photo-file&quot; name=&quot;photo-file&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;description&quot;&gt;Description&lt;/labe [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string groupgalleryupload {
            get {
                return ResourceManager.GetString("groupgalleryupload", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{USER_DISPLAY_NAME_OWNERSHIP} Gallery&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;!-- IF U_UPLOAD_PHOTO --&gt;
        ///		&lt;span id=&quot;new-photo&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_UPLOAD_PHOTO}&quot;&gt;Upload Photo&lt;/a&gt;&lt;/span&gt;
        ///		&lt;!-- ENDIF --&gt;
        ///		&lt;span id=&quot;new-gallery&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_GALLERY}&quot;&gt;New Gallery&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- IF GALLERIES --&gt;
        ///	&lt;ul id=&quot;gallery-list&quot;&gt;
        ///		&lt;!-- BEGIN gallery_list --&gt;
        ///		&lt;li&gt;
        ///			&lt;dl&gt;
        ///				&lt;!-- IF gallery_list.THUMBNAIL --&gt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewgallery {
            get {
                return ResourceManager.GetString("viewgallery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{USER_DISPLAY_NAME_OWNERSHIP} Gallery&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_UPLOAD_PHOTO --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-photo&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_UPLOAD_PHOTO}&quot;&gt;Upload Photo&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;!-- IF PAGINATION --&gt;
        ///	&lt;p&gt;&lt;strong&gt;Go to page:&lt;/strong&gt; {PAGINATION}&lt;/p&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;!-- IF PHOTOS --&gt;
        ///	&lt;ul id=&quot;photo-list&quot;&gt;
        ///		&lt;!-- BEGIN photo_list --&gt;
        ///		&lt;li&gt;
        ///			&lt;dl&gt;
        ///				&lt;dt&gt;&lt;a href=&quot;{photo_list.PHOTO_URI}&quot;&gt;&lt;img src [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewgroupgallery {
            get {
                return ResourceManager.GetString("viewgroupgallery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{PHOTO_TITLE}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_UPLOAD_PHOTO --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-photo&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_UPLOAD_PHOTO}&quot;&gt;Upload Photo&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;div id=&quot;view-photo&quot;&gt;
        ///	&lt;script type=&quot;text/javascript&quot;&gt;
        ///	&lt;!--
        ///		var user_tags = new Array();
        ///	&lt;!-- BEGIN user_tags --&gt;
        ///		user_tags[{user_tags.INDEX}] = new Array({user_tags.TAG_ID}, {user_tags.TAG_X}, {user_tags.TAG_Y});
        ///	&lt;!-- END user_tags --&gt;
        ///	--&gt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewphoto {
            get {
                return ResourceManager.GetString("viewphoto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 	&lt;div id=&quot;profile-gallery-pane&quot; class=&quot;pane&quot;&gt;
        ///	&lt;h3&gt;Group Gallery ({PHOTOS})&lt;/h3&gt;
        ///	&lt;!-- IF PHOTOS --&gt;
        ///	&lt;ul id=&quot;photo-list&quot;&gt;
        ///		&lt;!-- BEGIN photo_list --&gt;
        ///		&lt;li&gt;
        ///			&lt;dl&gt;
        ///				&lt;dt&gt;&lt;a href=&quot;{photo_list.PHOTO_URI}&quot;&gt;&lt;img src=&quot;{photo_list.THUMBNAIL}&quot; alt=&quot;{photo_list.TITLE}&quot; /&gt;&lt;/a&gt;&lt;/dt&gt;
        ///			&lt;/dl&gt;
        ///		&lt;/li&gt;
        ///		&lt;!-- END photo_list --&gt;
        ///	&lt;/ul&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;div class=&quot;cheat-clear-left&quot;&gt;&lt;/div&gt;
        ///	&lt;p&gt;&lt;a href=&quot;{U_GROUP_GALLERY}&quot;&gt;View All Photos&lt;/a&gt;&lt;/p&gt;
        ///	&lt;/div&gt;.
        /// </summary>
        internal static string viewprofilegallery {
            get {
                return ResourceManager.GetString("viewprofilegallery", resourceCulture);
            }
        }
    }
}
