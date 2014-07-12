/*
 * Box Social™
 * http://boxsocial.net/
  * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
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
using BoxSocial.Forms;
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
                return string.Empty;
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
                return Properties.Resources.icon;
            }
        }

        public override byte[] SvgIcon
        {
            get
            {
                return Properties.Resources.svgIcon;
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
            core.PostHooks += new Core.HookHandler(core_PostHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override bool ExecuteJob(Job job)
        {
            if (job.ItemId == 0)
            {
                return true;
            }

            switch (job.Function)
            {
                case "notifyEventComment":
                    Event.NotifyEventComment(core, job);
                    return true;
                case "notifyEventInvite":
                    break;
            }

            return false;
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = this.GetInstallInfo();

            aii.AddCommentType("EVENT");

            return aii;
        }

        public override Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get
            {
                Dictionary<string, PageSlugAttribute> slugs = new Dictionary<string, PageSlugAttribute>();
                //slugs.Add("calendar", new PageSlugAttribute("Calendar", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network));
                slugs.Add("calendar/events", new PageSlugAttribute("Events", AppPrimitives.Group | AppPrimitives.Network));
                slugs.Add("calendar/tasks", new PageSlugAttribute("Tasks", AppPrimitives.Group | AppPrimitives.Network));
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;
        }

        [Show(@"calendar", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showCalendar(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.Owner);
            }
        }

        [Show(@"calendar/([0-9]{4})", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showCalendarYear(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.Owner, int.Parse(core.PagePathParts[1].Value));
            }
        }

        [Show(@"calendar/([0-9]{4})/([0-9]{1,2})", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showCalendarMonth(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.Owner, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        [Show(@"calendar/([0-9]{4})/([0-9]{1,2})/([0-9]{1,2})", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showCalendarDay(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.Owner, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value), int.Parse(core.PagePathParts[3].Value));
            }
        }

        [Show(@"calendar/event/([\-0-9]+)", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showEvent(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Event.Show(sender, new ShowPPageEventArgs(page, long.Parse(core.PagePathParts[1].Value)));
            }
        }

        [Show(@"calendar/events", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showEvents(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Event.ShowAll(core, new ShowPPageEventArgs(page));
            }
        }

        [Show(@"calendar/task/([0-9]+)", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showTask(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Task.Show(core, page, page.Owner, long.Parse(core.PagePathParts[1].Value));
            }
        }

        [Show(@"calendar/tasks", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network)]
        private void showTasks(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Task.ShowAll(core, page, page.Owner);
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network | AppPrimitives.Application | AppPrimitives.Musician;
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

        void core_PostHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Member)
            {
                PostContent(e);
            }
        }

        void PostContent(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "postevent");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            string formSubmitUri = core.Hyperlink.AppendSid(e.Owner.AccountUriStub, true);
            template.Parse("U_ACCOUNT", formSubmitUri);
            template.Parse("S_ACCOUNT", formSubmitUri);

            int year = core.Functions.RequestInt("year", core.Tz.Now.Year);
            int month = core.Functions.RequestInt("month", core.Tz.Now.Month);
            int day = core.Functions.RequestInt("day", core.Tz.Now.Day);

            DateTimePicker startDateTimePicker = new DateTimePicker(core, "start-date");
            startDateTimePicker.ShowTime = true;
            startDateTimePicker.ShowSeconds = false;

            DateTimePicker endDateTimePicker = new DateTimePicker(core, "end-date");
            endDateTimePicker.ShowTime = true;
            endDateTimePicker.ShowSeconds = false;

            UserSelectBox inviteesUserSelectBox = new UserSelectBox(core, "invitees");

            /* */
            SelectBox timezoneSelectBox = UnixTime.BuildTimeZoneSelectBox("timezone");

            DateTime startDate = new DateTime(year, month, day, 8, 0, 0);
            DateTime endDate = new DateTime(year, month, day, 9, 0, 0);
            timezoneSelectBox.SelectedKey = core.Tz.TimeZoneCode.ToString();

            template.Parse("S_YEAR", year.ToString());
            template.Parse("S_MONTH", month.ToString());
            template.Parse("S_DAY", day.ToString());


            startDateTimePicker.Value = startDate;
            endDateTimePicker.Value = endDate;

            template.Parse("S_START_DATE", startDateTimePicker);
            template.Parse("S_END_DATE", endDateTimePicker);
            template.Parse("S_TIMEZONE", timezoneSelectBox);
            template.Parse("S_INVITEES", inviteesUserSelectBox);

            e.core.AddPostPanel("Event", template);
        }

        void ShowMiniCalendar(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todaymonthpanel");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            template.Parse("URI", Calendar.BuildMonthUri(e.core, e.core.Session.LoggedInMember, e.core.Tz.Now.Year, e.core.Tz.Now.Month));
            Calendar.DisplayMiniCalendar(e.core, template, e.core.Session.LoggedInMember, e.core.Tz.Now.Year, e.core.Tz.Now.Month);

            e.core.AddSidePanel(template);
        }

        void ShowToday(HookEventArgs e)
        {
            Template template = new Template(Assembly.GetExecutingAssembly(), "todayupcommingevents");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            long startTime = e.core.Tz.GetUnixTimeStamp(new DateTime(e.core.Tz.Now.Year, e.core.Tz.Now.Month, e.core.Tz.Now.Day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24 * 7; // skip ahead one week into the future

            Calendar cal = null;
            try
            {
                cal = new Calendar(core, core.Session.LoggedInMember);
            }
            catch (InvalidCalendarException)
            {
                cal = Calendar.Create(core, core.Session.LoggedInMember);
            }

            List<Event> events = cal.GetEvents(core, e.core.Session.LoggedInMember, startTime, endTime);

            template.Parse("U_NEW_EVENT", core.Hyperlink.AppendSid(string.Format("{0}calendar/new-event",
                e.core.Session.LoggedInMember.AccountUriStub)));

            template.Parse("U_CALENDAR", core.Hyperlink.AppendSid(string.Format("{0}calendar",
                e.core.Session.LoggedInMember.UriStub)));

            VariableCollection appointmentDaysVariableCollection = null;
            DateTime lastDay = e.core.Tz.Now;

            if (events.Count > 0)
            {
                template.Parse("HAS_EVENTS", "TRUE");
            }

            foreach(Event calendarEvent in events)
            {
                DateTime eventDay = calendarEvent.GetStartTime(e.core.Tz);
                DateTime eventEnd = calendarEvent.GetEndTime(e.core.Tz);

                if (appointmentDaysVariableCollection == null || lastDay.Day != eventDay.Day)
                {
                    lastDay = eventDay;
                    appointmentDaysVariableCollection = template.CreateChild("appointment_days_list");

                    appointmentDaysVariableCollection.Parse("DAY", core.Tz.DateTimeToDateString(eventDay));
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
            template.SetProse(core.Prose);
            List<Task> tasks = cal.GetTasks(core, e.core.Session.LoggedInMember, startTime, endTime, true);

            VariableCollection taskDaysVariableCollection = null;
            lastDay = e.core.Tz.Now;

            if (tasks.Count > 0)
            {
                template.Parse("HAS_TASKS", "TRUE");
            }

            template.Parse("U_TASKS", Task.BuildTasksUri(e.core, e.core.Session.LoggedInMember));

            foreach (Task calendarTask in tasks)
            {
                DateTime taskDue = calendarTask.GetDueTime(e.core.Tz);

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
