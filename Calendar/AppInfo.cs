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
                Calendar.Show(core, page.ProfileOwner);
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
                Calendar.Show(core, page.ProfileOwner, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value));
            }
        }

        private void showCalendarDay(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Calendar.Show(core, page.ProfileOwner, int.Parse(core.PagePathParts[1].Value), int.Parse(core.PagePathParts[2].Value), int.Parse(core.PagePathParts[3].Value));
            }
        }

        private void showEvent(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Event.Show(core, page.ProfileOwner, long.Parse(core.PagePathParts[1].Value));
            }
        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network;
        }

        void core_PageHooks(Core core, object sender)
        {
            if (sender is TPage)
            {
                TPage page = (TPage)sender;

                switch (page.Signature)
                {
                    case PageSignature.today:
                        Calendar.DisplayMiniCalendar(core, page.session.LoggedInMember, page.tz.Now.Year, page.tz.Now.Month);
                        ShowToday(core.db, page);
                        break;
                }
            }
        }

        void ShowToday(Mysql db, TPage page)
        {
            long startTime = page.tz.GetUnixTimeStamp(new DateTime(page.tz.Now.Year, page.tz.Now.Month, page.tz.Now.Day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24 * 7; // skip ahead one week into the future

            Calendar cal = new Calendar(db);
            List<Event> events = cal.GetEvents(core, page.session.LoggedInMember, startTime, endTime);

            VariableCollection appointmentDaysVariableCollection = null;
            DateTime lastDay = page.tz.Now;

            if (events.Count > 0)
            {
                page.template.ParseVariables("HAS_EVENTS", "TRUE");
            }

            foreach(Event calendarEvent in events)
            {
                DateTime eventDay = calendarEvent.GetStartTime(page.tz);
                DateTime eventEnd = calendarEvent.GetEndTime(page.tz);

                if (appointmentDaysVariableCollection == null || lastDay.Day != eventDay.Day)
                {
                    appointmentDaysVariableCollection = page.template.CreateChild("appointment_days_list");

                    appointmentDaysVariableCollection.ParseVariables("DAY", HttpUtility.HtmlEncode(eventDay.DayOfWeek.ToString()));
                }

                VariableCollection appointmentVariableCollection = appointmentDaysVariableCollection.CreateChild("appointments_list");

                appointmentVariableCollection.ParseVariables("TIME", HttpUtility.HtmlEncode(eventDay.ToShortTimeString() + " - " + eventEnd.ToShortTimeString()));
                appointmentVariableCollection.ParseVariables("SUBJECT", HttpUtility.HtmlEncode(calendarEvent.Subject));
                appointmentVariableCollection.ParseVariables("LOCATION", HttpUtility.HtmlEncode(calendarEvent.Location));
                appointmentVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Event.BuildEventUri(calendarEvent)));
            }
        }
    }
}
