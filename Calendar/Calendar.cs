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
			
			string[] fields = Event.GetFieldsPrefixed(typeof(Event));
			StringBuilder fieldsSb = new StringBuilder();
			bool first = true;
			
			foreach (string field in fields)
			{
				if (!first)
				{
					fieldsSb.Append(", ");
				}
				fieldsSb.Append(field);
				first = false;
			}
			
            DataTable eventsTable = db.Query(string.Format("SELECT {0} FROM events WHERE (event_access & {5:0} OR user_id = {6}) AND event_item_id = {1} AND event_item_type_id = {2} AND ((event_time_start_ut >= {3} AND event_time_start_ut <= {4}) OR (event_time_end_ut >= {3} AND event_time_end_ut <= {4}) OR (event_time_start_ut < {3} AND event_time_end_ut > {4})) ORDER BY event_time_start_ut ASC;",
                fieldsSb, owner.Id, owner.TypeId, startTimeRaw, endTimeRaw, readAccessLevel, loggedIdUid));

            foreach (DataRow dr in eventsTable.Rows)
            {
                events.Add(new Event(core, owner, dr));
            }

            if (owner.TypeId == ItemKey.GetTypeId(typeof(User)))
            {
                // now select events invited to
                SelectQuery query = Event.GetSelectQueryStub(typeof(Event));
                query.AddFields("event_invites.item_id", "event_invites.item_type_id", "event_invites.inviter_id", "event_invites.event_id");
                query.AddJoin(JoinTypes.Left, "event_invites", "event_id", "event_id");
                query.AddCondition("item_id", loggedIdUid);
                query.AddCondition("item_type_id", ItemKey.GetTypeId(typeof(User)));
                QueryCondition qc2 = query.AddCondition("event_time_start_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
                qc2.AddCondition("event_time_start_ut", ConditionEquality.LessThanEqual, endTimeRaw);
                QueryCondition qc3 = qc2.AddCondition(ConditionRelations.Or, "event_time_end_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
                qc3.AddCondition("event_time_end_ut", ConditionEquality.LessThanEqual, endTimeRaw);
                QueryCondition qc4 = qc3.AddCondition(ConditionRelations.Or, "event_time_start_ut", ConditionEquality.LessThan, startTimeRaw);
                qc4.AddCondition("event_time_end_ut", ConditionEquality.GreaterThan, endTimeRaw);

                eventsTable = db.Query(query);

                foreach (DataRow dr in eventsTable.Rows)
                {
                    events.Add(new Event(core, owner, dr));
                }


                User user = (User)owner;
                List<UserRelation> friends = user.GetFriendsBirthdays(startTimeRaw, endTimeRaw);

                foreach (UserRelation friend in friends)
                {
                    try
                    {
                        events.Add(new BirthdayEvent(core, user, friend, core.tz.DateTimeFromMysql(startTimeRaw).Year));
                    }
                    catch (InvalidEventException)
                    {
                        // Not a reciprocol friend, ignore
                    }
                }
            }

            events.Sort();

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
			
			string[] fields = Event.GetFieldsPrefixed(typeof(Task));
			StringBuilder fieldsSb = new StringBuilder();
			bool first = true;
			
			foreach (string field in fields)
			{
				if (!first)
				{
					fieldsSb.Append(", ");
				}
				fieldsSb.Append(field);
				first = false;
			}

            if (overdueTasks)
            {
                DataTable tasksTable = db.Query(string.Format("SELECT {0} FROM tasks WHERE (task_access & {5:0} OR user_id = {6}) AND task_item_id = {1} AND task_item_type_id = {2} AND ((task_due_date_ut >= {3} AND task_due_date_ut <= {4}) OR ({7} > task_due_date_ut AND task_percent_complete < 100)) ORDER BY task_due_date_ut ASC;",
                    fieldsSb, owner.Id, owner.TypeId, startTimeRaw, endTimeRaw, readAccessLevel, loggedIdUid, UnixTime.UnixTimeStamp()));

                foreach (DataRow dr in tasksTable.Rows)
                {
                    tasks.Add(new Task(core, owner, dr));
                }
            }
            else
            {
                DataTable tasksTable = db.Query(string.Format("SELECT {0} FROM tasks WHERE (task_access & {5:0} OR user_id = {6}) AND task_item_id = {1} AND task_item_type_id = {2} AND (task_due_date_ut >= {3} AND task_due_date_ut <= {4}) ORDER BY task_due_date_ut ASC;",
                    fieldsSb, owner.Id, owner.TypeId, startTimeRaw, endTimeRaw, readAccessLevel, loggedIdUid));

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
            DisplayMiniCalendar(core, template, null, owner, year, month);
        }

        public static void DisplayMiniCalendar(Core core, VariableCollection vc1, Primitive owner, int year, int month)
        {
            DisplayMiniCalendar(core, null, vc1, owner, year, month);
        }

        private static void DisplayMiniCalendar(Core core, Template template, VariableCollection vc1, Primitive owner, int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            DayOfWeek firstDay = new DateTime(year, month, 1).DayOfWeek;
            int offset = Calendar.GetFirstDayOfMonthOffset(firstDay);
            int weeks = (int)Math.Ceiling((days + offset) / 7.0);

            if (template != null)
            {
                template.Parse("CURRENT_MONTH", core.Functions.IntToMonth(month));
                template.Parse("CURRENT_YEAR", year.ToString());
            }
            else
            {
                vc1.Parse("MONTH", core.Functions.IntToMonth(month));
                vc1.Parse("U_MONTH", BuildMonthUri(core, owner, year, month));
            }

            for (int week = 0; week < weeks; week++)
            {
                VariableCollection weekVariableCollection;
                if (template != null)
                {
                    weekVariableCollection = template.CreateChild("week");
                }
                else
                {
                    weekVariableCollection = vc1.CreateChild("week");
                }

                weekVariableCollection.Parse("WEEK", (week + 1).ToString());

                if (week + 1 == 1)
                {
                    int daysPrev = DateTime.DaysInMonth(year - (month - 1) / 12, (month - 1) % 12 + 1);
                    for (int i = offset - 1; i >= 0; i--)
                    {
                        int day = daysPrev - i;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.Parse("DATE", day.ToString());
                        dayVariableCollection.Parse("URI", Calendar.BuildDateUri(core, owner, year - (month - 2) / 12, (month - 2) % 12 + 1, day));
                    }
                    for (int i = offset; i < 7; i++)
                    {
                        int day = i - offset + 1;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.Parse("DATE", day.ToString());
                        dayVariableCollection.Parse("URI", Calendar.BuildDateUri(core, owner, year, month, day));
                    }
                }
                else if (week + 1 == weeks)
                {
                    for (int i = week * 7 - offset; i < days; i++)
                    {
                        int day = i + 1;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.Parse("DATE", day.ToString());
                        dayVariableCollection.Parse("URI", Calendar.BuildDateUri(core, owner, year, month, day));
                    }
                    for (int i = 0; i < weeks * 7 - days - offset; i++)
                    {
                        int day = i + 1;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.Parse("DATE", day.ToString());
                        dayVariableCollection.Parse("URI", Calendar.BuildDateUri(core, owner, year + (month) / 12, (month) % 12 + 1, day));
                    }
                }
                else
                {
                    for (int i = 0; i < 7; i++)
                    {
                        int day = week * 7 + i + 1 - offset;

                        VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
                        dayVariableCollection.Parse("DATE", day.ToString());
                        dayVariableCollection.Parse("URI", Calendar.BuildDateUri(core, owner, year, month, day));
                    }
                }
            }
        }

        private static string BuildDateUri(Core core, Primitive owner, int year, int month, int day)
        {
            return core.Uri.AppendSid(string.Format("/{0}/calendar/{1}/{2}/{3}",
                owner.Key, year, month, day));
        }

        private static string BuildMonthUri(Core core, Primitive owner, int year, int month)
        {
            return core.Uri.AppendSid(string.Format("/{0}/calendar/{1}/{2}",
                owner.Key, year, month));
        }

        private static string BuildYearUri(Core core, Primitive owner, int year)
        {
            return core.Uri.AppendSid(string.Format("/{0}/calendar/{1}",
                owner.Key, year));
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

        public static int YearOfPreviousMonth(int year, int month)
        {
            if (month > 1)
            {
                return year;
            }
            else
            {
                return year - 1;
            }
        }

        public static int PreviousMonth(int month)
        {
            if (month > 1)
            {
                return month - 1;
            }
            else
            {
                return 12;
            }
        }

        public static int YearOfNextMonth(int year, int month)
        {
            if (month < 12)
            {
                return year;
            }
            else
            {
                return year + 1;
            }
        }

        public static int NextMonth(int month)
        {
            if (month < 12)
            {
                return month + 1;
            }
            else
            {
                return 1;
            }
        }

        public static void Show(Core core, TPage page, Primitive owner)
        {
            Show(core, page, owner, core.tz.Now.Year, core.tz.Now.Month);
        }

        public static void Show(Core core, TPage page, Primitive owner, int year)
        {
            page.template.SetTemplate("Calendar", "viewcalendaryear");

            page.template.Parse("CURRENT_YEAR", year.ToString());

            page.template.Parse("U_PREVIOUS_YEAR", Calendar.BuildYearUri(core, owner, year - 1));
            page.template.Parse("U_NEXT_YEAR", Calendar.BuildYearUri(core, owner, year + 1));

            for (int i = 1; i <= 12; i++)
            {
                DisplayMiniCalendar(core, page.template.CreateChild("month"), owner, year, i);
            }
        }

        public static void Show(Core core, TPage page, Primitive owner, int year, int month)
        {
            page.template.SetTemplate("Calendar", "viewcalendarmonth");

            if (month < 1 || month > 12)
            {
                core.Functions.Generate404();
            }

            // 15 year window
            if (year < DateTime.Now.Year - 5 || year > DateTime.Now.Year + 10)
            {
                core.Functions.Generate404();
            }

            page.template.Parse("CURRENT_MONTH", core.Functions.IntToMonth(month));
            page.template.Parse("CURRENT_YEAR", year.ToString());
            page.template.Parse("U_PREVIOUS_MONTH", Calendar.BuildMonthUri(core, owner, YearOfPreviousMonth(year, month), PreviousMonth(month)));
            page.template.Parse("U_NEXT_MONTH", Calendar.BuildMonthUri(core, owner, YearOfNextMonth(year, month), NextMonth(month)));

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.Parse("U_NEW_EVENT", core.Uri.BuildAccountSubModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", year),
                    string.Format("month={0}", month),
                    string.Format("day={0}", ((month == core.tz.Now.Month) ? core.tz.Now.Day : 1))));
            }

            int days = DateTime.DaysInMonth(year, month);
            DayOfWeek firstDay = new DateTime(year, month, 1).DayOfWeek;
            int offset = Calendar.GetFirstDayOfMonthOffset(firstDay);
            int weeks = (int)Math.Ceiling((days + offset) / 7.0);
            int daysPrev = DateTime.DaysInMonth(YearOfPreviousMonth(year, month), PreviousMonth(month));

            long startTime = 0;
            if (offset > 0)
            {
                // the whole month including entry days
                startTime = core.tz.GetUnixTimeStamp(new DateTime(YearOfPreviousMonth(year, month), PreviousMonth(month), daysPrev - offset + 1, 0, 0, 0));
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

            /*if (startTime == -8885289600 || startTime == 11404281600 || endTime == -8885289600 || endTime == 11404281600)
            {
                Functions.Generate404();
                return;
            }*/

            for (int week = 0; week < weeks; week++)
            {
                VariableCollection weekVariableCollection = page.template.CreateChild("week");

                weekVariableCollection.Parse("WEEK", (week + 1).ToString());

                /* lead in week */
                if (week + 1 == 1)
                {
                    int daysPrev2 = DateTime.DaysInMonth(YearOfPreviousMonth(year, month), PreviousMonth(month));
                    /* days in month prior */
                    for (int i = offset - 1; i >= 0; i--)
                    {
                        int day = daysPrev2 - i;

                        Calendar.showDayEvents(core, owner, YearOfPreviousMonth(year, month), PreviousMonth(month), day, weekVariableCollection, events);
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

                        Calendar.showDayEvents(core, owner, YearOfNextMonth(year, month), NextMonth(month), day, weekVariableCollection, events);
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
            calendarPath.Add(new string[] { month.ToString(), core.Functions.IntToMonth(month) });
            owner.ParseBreadCrumbs(calendarPath);
        }

        public static void Show(Core core, TPage page, Primitive owner, int year, int month, int day)
        {
            page.template.SetTemplate("Calendar", "viewcalendarday");

            if (month < 1 || month > 12)
            {
                core.Functions.Generate404();
            }

            if (day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                core.Functions.Generate404();
            }

            page.template.Parse("CURRENT_DAY", day.ToString());
            page.template.Parse("CURRENT_MONTH", core.Functions.IntToMonth(month));
            page.template.Parse("CURRENT_YEAR", year.ToString());

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.Parse("U_NEW_EVENT", core.Uri.BuildAccountSubModuleUri("calendar", "new-event", true,
                    string.Format("year={0}", year),
                    string.Format("month={0}", month),
                    string.Format("day={0}", day)));
            }

            long startTime = core.tz.GetUnixTimeStamp(new DateTime(year, month, day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24;

            Calendar cal = new Calendar(core);
            List<Event> events = cal.GetEvents(core, owner, startTime, endTime);

            bool hasAllDaysEvents = false;

            foreach (Event calendarEvent in events)
            {
                if (calendarEvent.AllDay)
                {
                    hasAllDaysEvents = true;
                    VariableCollection eventVariableCollection = page.template.CreateChild("event");

                    eventVariableCollection.Parse("TITLE", calendarEvent.Subject);
                    eventVariableCollection.Parse("URI", calendarEvent.Uri);
                }
            }

            if (hasAllDaysEvents)
            {
                page.template.Parse("ALL_DAY_EVENTS", "TRUE");
            }

            for (int hour = 0; hour < 24; hour++)
            {
                VariableCollection timeslotVariableCollection = page.template.CreateChild("timeslot");

                DateTime hourTime = new DateTime(year, month, day, hour, 0, 0);

                timeslotVariableCollection.Parse("TIME", hourTime.ToString("h tt").ToLower());

                showHourEvents(core, owner, year, month, day, hour, timeslotVariableCollection, events);
            }

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", "Calendar" });
            calendarPath.Add(new string[] { year.ToString(), year.ToString() });
            calendarPath.Add(new string[] { month.ToString(), core.Functions.IntToMonth(month) });
            calendarPath.Add(new string[] { day.ToString(), day.ToString() });
            owner.ParseBreadCrumbs(calendarPath);
        }

        private static void showDayEvents(Core core, Primitive owner, int year, int month, int day, VariableCollection weekVariableCollection, List<Event> events)
        {
            VariableCollection dayVariableCollection = weekVariableCollection.CreateChild("day");
            dayVariableCollection.Parse("DATE", day.ToString());
            dayVariableCollection.Parse("URI", Calendar.BuildDateUri(core, owner, year, month, day));
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

                eventVariableCollection.Parse("TITLE", calendarEvent.Subject.Substring(0, Math.Min(7, calendarEvent.Subject.Length)));
                if (calendarEvent.GetStartTime(core.tz).Day != day)
                {
                    eventVariableCollection.Parse("START_TIME", calendarEvent.GetStartTime(core.tz).ToString("d MMMM h:mmt").ToLower());
                }
                else
                {
                    eventVariableCollection.Parse("START_TIME", calendarEvent.GetStartTime(core.tz).ToString("h:mmt").ToLower());
                }
                eventVariableCollection.Parse("URI", calendarEvent.Uri);

                if (calendarEvent is BirthdayEvent)
                {
                    BirthdayEvent birthdayEvent = (BirthdayEvent)calendarEvent;

                    eventVariableCollection.Parse("BIRTH_DATE", birthdayEvent.User.Profile.DateOfBirth.ToString("d MMMM"));
                }

                hasEvents = true;

                // if the event ends before the end of the day, finish up the event
                if (calendarEvent.GetEndTime(core.tz).CompareTo(new DateTime(year, month, day, 23, 59, 59)) <= 0)
                {
                    expired.Add(calendarEvent);
                }
            }

            if (hasEvents)
            {
                dayVariableCollection.Parse("EVENTS", "TRUE");
            }

            foreach (Event calendarEvent in expired)
            {
                events.Remove(calendarEvent);
            }
        }

        private static void showHourEvents(Core core, Primitive owner, int year, int month, int day, int hour, VariableCollection timeslotVariableCollection, List<Event> events)
        {
            bool hasEvents = false;

            long startOfDay = core.tz.GetUnixTimeStamp(new DateTime(year, month, day, 0, 0, 0));
            long endOfDay = startOfDay + 60 * 60 * 24;

            List<Event> expired = new List<Event>();
            foreach (Event calendarEvent in events)
            {
                if (calendarEvent.AllDay)
                {
                    continue;
                }

                long startTime = calendarEvent.StartTimeRaw;
                long endTime = calendarEvent.EndTimeRaw;

                if (endTime > endOfDay)
                {
                    endTime = endOfDay;
                }

                if (startTime < startOfDay)
                {
                    startTime = startOfDay;
                }

                long hourTime = core.tz.GetUnixTimeStamp(new DateTime(year, month, day, hour, 59, 59));

                //if (calendarEvent.GetStartTime(core.tz).CompareTo(new DateTime(year, month, day, hour, 59, 59)) > 0)
                if (startTime > hourTime)
                {
                    break;
                }

                VariableCollection eventVariableCollection = timeslotVariableCollection.CreateChild("event");

                long height = (endTime - startTime) * 24 / 60 / 60;

                eventVariableCollection.Parse("TITLE", calendarEvent.Subject);
                eventVariableCollection.Parse("HEIGHT", height.ToString());
                eventVariableCollection.Parse("URI", calendarEvent.Uri);

                hasEvents = true;

                expired.Add(calendarEvent);
            }

            if (hasEvents)
            {
                timeslotVariableCollection.Parse("EVENTS", "TRUE");
            }

            foreach (Event calendarEvent in expired)
            {
                events.Remove(calendarEvent);
            }
        }
    }
}
