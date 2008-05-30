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

namespace BoxSocial.Applications.Calendar
{
    public class Calendar
    {
        private Core core;
        private Mysql db;

        public Calendar(Core core)
        {
            this.core = core;
            this.db = core.db;
        }

        public List<Event> GetEvents(Core core, Primitive owner, long startTimeRaw, long endTimeRaw)
        {
            List<Event> events = new List<Event>();

            long loggedIdUid = User.GetMemberId(core.session.LoggedInMember);
            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);

            DataTable eventsTable = db.Query(string.Format("SELECT {0} FROM events ev WHERE (ev.event_access & {5:0} OR ev.user_id = {6}) AND ev.event_item_id = {1} AND ev.event_item_type = '{2}' AND ((ev.event_time_start_ut >= {3} AND ev.event_time_start_ut <= {4}) OR (ev.event_time_end_ut >= {3} AND ev.event_time_end_ut <= {4})) ORDER BY ev.event_time_start_ut ASC;",
                Event.EVENT_INFO_FIELDS, owner.Id, owner.Type, startTimeRaw, endTimeRaw, readAccessLevel, loggedIdUid));

            foreach (DataRow dr in eventsTable.Rows)
            {
                events.Add(new Event(core, owner, dr));
            }

            return events;
        }

        public List<Task> GetTasks(Core core, Primitive owner, long startTimeRaw, long endTimeRaw)
        {
            return GetTasks(core, owner, startTimeRaw, endTimeRaw, false);
        }

        public List<Task> GetTasks(Core core, Primitive owner, long startTimeRaw, long endTimeRaw, bool overdueTasks)
        {
            List<Task> tasks = new List<Task>();

            long loggedIdUid = core.LoggedInMemberId;
            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);

            if (overdueTasks)
            {
                DataTable tasksTable = db.Query(string.Format("SELECT {0} FROM tasks tk WHERE (tk.task_access & {5:0} OR tk.user_id = {6}) AND tk.task_item_id = {1} AND tk.task_item_type = '{2}' AND ((tk.task_due_date_ut >= {3} AND tk.task_due_date_ut <= {4}) OR ({7} > tk.task_due_date_ut AND tk.task_percent_complete < 100)) ORDER BY tk.task_due_date_ut ASC;",
                    Task.TASK_INFO_FIELDS, owner.Id, owner.Type, startTimeRaw, endTimeRaw, readAccessLevel, loggedIdUid, UnixTime.UnixTimeStamp()));

                foreach (DataRow dr in tasksTable.Rows)
                {
                    tasks.Add(new Task(core, owner, dr));
                }
            }
            else
            {
                DataTable tasksTable = db.Query(string.Format("SELECT {0} FROM tasks tk WHERE (tk.task_access & {5:0} OR tk.user_id = {6}) AND tk.task_item_id = {1} AND tk.task_item_type = '{2}' AND (tk.task_due_date_ut >= {3} AND tk.task_due_date_ut <= {4}) ORDER BY tk.task_due_date_ut ASC;",
                    Task.TASK_INFO_FIELDS, owner.Id, owner.Type, startTimeRaw, endTimeRaw, readAccessLevel, loggedIdUid));

                foreach (DataRow dr in tasksTable.Rows)
                {
                    tasks.Add(new Task(core, owner, dr));
                }
            }

            return tasks;
        }

        /// <summary>
        /// Returns any events the user has on their personal calendar, and any events on group and network calendars that they are attending.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="startTimeRaw"></param>
        /// <param name="endTimeRaw"></param>
        /// <returns></returns>
        public List<Event> GetMyEvents(User owner, long startTimeRaw, long endTimeRaw)
        {
            List<Event> events = new List<Event>();

            return events;
        }

        public static void DisplayMiniCalendar(Core core, Template template, Primitive owner, int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            DayOfWeek firstDay = new DateTime(year, month, 1).DayOfWeek;
            int offset = Calendar.GetFirstDayOfMonthOffset(firstDay);
            int weeks = (int)Math.Ceiling((days + offset) / 7.0);

            template.ParseVariables("CURRENT_MONTH", Functions.IntToMonth(core.tz.Now.Month));
            template.ParseVariables("CURRENT_YEAR", core.tz.Now.Year.ToString());

            for (int week = 0; week < weeks; week++)
            {
                VariableCollection weekVariableCollection = template.CreateChild("week");

                weekVariableCollection.ParseVariables("WEEK", HttpUtility.HtmlEncode((week + 1).ToString()));

                if (week + 1 == 1)
                {
                    int daysPrev = DateTime.DaysInMonth(year - (month - 1) / 12, (month - 1) % 12 + 1);
                    for (int i = offset - 1; i >= 0; i--)
                    {
                        int day = daysPrev - i;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(day.ToString()));
                        dayVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Calendar.BuildDateUri(owner, year - (month - 2) / 12, (month - 2) % 12 + 1, day)));
                    }
                    for (int i = offset; i < 7; i++)
                    {
                        int day = i - offset + 1;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(day.ToString()));
                        dayVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Calendar.BuildDateUri(owner, year, month, day)));
                    }
                }
                else if (week + 1 == weeks)
                {
                    for (int i = week * 7 - offset; i < days; i++)
                    {
                        int day = i + 1;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(day.ToString()));
                        dayVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Calendar.BuildDateUri(owner, year, month, day)));
                    }
                    for (int i = 0; i < weeks * 7 - days - offset; i++)
                    {
                        int day = i + 1;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(day.ToString()));
                        dayVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Calendar.BuildDateUri(owner, year + (month) / 12, (month) % 12 + 1, day)));
                    }
                }
                else
                {
                    for (int i = 0; i < 7; i++)
                    {
                        int day = week * 7 + i + 1 - offset;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(day.ToString()));
                        dayVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Calendar.BuildDateUri(owner, year, month, day)));
                    }
                }
            }
        }

        private static string BuildDateUri(Primitive owner, int year, int month, int day)
        {
            return Linker.AppendSid(string.Format("/{0}/calendar/{1}/{2}/{3}",
                owner.Key, year, month, day));
        }

        private static int GetFirstDayOfMonthOffset(DayOfWeek firstDay)
        {
            // First day of the week is Monday
            switch (firstDay)
            {
                case DayOfWeek.Monday:
                    return 0;
                case DayOfWeek.Tuesday:
                    return 1;
                case DayOfWeek.Wednesday:
                    return 2;
                case DayOfWeek.Thursday:
                    return 3;
                case DayOfWeek.Friday:
                    return 4;
                case DayOfWeek.Saturday:
                    return 5;
                case DayOfWeek.Sunday:
                    return 6;
            }
            return 0;
        }

        public static void Show(Core core, TPage page, Primitive owner)
        {
            Show(core, page, owner, core.tz.Now.Year, core.tz.Now.Month);
        }

        public static void Show(Core core, TPage page, Primitive owner, int year, int month)
        {
            page.template.SetTemplate("Calendar", "viewcalendarmonth");

            page.template.ParseVariables("CURRENT_MONTH", HttpUtility.HtmlEncode(Functions.IntToMonth(month)));
            page.template.ParseVariables("CURRENT_YEAR", HttpUtility.HtmlEncode(year.ToString()));

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.ParseVariables("U_NEW_EVENT", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", year),
                    string.Format("month={0}", month),
                    string.Format("day={0}", ((month == core.tz.Now.Month) ? core.tz.Now.Day : 1)))));
            }

            int days = DateTime.DaysInMonth(year, month);
            DayOfWeek firstDay = new DateTime(year, month, 1).DayOfWeek;
            int offset = Calendar.GetFirstDayOfMonthOffset(firstDay);
            int weeks = (int)Math.Ceiling((days + offset) / 7.0);
            int daysPrev = DateTime.DaysInMonth(year - (month - 1) / 12, (core.tz.Now.Month - 1) % 12 + 1);

            long startTime = 0;
            if (offset > 0)
            {
                // the whole month including entry days
                startTime = core.tz.GetUnixTimeStamp(new DateTime(year - (month - 2) / 12, (month - 2) % 12 + 1, daysPrev - offset + 1, 0, 0, 0));
            }
            else
            {
                // the whole month
                startTime = core.tz.GetUnixTimeStamp(new DateTime(year, month, 1, 0, 0, 0));
            }

            // the whole month including exit days
            long endTime = startTime + 60 * 60 * 24 * weeks * 7;

            Calendar cal = new Calendar(core);
            List<Event> events = cal.GetEvents(core, owner, startTime, endTime);

            /*foreach (Event calEvent in events)
            {
                //HttpContext.Current.Response.Write("<hr />");
            }*/

            for (int week = 0; week < weeks; week++)
            {
                VariableCollection weekVariableCollection = page.template.CreateChild("week");

                weekVariableCollection.ParseVariables("WEEK", HttpUtility.HtmlEncode((week + 1).ToString()));

                /* lead in week */
                if (week + 1 == 1)
                {
                    int daysPrev2 = DateTime.DaysInMonth(year - (month - 2) / 12, (month - 2) % 12 + 1);
                    /* days in month prior */
                    for (int i = offset - 1; i >= 0; i--)
                    {
                        int day = daysPrev2 - i;

                        Calendar.showDayEvents(core, owner, year - (month - 2) / 12, (month - 2) % 12 + 1, day, weekVariableCollection, events);
                    }
                    /* first days in month */
                    for (int i = offset; i < 7; i++)
                    {
                        int day = i - offset + 1;

                        Calendar.showDayEvents(core, owner, year, month, day, weekVariableCollection, events);
                    }
                }
                /* lead out week */
                else if (week + 1 == weeks)
                {
                    /* last days in month */
                    for (int i = week * 7 - offset; i < days; i++)
                    {
                        int day = i + 1;

                        Calendar.showDayEvents(core, owner, year, month, day, weekVariableCollection, events);
                    }
                    /* days in month upcoming */
                    for (int i = 0; i < weeks * 7 - days - offset; i++)
                    {
                        int day = i + 1;

                        Calendar.showDayEvents(core, owner, year + (month) / 12, (month) % 12 + 1, day, weekVariableCollection, events);
                    }
                }
                else
                {
                    for (int i = 0; i < 7; i++)
                    {
                        int day = week * 7 + i + 1 - offset;

                        Calendar.showDayEvents(core, owner, year, month, day, weekVariableCollection, events);
                    }
                }
            }

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", "Calendar" });
            calendarPath.Add(new string[] { year.ToString(), year.ToString() });
            calendarPath.Add(new string[] { month.ToString(), Functions.IntToMonth(month) });
            page.template.ParseVariables("BREADCRUMBS", owner.GenerateBreadCrumbs(calendarPath));
        }

        public static void Show(Core core, TPage page, Primitive owner, int year, int month, int day)
        {
            page.template.SetTemplate("Calendar", "viewcalendarday");

            page.template.ParseVariables("CURRENT_DAY", HttpUtility.HtmlEncode(day.ToString()));
            page.template.ParseVariables("CURRENT_MONTH", HttpUtility.HtmlEncode(Functions.IntToMonth(month)));
            page.template.ParseVariables("CURRENT_YEAR", HttpUtility.HtmlEncode(year.ToString()));

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.ParseVariables("U_NEW_EVENT", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", year),
                    string.Format("month={0}", month),
                    string.Format("day={0}", day))));
            }

            long startTime = core.tz.GetUnixTimeStamp(new DateTime(year, month, day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24;

            Calendar cal = new Calendar(core);
            List<Event> events = cal.GetEvents(core, owner, startTime, endTime);

            for (int hour = 0; hour < 24; hour++)
            {
                VariableCollection timeslotVariableCollection = page.template.CreateChild("timeslot");

                DateTime hourTime = new DateTime(year, month, day, hour, 0, 0);

                timeslotVariableCollection.ParseVariables("TIME", HttpUtility.HtmlEncode(hourTime.ToString("h tt").ToLower()));

                showHourEvents(core, owner, year, month, day, hour, timeslotVariableCollection, events);
            }

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", "Calendar" });
            calendarPath.Add(new string[] { year.ToString(), year.ToString() });
            calendarPath.Add(new string[] { month.ToString(), Functions.IntToMonth(month) });
            calendarPath.Add(new string[] { day.ToString(), day.ToString() });
            page.template.ParseVariables("BREADCRUMBS", owner.GenerateBreadCrumbs(calendarPath));
        }

        private static void showDayEvents(Core core, Primitive owner, int year, int month, int day, VariableCollection weekVariableCollection, List<Event> events)
        {
            VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
            dayVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(day.ToString()));
            dayVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Calendar.BuildDateUri(owner, year, month, day)));
            bool hasEvents = false;

            List<Event> expired = new List<Event>();
            foreach (Event calendarEvent in events)
            {
                // if the event starts after the end of the day, skip this day
                if (calendarEvent.GetStartTime(core.tz).CompareTo(new DateTime(year, month, day, 23, 59, 59)) > 0)
                {
                    break;
                }

                VariableCollection eventVariableCollection = dayVariableCollection.CreateChild("event");

                eventVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(calendarEvent.Subject.Substring(0, Math.Min(7, calendarEvent.Subject.Length))));
                eventVariableCollection.ParseVariables("START_TIME", HttpUtility.HtmlEncode(calendarEvent.GetStartTime(core.tz).ToString("h:mmt").ToLower()));
                eventVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Event.BuildEventUri(calendarEvent)));

                hasEvents = true;

                // if the event ends before the end of the day, finish up the event
                if (calendarEvent.GetEndTime(core.tz).CompareTo(new DateTime(year, month, day, 23, 59, 59)) <= 0)
                {
                    expired.Add(calendarEvent);
                }
            }

            if (hasEvents)
            {
                dayVariableCollection.ParseVariables("EVENTS", "TRUE");
            }

            foreach (Event calendarEvent in expired)
            {
                events.Remove(calendarEvent);
            }
        }

        private static void showHourEvents(Core core, Primitive owner, int year, int month, int day, int hour, VariableCollection timeslotVariableCollection, List<Event> events)
        {
            bool hasEvents = false;

            List<Event> expired = new List<Event>();
            foreach (Event calendarEvent in events)
            {
                if (calendarEvent.GetStartTime(core.tz).CompareTo(new DateTime(year, month, day, hour, 59, 59)) > 0)
                {
                    break;
                }

                VariableCollection eventVariableCollection = timeslotVariableCollection.CreateChild("event");

                long height = (calendarEvent.EndTimeRaw - calendarEvent.StartTimeRaw) * 24 / 60 / 60;

                eventVariableCollection.ParseVariables("TITLE", HttpUtility.HtmlEncode(calendarEvent.Subject));
                eventVariableCollection.ParseVariables("HEIGHT", HttpUtility.HtmlEncode(height.ToString()));
                eventVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Event.BuildEventUri(calendarEvent)));

                hasEvents = true;

                expired.Add(calendarEvent);
            }

            if (hasEvents)
            {
                timeslotVariableCollection.ParseVariables("EVENTS", "TRUE");
            }

            foreach (Event calendarEvent in expired)
            {
                events.Remove(calendarEvent);
            }
        }
    }
}
