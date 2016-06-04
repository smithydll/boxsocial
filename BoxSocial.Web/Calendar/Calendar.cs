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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.Calendar
{
    [DataTable("calendar")]
    [Permission("VIEW", "Can view your calendar", PermissionTypes.View)]
    [Permission("INVITE_EVENTS", "Can invite people to events", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_EVENTS", "Can create events", PermissionTypes.CreateAndEdit)]
    [Permission("EDIT_EVENTS", "Can edit events", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_TASKS", "Can create tasks", PermissionTypes.CreateAndEdit)]
    [Permission("EDIT_TASKS", "Can edit tasks", PermissionTypes.CreateAndEdit)]
    [Permission("ASSIGN_TASKS", "Can assign tasks to people", PermissionTypes.CreateAndEdit)]
    public class Calendar : NumberedItem, IPermissibleItem
    {
        [DataField("calendar_id", DataFieldKeys.Primary)]
        private long calendarId;
        [DataField("calendar_item", DataFieldKeys.Unique)]
        private ItemKey ownerKey;
        [DataField("calendar_events")]
        private long eventCount;
        [DataField("calendar_tasks")]
        private bool taskCount;
        [DataField("calendar_simple_permissions")]
        private bool simplePermissions;

        private Primitive owner;
        private Access access;

        public override long Id
        {
            get
            {
                return calendarId;
            }
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return ownerKey;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public Calendar(Core core, Primitive owner)
            : base(core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (owner == null)
            {
                throw new InvalidUserException();
            }

            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Calendar_ItemLoad);

            try
            {
                LoadItem("calendar_item_id", "calendar_item_type_id", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidCalendarException();
            }
        }

        public Calendar(Core core, DataRow calendarRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Calendar_ItemLoad);

            loadItemInfo(calendarRow);
        }

        public Calendar(Core core, System.Data.Common.DbDataReader calendarRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Calendar_ItemLoad);

            loadItemInfo(calendarRow);
        }

        public Calendar(Core core, long calendarId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Calendar_ItemLoad);

            try
            {
                LoadItem(calendarId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidCalendarException();
            }
        }

        protected override void loadItemInfo(DataRow calendarRow)
        {
            loadValue(calendarRow, "calendar_id", out calendarId);
            loadValue(calendarRow, "calendar_item", out ownerKey);
            loadValue(calendarRow, "calendar_events", out eventCount);
            loadValue(calendarRow, "calendar_tasks", out taskCount);
            loadValue(calendarRow, "calendar_simple_permissions", out simplePermissions);

            itemLoaded(calendarRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader calendarRow)
        {
            loadValue(calendarRow, "calendar_id", out calendarId);
            loadValue(calendarRow, "calendar_item", out ownerKey);
            loadValue(calendarRow, "calendar_events", out eventCount);
            loadValue(calendarRow, "calendar_tasks", out taskCount);
            loadValue(calendarRow, "calendar_simple_permissions", out simplePermissions);

            itemLoaded(calendarRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        /// <summary>
        /// ItemLoad event
        /// </summary>
        private void Calendar_ItemLoad()
        {
        }

        /// <summary>
        /// Creates a new blog for the logged in user.
        /// </summary>
        /// <param name="core"></param>
        /// <returns></returns>
        public static Calendar Create(Core core, Primitive owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }


            InsertQuery iQuery = new InsertQuery(GetTable(typeof(Calendar)));
            iQuery.AddField("calendar_item_id", owner.Id);
            iQuery.AddField("calendar_item_type_id", owner.TypeId);
            iQuery.AddField("calendar_simple_permissions", true);

            long calendarId = core.Db.Query(iQuery);

            Calendar newCalendar = new Calendar(core, owner);

            if (owner is User)
            {
                Access.CreateAllGrantsForOwner(core, newCalendar);
                newCalendar.Access.CreateGrantForPrimitive(Friend.GetFriendsGroupKey(core), "VIEW");
            }
            if (owner is UserGroup)
            {
                newCalendar.Access.CreateGrantForPrimitive(UserGroup.GetGroupOperatorsGroupKey(core), "VIEW", "CREATE_EVENTS", "CREATE_TASKS", "ASSIGN_TASKS", "EDIT_EVENTS", "EDIT_TASKS");
                newCalendar.Access.CreateGrantForPrimitive(UserGroup.GetGroupOfficersGroupKey(core), "VIEW", "CREATE_EVENTS", "CREATE_TASKS", "ASSIGN_TASKS", "EDIT_EVENTS", "EDIT_TASKS");
                newCalendar.Access.CreateGrantForPrimitive(User.GetEveryoneGroupKey(core), "VIEW");
                newCalendar.Access.CreateGrantForPrimitive(User.GetCreatorKey(core), "EDIT_EVENTS", "EDIT_TASKS");
            }

            return newCalendar;
        }

        public List<Event> GetEvents(Core core, Primitive owner, long startTimeRaw, long endTimeRaw)
        {
            List<Event> events = new List<Event>();

            SelectQuery sQuery = Item.GetSelectQueryStub(core, typeof(Event));
            sQuery.AddCondition("event_item_id", owner.Id);
            sQuery.AddCondition("event_item_type_id", owner.TypeId);
            QueryCondition sqc2 = sQuery.AddCondition("event_time_start_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
            sqc2.AddCondition("event_time_start_ut", ConditionEquality.LessThanEqual, endTimeRaw);
            QueryCondition sqc3 = sqc2.AddCondition(ConditionRelations.Or, "event_time_end_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
            sqc3.AddCondition("event_time_end_ut", ConditionEquality.LessThanEqual, endTimeRaw);
            QueryCondition sqc4 = sqc3.AddCondition(ConditionRelations.Or, "event_time_start_ut", ConditionEquality.LessThan, startTimeRaw);
            sqc4.AddCondition("event_time_end_ut", ConditionEquality.GreaterThan, endTimeRaw);
            sQuery.AddSort(SortOrder.Ascending, "event_time_start_ut");

            {
                System.Data.Common.DbDataReader eventsReader = db.ReaderQuery(sQuery);

                while (eventsReader.Read())
                {
                    events.Add(new Event(core, eventsReader));
                }

                eventsReader.Close();
                eventsReader.Dispose();
            }

            if (owner.TypeId == ItemKey.GetTypeId(core, typeof(User)))
            {
                // now select events invited to
                SelectQuery query = Event.GetSelectQueryStub(core, typeof(Event));
                query.AddFields("event_invites.item_id", "event_invites.item_type_id", "event_invites.inviter_id", "event_invites.event_id");
                query.AddJoin(JoinTypes.Left, new DataField(typeof(Event), "event_id"), new DataField("event_invites", "event_id"));
                query.AddCondition("item_id", core.LoggedInMemberId);
                query.AddCondition("item_type_id", ItemKey.GetTypeId(core, typeof(User)));
                QueryCondition qc2 = query.AddCondition("event_time_start_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
                qc2.AddCondition("event_time_start_ut", ConditionEquality.LessThanEqual, endTimeRaw);
                QueryCondition qc3 = qc2.AddCondition(ConditionRelations.Or, "event_time_end_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
                qc3.AddCondition("event_time_end_ut", ConditionEquality.LessThanEqual, endTimeRaw);
                QueryCondition qc4 = qc3.AddCondition(ConditionRelations.Or, "event_time_start_ut", ConditionEquality.LessThan, startTimeRaw);
                qc4.AddCondition("event_time_end_ut", ConditionEquality.GreaterThan, endTimeRaw);

                System.Data.Common.DbDataReader eventsReader = db.ReaderQuery(sQuery);

                while (eventsReader.Read())
                {
                    events.Add(new Event(core, eventsReader));
                }

                eventsReader.Close();
                eventsReader.Dispose();

                User user = (User)owner;
                List<UserRelation> friends = user.GetFriendsBirthdays(startTimeRaw, endTimeRaw);

                foreach (UserRelation friend in friends)
                {
                    try
                    {
                        events.Add(new BirthdayEvent(core, user, friend, core.Tz.DateTimeFromMysql(startTimeRaw).Year));
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

            SelectQuery query = Item.GetSelectQueryStub(core, typeof(Task));
            query.AddCondition("task_item_id", owner.Id);
            query.AddCondition("task_item_type_id", owner.TypeId);
            QueryCondition qc1 = query.AddCondition("task_due_date_ut", ConditionEquality.GreaterThanEqual, startTimeRaw);
            qc1.AddCondition("task_due_date_ut", ConditionEquality.LessThanEqual, endTimeRaw);
            query.AddSort(SortOrder.Ascending, "task_due_date_ut");
			
            if (overdueTasks)
            {
                QueryCondition qc2 = qc1.AddCondition(ConditionRelations.Or, "task_due_date_ut", ConditionEquality.LessThan, UnixTime.UnixTimeStamp());
                qc2.AddCondition("task_percent_complete", ConditionEquality.LessThan, 100);

                System.Data.Common.DbDataReader tasksReader = db.ReaderQuery(query);

                while (tasksReader.Read())
                {
                    tasks.Add(new Task(core, owner, tasksReader));
                }

                tasksReader.Close();
                tasksReader.Dispose();
            }
            else
            {
                DataTable tasksTable = db.Query(query);

                System.Data.Common.DbDataReader tasksReader = db.ReaderQuery(query);

                while (tasksReader.Read())
                {
                    tasks.Add(new Task(core, owner, tasksReader));
                }

                tasksReader.Close();
                tasksReader.Dispose();
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

        internal static string BuildDateUri(Core core, Primitive owner, int year, int month, int day)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}calendar/{1}/{2}/{3}",
                owner.UriStub, year, month, day));
        }

        internal static string BuildMonthUri(Core core, Primitive owner, int year, int month)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}calendar/{1}/{2}",
                owner.UriStub, year, month));
        }

        internal static string BuildYearUri(Core core, Primitive owner, int year)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}calendar/{1}",
                owner.UriStub, year));
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
            Show(core, page, owner, core.Tz.Now.Year, core.Tz.Now.Month);
        }

        public static void Show(Core core, TPage page, Primitive owner, int year)
        {
            core.Template.SetTemplate("Calendar", "viewcalendaryear");

            // 15 year window
            if (year < DateTime.Now.Year - 10 || year > DateTime.Now.Year + 5)
            {
                core.Functions.Generate404();
            }

            /* pages */
            core.Display.ParsePageList(owner, true);

            core.Template.Parse("PAGE_TITLE", year.ToString());

            core.Template.Parse("CURRENT_YEAR", year.ToString());

            if (year - 1 >= DateTime.Now.Year - 10)
            {
                core.Template.Parse("U_PREVIOUS_YEAR", Calendar.BuildYearUri(core, owner, year - 1));
            }
            if (year + 1 <= DateTime.Now.Year + 5)
            {
                core.Template.Parse("U_NEXT_YEAR", Calendar.BuildYearUri(core, owner, year + 1));
            }

            for (int i = 1; i <= 12; i++)
            {
                DisplayMiniCalendar(core, core.Template.CreateChild("month"), owner, year, i);
            }

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", core.Prose.GetString("CALENDAR") });
            calendarPath.Add(new string[] { year.ToString(), year.ToString() });
            owner.ParseBreadCrumbs(calendarPath);
        }

        public static void Show(Core core, TPage page, Primitive owner, int year, int month)
        {
            core.Template.SetTemplate("Calendar", "viewcalendarmonth");

            if (month < 1 || month > 12)
            {
                core.Functions.Generate404();
            }

            // 15 year window
            if (year < DateTime.Now.Year - 10 || year > DateTime.Now.Year + 5)
            {
                core.Functions.Generate404();
            }

            /* pages */
            core.Display.ParsePageList(owner, true);

            core.Template.Parse("PAGE_TITLE", core.Functions.IntToMonth(month) + " " + year.ToString());

            core.Template.Parse("CURRENT_MONTH", core.Functions.IntToMonth(month));
            core.Template.Parse("CURRENT_YEAR", year.ToString());

            core.Template.Parse("U_PREVIOUS_MONTH", Calendar.BuildMonthUri(core, owner, YearOfPreviousMonth(year, month), PreviousMonth(month)));
            core.Template.Parse("U_NEXT_MONTH", Calendar.BuildMonthUri(core, owner, YearOfNextMonth(year, month), NextMonth(month)));

            Calendar cal = null;
            try
            {
                cal = new Calendar(core, owner);
            }
            catch (InvalidCalendarException)
            {
                cal = Calendar.Create(core, owner);
            }

            if (cal.Access.Can("CREATE_EVENTS"))
            {
                core.Template.Parse("U_NEW_EVENT", core.Hyperlink.BuildAccountSubModuleUri(owner, "calendar", "new-event", true,
                    string.Format("year={0}", year),
                    string.Format("month={0}", month),
                    string.Format("day={0}", ((month == core.Tz.Now.Month) ? core.Tz.Now.Day : 1))));
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
                startTime = core.Tz.GetUnixTimeStamp(new DateTime(YearOfPreviousMonth(year, month), PreviousMonth(month), daysPrev - offset + 1, 0, 0, 0));
            }
            else
            {
                // the whole month
                startTime = core.Tz.GetUnixTimeStamp(new DateTime(year, month, 1, 0, 0, 0));
            }

            // the whole month including exit days
            long endTime = startTime + 60 * 60 * 24 * weeks * 7;

            List<Event> events = cal.GetEvents(core, owner, startTime, endTime);

            /*if (startTime == -8885289600 || startTime == 11404281600 || endTime == -8885289600 || endTime == 11404281600)
            {
                Functions.Generate404();
                return;
            }*/

            for (int week = 0; week < weeks; week++)
            {
                VariableCollection weekVariableCollection = core.Template.CreateChild("week");

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
            calendarPath.Add(new string[] { "calendar", core.Prose.GetString("CALENDAR") });
            calendarPath.Add(new string[] { year.ToString(), year.ToString() });
            calendarPath.Add(new string[] { month.ToString(), core.Functions.IntToMonth(month) });
            owner.ParseBreadCrumbs(calendarPath);
        }

        public static void Show(Core core, TPage page, Primitive owner, int year, int month, int day)
        {
            core.Template.SetTemplate("Calendar", "viewcalendarday");

            // 15 year window
            if (year < DateTime.Now.Year - 10 || year > DateTime.Now.Year + 5)
            {
                core.Functions.Generate404();
            }

            if (month < 1 || month > 12)
            {
                core.Functions.Generate404();
            }

            if (day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                core.Functions.Generate404();
            }

            /* pages */
            core.Display.ParsePageList(owner, true);

            core.Template.Parse("PAGE_TITLE", day.ToString() + " " + core.Functions.IntToMonth(month) + " " + year.ToString());

            core.Template.Parse("CURRENT_DAY", day.ToString());
            core.Template.Parse("CURRENT_MONTH", core.Functions.IntToMonth(month));
            core.Template.Parse("CURRENT_YEAR", year.ToString());

            long startTime = core.Tz.GetUnixTimeStamp(new DateTime(year, month, day, 0, 0, 0));
            long endTime = startTime + 60 * 60 * 24;

            Calendar cal = null;
            try
            {
                cal = new Calendar(core, owner);
            }
            catch (InvalidCalendarException)
            {
                cal = Calendar.Create(core, owner);
            }

            if (cal.Access.Can("CREATE_EVENTS"))
            {
                core.Template.Parse("U_NEW_EVENT", core.Hyperlink.BuildAccountSubModuleUri(owner, "calendar", "new-event", true,
                    string.Format("year={0}", year),
                    string.Format("month={0}", month),
                    string.Format("day={0}", day)));
            }

            List<Event> events = cal.GetEvents(core, owner, startTime, endTime);

            bool hasAllDaysEvents = false;

            foreach (Event calendarEvent in events)
            {
                if (calendarEvent.AllDay)
                {
                    hasAllDaysEvents = true;
                    VariableCollection eventVariableCollection = core.Template.CreateChild("event");

                    eventVariableCollection.Parse("TITLE", calendarEvent.Subject);
                    eventVariableCollection.Parse("URI", calendarEvent.Uri);
                }
            }

            if (hasAllDaysEvents)
            {
                core.Template.Parse("ALL_DAY_EVENTS", "TRUE");
            }

            VariableCollection[] hours = new VariableCollection[24];

            for (int hour = 0; hour < 24; hour++)
            {
                VariableCollection timeslotVariableCollection = core.Template.CreateChild("timeslot");

                DateTime hourTime = new DateTime(year, month, day, hour, 0, 0);

                timeslotVariableCollection.Parse("TIME", hourTime.ToString("h tt").ToLower());
                hours[hour] = timeslotVariableCollection;
            }

            showHourEvents(core, owner, year, month, day, hours, events);

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", core.Prose.GetString("CALENDAR") });
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

            DateTime now = core.Tz.Now;
            if (year == now.Year && month == now.Month && day == now.Day)
            {
                dayVariableCollection.Parse("CLASS", "today");
            }

            bool hasEvents = false;

            List<Event> expired = new List<Event>();
            foreach (Event calendarEvent in events)
            {
                // if the event starts after the end of the day, skip this day
                if (calendarEvent.GetStartTime(core.Tz).CompareTo(new DateTime(year, month, day, 23, 59, 59)) > 0)
                {
                    break;
                }

                VariableCollection eventVariableCollection = dayVariableCollection.CreateChild("event");

                eventVariableCollection.Parse("TITLE", calendarEvent.Subject);
                if (calendarEvent.GetStartTime(core.Tz).Day != day)
                {
                    eventVariableCollection.Parse("START_TIME", calendarEvent.GetStartTime(core.Tz).ToString("d MMMM h:mmt").ToLower());
                }
                else
                {
                    eventVariableCollection.Parse("START_TIME", calendarEvent.GetStartTime(core.Tz).ToString("h:mmt").ToLower());
                }
                eventVariableCollection.Parse("URI", calendarEvent.Uri);

                if (calendarEvent is BirthdayEvent)
                {
                    BirthdayEvent birthdayEvent = (BirthdayEvent)calendarEvent;

                    eventVariableCollection.Parse("BIRTH_DATE", birthdayEvent.User.Profile.DateOfBirth.Day + " " + core.Tz.MonthToString(birthdayEvent.User.Profile.DateOfBirth.Month));
                }

                hasEvents = true;

                // if the event ends before the end of the day, finish up the event
                if (calendarEvent.GetEndTime(core.Tz).CompareTo(new DateTime(year, month, day, 23, 59, 59)) <= 0)
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

        private static void showHourEvents(Core core, Primitive owner, int year, int month, int day, VariableCollection[] timeslotVariableCollections, List<Event> events)
        {
            bool hasEvents = false;

            long startOfDay = core.Tz.GetUnixTimeStamp(new DateTime(year, month, day, 0, 0, 0));
            long endOfDay = startOfDay + 60 * 60 * 24;

            long[] heights = new long[events.Count];
            long[] tops = new long[events.Count];
            double[] widths = new double[events.Count];
            double[] lefts = new double[events.Count];
            int[] eventCount = new int[96];
            int[] eventNumber = new int[96];

            int hourHeight = 32;

            List<Event> expired = new List<Event>();
            int i = 0;
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

                DateTime startDateTime = core.Tz.DateTimeFromMysql(startTime);
                DateTime endDateTime = core.Tz.DateTimeFromMysql(endTime - 1);
                long startMinute = startDateTime.Minute;
                long startHour = startDateTime.Hour;
                long endMinute = endDateTime.Minute;
                long endHour = endDateTime.Hour;
                int startFifteen = (int)Math.Floor(startHour * 4.0 + startMinute / 15.0);
                int endFifteen = (int)Math.Floor(endHour * 4.0 + endMinute / 15.0);

                for (int j = startFifteen; j <= endFifteen; j++)
                {
                    eventCount[j]++;
                }


                heights[i] = (endTime - startTime) * hourHeight / 60 / 60;
                tops[i] = startMinute * 36 / 60;
                widths[i] = 100.0;
                lefts[i] = 100 - 100.0 / Math.Max(1, eventCount[startFifteen]);

                i++;
            }

            i = 0;
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

                DateTime startDateTime = core.Tz.DateTimeFromMysql(startTime);
                DateTime endDateTime = core.Tz.DateTimeFromMysql(endTime - 1);
                long startMinute = startDateTime.Minute;
                long startHour = startDateTime.Hour;
                long endMinute = endDateTime.Minute;
                long endHour = endDateTime.Hour;
                int startFifteen = (int)Math.Floor(startHour * 4.0 + startMinute / 15.0);
                int endFifteen = (int)Math.Floor(endHour * 4.0 + endMinute / 15.0);

                int maxEventsOnFifteen = 0;
                for (int j = startFifteen; j <= endFifteen; j++)
                {
                    eventNumber[j]++;
                    if (eventCount[j] > maxEventsOnFifteen)
                    {
                        maxEventsOnFifteen = eventCount[j];
                    }
                }

                lefts[i] = 100 - 100.0 * eventNumber[startFifteen] / Math.Max(1, maxEventsOnFifteen);

                widths[i] /= Math.Max(1, maxEventsOnFifteen);

                VariableCollection eventVariableCollection = timeslotVariableCollections[startHour].CreateChild("event");

                eventVariableCollection.Parse("TITLE", calendarEvent.Subject);
                eventVariableCollection.Parse("URI", calendarEvent.Uri);

                eventVariableCollection.Parse("LEFT", lefts[i].ToString());
                eventVariableCollection.Parse("WIDTH", widths[i].ToString());
                eventVariableCollection.Parse("TOP", tops[i].ToString());
                eventVariableCollection.Parse("HEIGHT", heights[i].ToString());

                timeslotVariableCollections[startHour].Parse("EVENTS", "TRUE");

                expired.Add(calendarEvent);
                i++;
            }

            foreach (Event calendarEvent in expired)
            {
                events.Remove(calendarEvent);
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }


        public List<AccessControlPermission> AclPermissions
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ownerKey;
            }
        }

        public string DisplayTitle
        {
            get
            {
                return "Calendar: " + Owner.DisplayName + " (" + Owner.Key + ")";
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidCalendarException : Exception
    {
    }
}
