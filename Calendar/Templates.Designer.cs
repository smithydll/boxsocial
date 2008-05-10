﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1433
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BoxSocial.Applications.Calendar {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BoxSocial.Applications.Calendar.Templates", typeof(Templates).Assembly);
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
        ///   Looks up a localized string similar to &lt;h3&gt;New Event&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_FORM_ACTION}&quot; method=&quot;post&quot; enctype=&quot;multipart/form-data&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;New Event&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;subject&quot;&gt;Subject&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;subject&quot; name=&quot;subject&quot; value=&quot;{S_SUBJECT}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;location&quot;&gt;Location&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;location&quot; name=&quot;location&quot; value=&quot;{S_LOCATION}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///      &lt;dt&gt;&lt;label for=&quot;location&quot;&gt;Invitees&lt;/label&gt;&lt; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_calendar_event_new {
            get {
                return ResourceManager.GetString("account_calendar_event_new", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;Manage Calendar&lt;/h3&gt;.
        /// </summary>
        internal static string account_calendar_manage {
            get {
                return ResourceManager.GetString("account_calendar_manage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;h3&gt;New Task&lt;/h3&gt;
        ///
        ///&lt;form action=&quot;{S_FORM_ACTION}&quot; method=&quot;post&quot; enctype=&quot;multipart/form-data&quot;&gt;
        ///	&lt;fieldset&gt;
        ///		&lt;legend&gt;New Task&lt;/legend&gt;
        ///		&lt;dl&gt;
        ///			&lt;dt&gt;&lt;label for=&quot;topic&quot;&gt;Topic&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;&lt;input type=&quot;text&quot; id=&quot;topic&quot; name=&quot;topic&quot; value=&quot;{S_TOPIC}&quot; style=&quot;width: 100%;&quot; /&gt;&lt;/dd&gt;
        ///			&lt;dt&gt;&lt;label&gt;Due Date&lt;/label&gt;&lt;/dt&gt;
        ///			&lt;dd&gt;
        ///				&lt;label&gt;Year: {S_DUE_YEAR}&lt;/label&gt;
        ///				&lt;label&gt;Month: {S_DUE_MONTH}&lt;/label&gt;
        ///				&lt;label&gt;Day: {S_DUE_DAY}&lt;/label&gt;
        ///				&lt;label&gt;Hour: {S_DUE_HOUR}&lt;/label&gt;
        ///				&lt;label&gt;Minute: [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string account_calendar_task_new {
            get {
                return ResourceManager.GetString("account_calendar_task_new", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 	&lt;div id=&quot;today-month&quot; style=&quot;border: solid 1px #EEEEEE; margin-bottom: 5px;&quot;&gt;
        ///		{CURRENT_MONTH} {CURRENT_YEAR}
        ///		&lt;div class=&quot;month-small&quot;&gt;
        ///			&lt;div class=&quot;week&quot;&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Mo&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Tu&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Wd&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Th&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Fr&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Sa&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Su&lt;/div&gt;
        ///			&lt;/div&gt;
        ///			&lt;!-- BEGIN week --&gt;
        ///			&lt;div class=&quot;week&quot;&gt;
        ///				&lt;!-- BEGIN week.day --&gt;
        ///				&lt;div class=&quot;day&quot;&gt;&lt;a href=&quot;{week.day.UR [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string todaymonthpanel {
            get {
                return ResourceManager.GetString("todaymonthpanel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 	&lt;div id=&quot;today-tasks&quot; style=&quot;border: solid 1px #EEEEEE; margin-bottom: 5px;&quot;&gt;
        ///		&lt;h4&gt;&lt;a href=&quot;{U_TASKS}&quot;&gt;Tasks&lt;/a&gt;&lt;/h4&gt;
        ///		&lt;!-- IF U_NEW_TASK --&gt;
        ///		&lt;div id=&quot;new-stuff&quot;&gt;
        ///			&lt;span id=&quot;new-task&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_TASK}&quot;&gt;New Task&lt;/a&gt;&lt;/span&gt;
        ///		&lt;/div&gt;
        ///		&lt;!-- ENDIF --&gt;
        ///		&lt;!-- IF HAS_TASKS --&gt;
        ///		&lt;ul id=&quot;today-tasks-list&quot;&gt;
        ///			&lt;!-- BEGIN task_days --&gt;
        ///			&lt;li&gt;&lt;strong&gt;{task_days.DAY}&lt;/strong&gt;
        ///				&lt;!-- BEGIN task_days.task_list --&gt;
        ///				&lt;dl&gt;
        ///					&lt;dt&gt;{task_days.task_list.DATE}&lt;/dt&gt;
        ///					&lt;dd [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string todaytaskspanel {
            get {
                return ResourceManager.GetString("todaytaskspanel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 	&lt;div id=&quot;today-calendar&quot; style=&quot;margin-right: 260px; border: solid 1px #EEEEEE; margin-bottom: 5px;&quot;&gt;
        ///		&lt;h4&gt;&lt;a href=&quot;{U_CALENDAR}&quot;&gt;Calendar&lt;/a&gt;&lt;/h4&gt;
        ///		&lt;!-- IF HAS_EVENTS --&gt;
        ///		&lt;ul id=&quot;today-events&quot;&gt;
        ///			&lt;!-- BEGIN appointment_days_list --&gt;
        ///			&lt;li&gt;
        ///				&lt;h5&gt;{appointment_days_list.DAY}&lt;/h5&gt;
        ///				&lt;dl&gt;
        ///					&lt;!-- BEGIN appointment_days_list.appointments_list --&gt;
        ///					&lt;dt&gt;{appointment_days_list.appointments_list.TIME}&lt;/dt&gt;
        ///					&lt;dd&gt;&lt;a href=&quot;{appointment_days_list.appointments_list.URI}&quot;&gt;{appointment_days_ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string todayupcommingevents {
            get {
                return ResourceManager.GetString("todayupcommingevents", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{CURRENT_DAY} {CURRENT_MONTH} {CURRENT_YEAR}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_NEW_EVENT --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-event&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_EVENT}&quot;&gt;New Event&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;ul id=&quot;day-timeslots&quot;&gt;
        ///		&lt;!-- BEGIN timeslot --&gt;
        ///		&lt;li&gt;
        ///			&lt;dl&gt;
        ///				&lt;dt&gt;{timeslot.TIME}&lt;/dt&gt;
        ///				&lt;dd&gt;
        ///					&lt;!-- IF timeslot.EVENTS --&gt;
        ///					&lt;ul class=&quot;day-events&quot;&gt;
        ///						&lt;!-- BEGIN timeslot.event --&gt;
        ///						&lt;li class=&quot;event&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewcalendarday {
            get {
                return ResourceManager.GetString("viewcalendarday", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{SUBJECT}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_NEW_EVENT --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-event&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_EVENT}&quot;&gt;New Event&lt;/a&gt;&lt;/span&gt;
        ///		&lt;span id=&quot;edit-event&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_EDIT_EVENT}&quot;&gt;Edit Event&lt;/a&gt;&lt;/span&gt;
        ///    &lt;span id=&quot;delete-event&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_DELETE_EVENT}&quot;&gt;Delete Event&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;!-- IF LOCATION --&gt;
        ///	&lt;p&gt;Location: {LOCATION}&lt;/p&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;p&gt;Sta [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewcalendarevent {
            get {
                return ResourceManager.GetString("viewcalendarevent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{CURRENT_MONTH} {CURRENT_YEAR}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_NEW_EVENT --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-event&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_EVENT}&quot;&gt;New Event&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///		&lt;div class=&quot;month&quot;&gt;
        ///			&lt;div class=&quot;week-head&quot;&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Monday&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Tuesday&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Wednesday&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Thursday&lt;/div&gt;
        ///				&lt;div class=&quot;day&quot;&gt;Friday&lt;/div&gt;
        ///				&lt;div class=&quot; [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewcalendarmonth {
            get {
                return ResourceManager.GetString("viewcalendarmonth", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;{TOPIC}&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_NEW_TASK --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-task&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_TASK}&quot;&gt;New Task&lt;/a&gt;&lt;/span&gt;
        ///		&lt;span id=&quot;edit-task&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_EDIT_TASK}&quot;&gt;Edit Task&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;!-- IF DUE_DATE --&gt;
        ///	&lt;p&gt;Due Date: {DUE_DATE}&lt;/p&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;!-- IF DESCRIPTION --&gt;
        ///	&lt;p&gt;{DESCRIPTION}&lt;/p&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///&lt;!-- INCLUDE page_footer.html --&gt;.
        /// </summary>
        internal static string viewcalendartask {
            get {
                return ResourceManager.GetString("viewcalendartask", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;!-- INCLUDE page_header.html --&gt;
        ///	&lt;h2&gt;Tasks&lt;/h2&gt;
        ///	&lt;p&gt;{BREADCRUMBS}&lt;/p&gt;
        ///	&lt;!-- IF U_NEW_TASK --&gt;
        ///	&lt;div id=&quot;new-stuff&quot;&gt;
        ///		&lt;span id=&quot;new-task&quot; class=&quot;post-button&quot;&gt;&lt;a href=&quot;{U_NEW_TASK}&quot;&gt;New Task&lt;/a&gt;&lt;/span&gt;
        ///	&lt;/div&gt;
        ///	&lt;!-- ENDIF --&gt;
        ///	&lt;!-- IF HAS_TASKS --&gt;
        ///	&lt;ul id=&quot;today-tasks-list&quot;&gt;
        ///		&lt;!-- BEGIN task_days --&gt;
        ///		&lt;li&gt;&lt;strong&gt;{task_days.DAY}&lt;/strong&gt;
        ///			&lt;!-- BEGIN task_days.task_list --&gt;
        ///			&lt;dl&gt;
        ///				&lt;dt&gt;{task_days.task_list.DATE}&lt;/dt&gt;
        ///				&lt;dd&gt;
        ///					&lt;span class=&quot;{task_days.task_list.CLASS}&quot;&gt;
        ///					{ta [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string viewcalendartasks {
            get {
                return ResourceManager.GetString("viewcalendartasks", resourceCulture);
            }
        }
    }
}
