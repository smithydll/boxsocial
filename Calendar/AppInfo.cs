/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.Applications.Calendar
{
    public class AppInfo : Application
    {
        public AppInfo(Core core)
            : base(core)
        {
        }

        public override string Title
        {
            get
            {
                return "Calendar";
            }
        }

        public override string Stub
        {
            get
            {
                return "calendar";
            }
        }

        public override string Description
        {
            get
            {
                return "";
            }
        }

        public override bool UsesComments
        {
            get
            {
                return true;
            }
        }

        public override bool UsesRatings
        {
            get
            {
                return false;
            }
        }

        public override System.Drawing.Image Icon
        {
            get
            {
                return Properties.Resources.calendar;
            }
        }

        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        public override string JavaScript
        {
            get
            {
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
			
            core.RegisterCommentHandle(ItemKey.GetTypeId(typeof(Event)), eventCanPostComment, eventCanDeleteComment, eventAdjustCommentCount, eventCommentPosted);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("calendar", @"^/calendar(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/([0-9]{4})(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/([0-9]{4})/([0-9]{1,2})(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/([0-9]{4})/([0-9]{1,2})/([0-9]{1,2})(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/event/([0-9]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/task/([0-9]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/tasks(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);

            aii.AddModule("calendar");

            aii.AddCommentType("EVENT");

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("calendar", "Calendar");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/calendar(|/)$", showCalendar, 1);
            core.RegisterApplicationPage(@"^/calendar/([0-9]{4})(|/)$", showCalendarYear, 2);
            core.RegisterApplicationPage(@"^/calendar/([0-9]{4})/([0-9]{1,2})(|/)$", showCalendarMonth, 3);
            core.RegisterApplicationPage(@"^/calendar/([0-9]{4})/([0-9]{1,2})/([0-9]{1,2})(|/)$", showCalendarDay, 4);
            //core.RegisterApplicationPage(@"^/calendar/event/([0-9]+)(|/)$", showEvent, 5);
            //core.RegisterApplicationPage(@"^/calendar/task/([0-9]+)(|/)$", showTask, 6);
            core.RegisterApplicationPage(@"^/calendar/tasks(|/)$", showTasks, 7);
        }

        /// <summary>
        /// Callback on a comment being posted to an event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data</param>
        private void eventCommentPosted(CommentPostedEventArgs e)
        {
            // Notify of a new comment
            Event calendarEvent = new Event(core, null, e.ItemId);
            User owner = (User)calendarEvent.Owner;

            ApplicationEntry ae = new ApplicationEntry(core, owner, "Calendar");

            Template notificationTemplate = new Template(Assembly.GetExecutingAssembly(), "user_event_notification");
            notificationTemplate.Parse("U_PROFILE", e.Comment.BuildUri(calendarEvent));
            notificationTemplate.Parse("POSTER", e.Poster.DisplayName);
            notificationTemplate.Parse("COMMENT", Functions.TrimStringToWord(e.Comment.Body, Notification.NOTIFICATION_MAX_BODY));

            ae.SendNotification(owner, string.Format("[user]{0}[/user] commented on your event.", e.Poster.Id), notificationTemplate.ToString());
        }

        /// <summary>
        /// Determines if a user can post a comment to an event.
        /// </summary>
        /// <param name="itemId">Event id</param>
        /// <param name="member">User to interrogate</param>
        /// <returns>True if the user can post a comment, false otherwise</returns>
        private bool eventCanPostComment(ItemKey itemKey, User member)
        {
            Event calendarEvent = new Event(core, null, itemKey.Id);
            calendarEvent.Access.SetViewer(member);

            if (calendarEvent.Access.Can("COMMENT") || calendarEvent.IsInvitee(member))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if a user can delete a comment from an event
        /// </summary>
        /// <param name="itemId">Event id</param>
        /// <param name="member">User to interrogate</param>
        /// <returns>True if the user can delete a comment, false otherwise</returns>
        private bool eventCanDeleteComment(ItemKey itemKey, User member)
        {
            Event calendarEvent = new Event(core, null, itemKey.Id);

            if (calendarEvent.UserId == member.UserId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Adjusts the comment count for the event
        /// </summary>
        /// <param name="itemId">Event id</param>
        /// <param name="adjustment">Amount to adjust the comment count by</param>
        private void eventAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            core.db.UpdateQuery(string.Format("UPDATE events SET event_comments = event_comments + {1} WHERE event_id = {0};",
                itemKey.Id, adjustment));
        }

        private void showCalendar(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Calendar.Show(core, page, page.User);
            }
        }

        private void showCalendarYear(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Calendar.Show(core, page, page.User, int.Parse(core.PagePathParts[1].Value));
            }
        }

        private void showCalendarMonth(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Calendar.Show(core, page, page.User, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        private void showCalendarDay(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Calendar.Show(core, page, page.User, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value), int.Parse(core.PagePathParts[3].Value));
            }
        }

        [Show(@"^/calendar/event/([0-9]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showEvent(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Event.Show(core, page, page.User, long.Parse(core.PagePathParts[1].Value));
            }
        }

        [Show(@"^/calendar/task/([0-9]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showTask(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Task.Show(core, page, page.User, long.Parse(core.PagePathParts[1].Value));
            }
        }

        private void showTasks(Core core, object sender)
        {
            if (sender is UPage)
            {
                UPage page = (UPage)sender;
                Task.ShowAll(core, page, page.User);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network;
        }

        void core_PageHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.None)
            {
                if (e.core.PagePath.ToLower() == "/default.aspx")
                {
                    ShowMiniCalendar(e);
                    ShowToday(e);
                }
            }
        }

        void ShowMiniCalendar(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todaymonthpanel");

            Calendar.DisplayMiniCalendar(e.core, template, e.core.session.LoggedInMember, e.core.tz.Now.Year, e.core.tz.Now.Month);

            e.core.AddSidePanel(template);
        }

        void ShowToday(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todayupcommingevents");

            long startTime = e.core.tz.GetUnixTimeStamp(new DateTime(e.core.tz.Now.Year, e.core.tz.Now.Month, e.core.tz.Now.Day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24 * 7; // skip ahead one week into the future

            Calendar cal = new Calendar(e.core);
            List<Event> events = cal.GetEvents(core, e.core.session.LoggedInMember, startTime, endTime);

            template.Parse("U_CALENDAR", core.Uri.AppendSid(string.Format("/{0}/calendar",
                e.core.session.LoggedInMember.UserName)));

            VariableCollection appointmentDaysVariableCollection = null;
            DateTime lastDay = e.core.tz.Now;

            if (events.Count > 0)
            {
                template.Parse("HAS_EVENTS", "TRUE");
            }

            foreach(Event calendarEvent in events)
            {
                DateTime eventDay = calendarEvent.GetStartTime(e.core.tz);
                DateTime eventEnd = calendarEvent.GetEndTime(e.core.tz);

                if (appointmentDaysVariableCollection == null || lastDay.Day != eventDay.Day)
                {
                    lastDay = eventDay;
                    appointmentDaysVariableCollection = template.CreateChild("appointment_days_list");

                    appointmentDaysVariableCollection.Parse("DAY", eventDay.DayOfWeek.ToString());
                }

                VariableCollection appointmentVariableCollection = appointmentDaysVariableCollection.CreateChild("appointments_list");

                appointmentVariableCollection.Parse("TIME", eventDay.ToShortTimeString() + " - " + eventEnd.ToShortTimeString());
                appointmentVariableCollection.Parse("SUBJECT", calendarEvent.Subject);
                appointmentVariableCollection.Parse("LOCATION", calendarEvent.Location);
                appointmentVariableCollection.Parse("URI", Event.BuildEventUri(core, calendarEvent));
            }

            e.core.AddMainPanel(template);

            //
            // Tasks panel
            //

            template = new Template(Assembly.GetExecutingAssembly(), "todaytaskspanel");
            List<Task> tasks = cal.GetTasks(core, e.core.session.LoggedInMember, startTime, endTime, true);

            VariableCollection taskDaysVariableCollection = null;
            lastDay = e.core.tz.Now;

            if (tasks.Count > 0)
            {
                template.Parse("HAS_TASKS", "TRUE");
            }

            template.Parse("U_TASKS", Task.BuildTasksUri(e.core, e.core.session.LoggedInMember));

            foreach (Task calendarTask in tasks)
            {
                DateTime taskDue = calendarTask.GetDueTime(e.core.tz);

                if (taskDaysVariableCollection == null || lastDay.Day != taskDue.Day)
                {
                    lastDay = taskDue;
                    taskDaysVariableCollection = template.CreateChild("task_days");

                    taskDaysVariableCollection.Parse("DAY", taskDue.DayOfWeek.ToString());
                }

                VariableCollection taskVariableCollection = taskDaysVariableCollection.CreateChild("task_list");

                taskVariableCollection.Parse("DATE", taskDue.ToShortDateString() + " (" + taskDue.ToShortTimeString() + ")");
                taskVariableCollection.Parse("TOPIC", calendarTask.Topic);
                taskVariableCollection.Parse("ID", calendarTask.Id.ToString());
                taskVariableCollection.Parse("URI", Task.BuildTaskUri(core, calendarTask));
                taskVariableCollection.Parse("U_MARK_COMPLETE", Task.BuildTaskMarkCompleteUri(core, calendarTask));

                if (calendarTask.Status == TaskStatus.Overdue)
                {
                    taskVariableCollection.Parse("OVERDUE", "TRUE");
                    taskVariableCollection.Parse("CLASS", "overdue-task");
                }
                else if (calendarTask.Status == TaskStatus.Completed)
                {
                    taskVariableCollection.Parse("COMPLETE", "TRUE");
                    taskVariableCollection.Parse("CLASS", "complete-task");
                }
                else
                {
                    taskVariableCollection.Parse("CLASS", "task");
                }

                if (calendarTask.Priority == TaskPriority.High)
                {
                    taskDaysVariableCollection.Parse("HIGH_PRIORITY", "TRUE");
                }
                else if (calendarTask.Priority == TaskPriority.Low)
                {
                    taskDaysVariableCollection.Parse("LOW_PRIORITY", "TRUE");
                }
            }

            e.core.AddSidePanel(template);
        }
    }
}
