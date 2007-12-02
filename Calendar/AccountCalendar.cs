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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Applications.Calendar;

namespace BoxSocial.Applications.Calendar
{
    public class AccountCalendar : AccountModule
    {
        public AccountCalendar(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageCalendar);
            RegisterSubModule += new RegisterSubModuleHandler(NewEvent);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
            
        }

        public override string Name
        {
            get
            {
                return "Calendar";
            }
        }

        public override string Key
        {
            get
            {
                return "calendar";
            }
        }

        public override int Order
        {
            get
            {
                return 9;
            }
        }

        private void ManageCalendar(string submodule)
        {
            subModules.Add("calendar", "Manage Calendar");
            if (submodule != "calendar" && !string.IsNullOrEmpty(submodule)) return;
        }

        private void NewEvent(string submodule)
        {
            subModules.Add("new-event", "New Event");
            if (submodule != "new-event") return;

            if (Request.Form["save"] != null)
            {
                SaveNewEvent();
            }

            bool edit = false;
            ushort eventAccess = 0;

            if (Request.QueryString["mode"] == "edit")
            {
                edit = true;
            }

            template.SetTemplate("account_calendar_event_new.html");

            int year = Functions.RequestInt("year", tz.Now.Year);
            int month = Functions.RequestInt("month", tz.Now.Month);
            int day = Functions.RequestInt("day", tz.Now.Day);

            DateTime startDate = new DateTime(year, month, day, 8, 0, 0);
            DateTime endDate = new DateTime(year, month, day, 9, 0, 0);

            string subject = "";
            string location = "";
            string description = "";

            Dictionary<string, string> years = new Dictionary<string, string>();
            for (int i = DateTime.Now.AddYears(-110).Year; i < DateTime.Now.AddYears(110).Year; i++)
            {
                years.Add(i.ToString(), i.ToString());
            }

            Dictionary<string, string> months = new Dictionary<string, string>();
            for (int i = 1; i < 13; i++)
            {
                months.Add(i.ToString(), Functions.IntToMonth(i));
            }

            Dictionary<string, string> days = new Dictionary<string, string>();
            for (int i = 1; i < 32; i++)
            {
                days.Add(i.ToString(), i.ToString());
            }

            Dictionary<string, string> hours = new Dictionary<string, string>();
            for (int i = 0; i < 24; i++)
            {
                DateTime hourTime = new DateTime(year, month, day, i, 0, 0);
                hours.Add(i.ToString(), hourTime.ToString("h tt").ToLower());
            }

            Dictionary<string, string> minutes = new Dictionary<string, string>();
            for (int i = 0; i < 60; i++)
            {
                minutes.Add(i.ToString(), string.Format("{0:00}", i));
            }

            if (edit)
            {
                int id = Functions.RequestInt("id", -1);

                if (id < 1)
                {
                    Display.ShowMessage(core, "Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }

                try
                {
                    Event calendarEvent = new Event(db, loggedInMember, id);

                    template.ParseVariables("EDIT", "TRUE");
                    template.ParseVariables("ID", HttpUtility.HtmlEncode(calendarEvent.EventId.ToString()));

                    startDate = calendarEvent.GetStartTime(core.tz);
                    endDate = calendarEvent.GetEndTime(core.tz);

                    eventAccess = calendarEvent.Permissions;

                    subject = calendarEvent.Subject;
                    location = calendarEvent.Location;
                    description = calendarEvent.Description;
                }
                catch
                {
                    Display.ShowMessage(core, "Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }
            }

            template.ParseVariables("S_YEAR", HttpUtility.HtmlEncode(year.ToString()));
            template.ParseVariables("S_MONTH", HttpUtility.HtmlEncode(month.ToString()));
            template.ParseVariables("S_DAY", HttpUtility.HtmlEncode(day.ToString()));

            template.ParseVariables("S_START_YEAR", Functions.BuildSelectBox("start-year", years, startDate.Year.ToString()));
            template.ParseVariables("S_END_YEAR", Functions.BuildSelectBox("end-year", years, endDate.Year.ToString()));

            template.ParseVariables("S_START_MONTH", Functions.BuildSelectBox("start-month", months, startDate.Month.ToString()));
            template.ParseVariables("S_END_MONTH", Functions.BuildSelectBox("end-month", months, endDate.Month.ToString()));

            template.ParseVariables("S_START_DAY", Functions.BuildSelectBox("start-day", days, startDate.Day.ToString()));
            template.ParseVariables("S_END_DAY", Functions.BuildSelectBox("end-day", days, endDate.Day.ToString()));

            template.ParseVariables("S_START_HOUR", Functions.BuildSelectBox("start-hour", hours, startDate.Hour.ToString()));
            template.ParseVariables("S_END_HOUR", Functions.BuildSelectBox("end-hour", hours, endDate.Hour.ToString()));

            template.ParseVariables("S_START_MINUTE", Functions.BuildSelectBox("start-minute", minutes, startDate.Minute.ToString()));
            template.ParseVariables("S_END_MINUTE", Functions.BuildSelectBox("end-minute", minutes, endDate.Minute.ToString()));

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            template.ParseVariables("S_EVENT_PERMS", Functions.BuildPermissionsBox(eventAccess, permissions));

            template.ParseVariables("S_SUBJECT", HttpUtility.HtmlEncode(subject));
            template.ParseVariables("S_LOCATION", HttpUtility.HtmlEncode(location));
            template.ParseVariables("S_DESCRIPTION", HttpUtility.HtmlEncode(description));

            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(ZzUri.AppendSid("/account/", true)));
        }

        private void SaveNewEvent()
        {
            long eventId = 0;
            string subject = "";
            string location = "";
            string description = "";
            DateTime startTime = tz.Now;
            DateTime endTime = tz.Now;
            bool edit = false;

            if (Request.QueryString["sid"] != session.SessionId)
            {
                Display.ShowMessage(core, "Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            if (Request.Form["mode"] == "edit")
            {
                edit = true;
            }

            try
            {
                subject = Request.Form["subject"];
                location = Request.Form["location"];
                description = Request.Form["description"];

                startTime = new DateTime(
                    int.Parse(Request.Form["start-year"]),
                    int.Parse(Request.Form["start-month"]),
                    int.Parse(Request.Form["start-day"]),
                    int.Parse(Request.Form["start-hour"]),
                    int.Parse(Request.Form["start-minute"]),
                    0);

                endTime = new DateTime(
                    int.Parse(Request.Form["end-year"]),
                    int.Parse(Request.Form["end-month"]),
                    int.Parse(Request.Form["end-day"]),
                    int.Parse(Request.Form["end-hour"]),
                    int.Parse(Request.Form["end-minute"]),
                    0);

                if (edit)
                {
                    eventId = long.Parse(Request.Form["id"]);
                }
            }
            catch
            {
                Display.ShowMessage(core, "Invalid submission", "You have made an invalid form submission.");
                return;
            }


            if (!edit)
            {
                Event calendarEvent = Event.Create(db, loggedInMember, loggedInMember, subject, location, description, tz.GetUnixTimeStamp(startTime), tz.GetUnixTimeStamp(endTime), Functions.GetPermission());

                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Event.BuildEventUri(calendarEvent)));
                Display.ShowMessage(core, "Event Created", "You have successfully created a new event.");
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE events SET event_subject = '{2}', event_location = '{3}', event_description = '{4}', event_time_start_ut = {5}, event_time_end_ut = {6}, event_access = {7} WHERE user_id = {0} AND event_id = {1};",
                    loggedInMember.UserId, eventId, Mysql.Escape(subject), Mysql.Escape(location), Mysql.Escape(description), tz.GetUnixTimeStamp(startTime), tz.GetUnixTimeStamp(endTime), Functions.GetPermission()));

                Event calendarEvent = new Event(db, loggedInMember, eventId);

                template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Event.BuildEventUri(calendarEvent)));
                Display.ShowMessage(core, "Event Saved", "You have successfully saved your changes to the event.");
            }
        }
    }
}
