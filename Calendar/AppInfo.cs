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
        public override string Title
        {
            get
            {
                return "Calendar";
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
                return false;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            aii.AddSlug("calendar", @"^/calendar(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/([0-9]{4})(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/([0-9]{4})/([0-9]{1,2})(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/([0-9]{4})/([0-9]{1,2})/([0-9]{1,2})(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);
            aii.AddSlug("calendar", @"^/calendar/event/([0-9]+)(|/)$", AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network);

            aii.AddModule("calendar");

            return aii;
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

            core.RegisterApplicationPage(@"^/calendar(|/)$", showCalendar, 1);
            core.RegisterApplicationPage(@"^/calendar/([0-9]{4})(|/)$", showCalendarYear, 2);
            core.RegisterApplicationPage(@"^/calendar/([0-9]{4})/([0-9]{1,2})(|/)$", showCalendarMonth, 3);
            core.RegisterApplicationPage(@"^/calendar/([0-9]{4})/([0-9]{1,2})/([0-9]{1,2})(|/)$", showCalendarDay, 4);
            core.RegisterApplicationPage(@"^/calendar/event/([0-9]+)(|/)$", showEvent, 5);
        }

        private void showCalendar(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.ProfileOwner);
            }
        }

        private void showCalendarYear(Core core, object sender)
        {
        }

        private void showCalendarMonth(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.ProfileOwner, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        private void showCalendarDay(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page, page.ProfileOwner, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value), int.Parse(core.PagePathParts[3].Value));
            }
        }

        private void showEvent(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Event.Show(core, page, page.ProfileOwner, long.Parse(core.PagePathParts[1].Value));
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
                if (e.core.PagePath == "/default.aspx")
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

            Calendar cal = new Calendar(e.core.db);
            List<Event> events = cal.GetEvents(core, e.core.session.LoggedInMember, startTime, endTime);

            template.ParseVariables("U_CALENDAR", HttpUtility.HtmlEncode(ZzUri.AppendSid(string.Format("/{0}/calendar",
                e.core.session.LoggedInMember.UserName))));

            VariableCollection appointmentDaysVariableCollection = null;
            DateTime lastDay = e.core.tz.Now;

            if (events.Count > 0)
            {
                template.ParseVariables("HAS_EVENTS", "TRUE");
            }

            foreach(Event calendarEvent in events)
            {
                DateTime eventDay = calendarEvent.GetStartTime(e.core.tz);
                DateTime eventEnd = calendarEvent.GetEndTime(e.core.tz);

                if (appointmentDaysVariableCollection == null || lastDay.Day != eventDay.Day)
                {
                    appointmentDaysVariableCollection = template.CreateChild("appointment_days_list");

                    appointmentDaysVariableCollection.ParseVariables("DAY", HttpUtility.HtmlEncode(eventDay.DayOfWeek.ToString()));
                }

                VariableCollection appointmentVariableCollection = appointmentDaysVariableCollection.CreateChild("appointments_list");

                appointmentVariableCollection.ParseVariables("TIME", HttpUtility.HtmlEncode(eventDay.ToShortTimeString() + " - " + eventEnd.ToShortTimeString()));
                appointmentVariableCollection.ParseVariables("SUBJECT", HttpUtility.HtmlEncode(calendarEvent.Subject));
                appointmentVariableCollection.ParseVariables("LOCATION", HttpUtility.HtmlEncode(calendarEvent.Location));
                appointmentVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Event.BuildEventUri(calendarEvent)));
            }

            e.core.AddMainPanel(template);
        }
    }
}
