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
    [AccountModule("calendar")]
    public class AccountCalendar : AccountModule
    {
        public AccountCalendar(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(ManageCalendar);
            RegisterSubModule += new RegisterSubModuleHandler(NewEvent);
            RegisterSubModule += new RegisterSubModuleHandler(NewTask);
            RegisterSubModule += new RegisterSubModuleHandler(MarkTaskComplete);
            RegisterSubModule += new RegisterSubModuleHandler(EventInvite);
            RegisterSubModule += new RegisterSubModuleHandler(DeleteEvent);
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

        /*public override string Key
        {
            get
            {
                return "calendar";
            }
        }*/

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

            template.SetTemplate("Calendar", "account_calendar_manage");
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

            template.SetTemplate("Calendar", "account_calendar_event_new");

            int year = Functions.RequestInt("year", tz.Now.Year);
            int month = Functions.RequestInt("month", tz.Now.Month);
            int day = Functions.RequestInt("day", tz.Now.Day);

            string inviteeIdList = Request.Form["inviteeses"];
            string inviteeUsernameList = Request.Form["invitees"];
            List<long> inviteeIds = new List<long>();

            if (!(string.IsNullOrEmpty(inviteeIdList)))
            {
                string[] inviteesIds = inviteeIdList.Split(new char[] { ' ', '\t', ';', ',' });
                foreach (string inviteeId in inviteesIds)
                {
                    try
                    {
                        inviteeIds.Add(long.Parse(inviteeId));
                    }
                    catch
                    {
                    }
                }
            }

            if (!(string.IsNullOrEmpty(inviteeUsernameList)))
            {
                string[] inviteesUsernames = inviteeUsernameList.Split(new char[] { ' ', '\t', ';', ',' });
                foreach (string inviteeUsername in inviteesUsernames)
                {
                    if (!string.IsNullOrEmpty(inviteeUsername))
                    {
                        try
                        {
                            /* TODO: increase performance by creating LoadUserProfiles(List<string>); */
                            long inviteeId = core.LoadUserProfile(inviteeUsername);
                            inviteeIds.Add(inviteeId);
                        }
                        catch (InvalidUserException)
                        {
                        }
                    }
                }
            }

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
                    Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }

                try
                {
                    Event calendarEvent = new Event(core, loggedInMember, id);
                    inviteeIds.AddRange(calendarEvent.GetInvitees());

                    template.ParseVariables("EDIT", "TRUE");
                    template.ParseVariables("ID", HttpUtility.HtmlEncode(calendarEvent.EventId.ToString()));

                    startDate = calendarEvent.GetStartTime(core.tz);
                    endDate = calendarEvent.GetEndTime(core.tz);

                    eventAccess = calendarEvent.Permissions;

                    subject = calendarEvent.Subject;
                    location = calendarEvent.Location;
                    description = calendarEvent.Description;
                }
                catch (InvalidEventException)
                {
                    Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
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

            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));

            StringBuilder outputInvitees = null;
            StringBuilder outputInviteesIds = null;

            core.LoadUserProfiles(inviteeIds);

            foreach (long inviteeId in inviteeIds)
            {
                if (outputInvitees == null)
                {
                    outputInvitees = new StringBuilder();
                    outputInviteesIds = new StringBuilder();
                }
                else
                {
                    outputInvitees.Append("; ");
                    outputInviteesIds.Append(",");
                }

                outputInvitees.Append(core.UserProfiles[inviteeId].DisplayName);
                outputInviteesIds.Append(inviteeId.ToString());
            }

            if (outputInvitees != null)
            {
                template.ParseVariables("S_INVITEES", HttpUtility.HtmlEncode(outputInvitees.ToString()));
            }

            if (outputInviteesIds != null)
            {
                template.ParseVariables("INVITEES_ID_LIST", HttpUtility.HtmlEncode(outputInviteesIds.ToString()));
            }
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
                Display.ShowMessage("Unauthorised", "You are unauthorised to do this action.");
                return;
            }

            if (Request.Form["mode"] == "edit")
            {
                edit = true;
            }

            string inviteeIdList = Request.Form["inviteeses"];
            string inviteeUsernameList = Request.Form["invitees"];
            List<long> inviteeIds = new List<long>();

            if (!(string.IsNullOrEmpty(inviteeIdList)))
            {
                string[] inviteesIds = inviteeIdList.Split(new char[] { ' ', '\t', ';', ',' });
                foreach (string inviteeId in inviteesIds)
                {
                    try
                    {
                        inviteeIds.Add(long.Parse(inviteeId));
                    }
                    catch
                    {
                    }
                }
            }

            if (!(string.IsNullOrEmpty(inviteeUsernameList)))
            {
                string[] inviteesUsernames = inviteeUsernameList.Split(new char[] { ' ', '\t', ';', ',' });
                List<string> inviteesUsernamesList = new List<string>();

                foreach (string inviteeUsername in inviteesUsernames)
                {
                    if (!string.IsNullOrEmpty(inviteeUsername))
                    {
                        inviteesUsernamesList.Add(inviteeUsername);
                    }
                }

                inviteeIds.AddRange(core.LoadUserProfiles(inviteesUsernamesList));
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
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }


            if (!edit)
            {
                Event calendarEvent = Event.Create(core, loggedInMember, loggedInMember, subject, location, description, tz.GetUnixTimeStamp(startTime), tz.GetUnixTimeStamp(endTime), Functions.GetPermission());

                foreach (long inviteeId in inviteeIds)
                {
                    calendarEvent.Invite(core, core.UserProfiles[inviteeId]);
                }

                SetRedirectUri(Event.BuildEventUri(calendarEvent));
                Display.ShowMessage("Event Created", "You have successfully created a new event.");
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE events SET event_subject = '{2}', event_location = '{3}', event_description = '{4}', event_time_start_ut = {5}, event_time_end_ut = {6}, event_access = {7} WHERE user_id = {0} AND event_id = {1};",
                    loggedInMember.UserId, eventId, Mysql.Escape(subject), Mysql.Escape(location), Mysql.Escape(description), tz.GetUnixTimeStamp(startTime), tz.GetUnixTimeStamp(endTime), Functions.GetPermission()));

                Event calendarEvent = new Event(core, loggedInMember, eventId);

                core.LoadUserProfiles(inviteeIds);

                foreach (long inviteeId in inviteeIds)
                {
                    calendarEvent.Invite(core, core.UserProfiles[inviteeId]);
                }

                SetRedirectUri(Event.BuildEventUri(calendarEvent));
                Display.ShowMessage("Event Saved", "You have successfully saved your changes to the event.");
            }
        }

        private void DeleteEvent(string submodule)
        {
            if (submodule != "delete-event") return;

            AuthoriseRequestSid();

            long eventId = Functions.RequestLong("id", 0);

            if (Display.GetConfirmBoxResult() != ConfirmBoxResult.None)
            {
                SaveDeleteEvent();
            }
            else
            {
                if (eventId > 0)
                {
                    Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
                    hiddenFieldList.Add("module", "calendar");
                    hiddenFieldList.Add("sub", "delete-event");
                    hiddenFieldList.Add("id", eventId.ToString());

                    Display.ShowConfirmBox(HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)), "Do you want to delete this event?", "Are you sure you want to delete this event, you cannot undo this delete operation.", hiddenFieldList);
                }
                else
                {
                    Display.ShowMessage("Invalid", "You have specified an invalid event to delete.");
                }
            }
        }

        private void SaveDeleteEvent()
        {
            AuthoriseRequestSid();

            long eventId = Functions.FormLong("id", 0);

            if (Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                if (eventId > 0)
                {
                    Event calendarEvent = new Event(core, null, eventId);

                    try
                    {
                        calendarEvent.Delete(core);

                        SetRedirectUri(BuildModuleUri("calendar"));
                        Display.ShowMessage("Event Deleted", "You have deleted an event from your calendar.");
                    }
                    catch (NotLoggedInException)
                    {
                        Display.ShowMessage("Unauthorised", "You are unauthorised to delete this event.");
                    }
                }
                else
                {
                    Display.ShowMessage("Invalid", "You have specified an invalid event to delete.");
                }
            }
        }

        private void NewTask(string submodule)
        {
            subModules.Add("new-task", "New Task");
            if (submodule != "new-task") return;

            if (Request.Form["save"] != null)
            {
                SaveNewTask();
            }

            bool edit = false;
            ushort taskAccess = 0;

            if (Request.QueryString["mode"] == "edit")
            {
                edit = true;
            }

            template.SetTemplate("Calendar", "account_calendar_task_new");

            int year = Functions.RequestInt("year", tz.Now.Year);
            int month = Functions.RequestInt("month", tz.Now.Month);
            int day = Functions.RequestInt("day", tz.Now.Day);

            byte percentComplete = 0;
            TaskPriority priority = TaskPriority.Normal;

            DateTime dueDate = new DateTime(year, month, day, 16, 0, 0);

            string topic = "";
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

            Dictionary<string, string> percentages = new Dictionary<string, string>();
            for (int i = 0; i <= 100; i += 25)
            {
                percentages.Add(i.ToString(), i.ToString() + "%");
            }

            Dictionary<string, string> priorities = new Dictionary<string, string>();
            priorities.Add(((byte)TaskPriority.Low).ToString(), "Low");
            priorities.Add(((byte)TaskPriority.Normal).ToString(), "Normal");
            priorities.Add(((byte)TaskPriority.High).ToString(), "High");

            if (edit)
            {
                int id = Functions.RequestInt("id", -1);

                if (id < 1)
                {
                    Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }

                try
                {
                    Task calendarTask = new Task(core, loggedInMember, id);

                    template.ParseVariables("EDIT", "TRUE");
                    template.ParseVariables("ID", HttpUtility.HtmlEncode(calendarTask.TaskId.ToString()));

                    dueDate = calendarTask.GetDueTime(core.tz);

                    taskAccess = calendarTask.Permissions;

                    topic = calendarTask.Topic;
                    description = calendarTask.Description;

                    percentComplete = calendarTask.PercentageComplete;
                    priority = calendarTask.Priority;
                }
                catch
                {
                    Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }
            }

            template.ParseVariables("S_YEAR", HttpUtility.HtmlEncode(year.ToString()));
            template.ParseVariables("S_MONTH", HttpUtility.HtmlEncode(month.ToString()));
            template.ParseVariables("S_DAY", HttpUtility.HtmlEncode(day.ToString()));

            template.ParseVariables("S_DUE_YEAR", Functions.BuildSelectBox("due-year", years, dueDate.Year.ToString()));

            template.ParseVariables("S_DUE_MONTH", Functions.BuildSelectBox("due-month", months, dueDate.Month.ToString()));

            template.ParseVariables("S_DUE_DAY", Functions.BuildSelectBox("due-day", days, dueDate.Day.ToString()));

            template.ParseVariables("S_DUE_HOUR", Functions.BuildSelectBox("due-hour", hours, dueDate.Hour.ToString()));

            template.ParseVariables("S_DUE_MINUTE", Functions.BuildSelectBox("due-minute", minutes, dueDate.Minute.ToString()));

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            template.ParseVariables("S_TASK_PERMS", Functions.BuildPermissionsBox(taskAccess, permissions));

            template.ParseVariables("S_TOPIC", HttpUtility.HtmlEncode(topic));
            template.ParseVariables("S_DESCRIPTION", HttpUtility.HtmlEncode(description));

            template.ParseVariables("S_PERCENT_COMPLETE", Functions.BuildSelectBox("percent-complete", percentages, percentComplete.ToString()));
            template.ParseVariables("S_PRIORITY", Functions.BuildSelectBox("priority", priorities, ((byte)priority).ToString()));

            template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
        }

        private void SaveNewTask()
        {
            long taskId = 0;
            string topic = "";
            string description = "";
            byte percentComplete = Functions.FormByte("percent-complete", 0);
            TaskPriority priority = (TaskPriority)Functions.FormByte("priority", (byte)TaskPriority.Normal);
            DateTime dueDate = tz.Now;
            bool edit = false;

            AuthoriseRequestSid();

            if (Request.Form["mode"] == "edit")
            {
                edit = true;
            }

            try
            {
                topic = Request.Form["topic"];
                description = Request.Form["description"];

                dueDate = new DateTime(
                    int.Parse(Request.Form["due-year"]),
                    int.Parse(Request.Form["due-month"]),
                    int.Parse(Request.Form["due-day"]),
                    int.Parse(Request.Form["due-hour"]),
                    int.Parse(Request.Form["due-minute"]),
                    0);

                if (edit)
                {
                    taskId = long.Parse(Request.Form["id"]);
                }
            }
            catch
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }

            if (description == null)
            {
                description = "";
            }


            if (!edit)
            {
                TaskStatus status = TaskStatus.Future;

                if (percentComplete == 100)
                {
                    status = TaskStatus.Completed;
                }

                Task calendarTask = Task.Create(core, loggedInMember, loggedInMember, topic, description, tz.GetUnixTimeStamp(dueDate), Functions.GetPermission(), status, percentComplete, priority);

                SetRedirectUri(Task.BuildTaskUri(calendarTask));
                Display.ShowMessage("Task Created", "You have successfully created a new task.");
            }
            else
            {
                TaskStatus status = TaskStatus.Future;

                if (percentComplete == 100)
                {
                    status = TaskStatus.Completed;
                }

                UpdateQuery query = new UpdateQuery("tasks");
                query.AddField("task_topic", topic);
                query.AddField("task_description", description);
                query.AddField("task_due_date_ut", tz.GetUnixTimeStamp(dueDate));
                query.AddField("task_access", Functions.GetPermission());
                query.AddField("task_percent_complete", percentComplete);
                query.AddField("task_status", (byte)status);
                query.AddField("task_priority", (byte)priority);
                query.AddCondition("user_id", loggedInMember.UserId);
                query.AddCondition("task_id", taskId);

                db.Query(query);

                Task calendarTask = new Task(core, loggedInMember, taskId);

                SetRedirectUri(Task.BuildTaskUri(calendarTask));
                Display.ShowMessage("Task Saved", "You have successfully saved your changes to the task.");
            }
        }

        private void MarkTaskComplete(string submodule)
        {
            if (submodule != "task-complete") return;

            long taskId = Functions.FormLong("id", 0);
            bool isAjax = false;

            if (Request["ajax"] == "true")
            {
                isAjax = true;
            }

            AuthoriseRequestSid();

            try
            {
                Task task = new Task(core, session.LoggedInMember, taskId);

                UpdateQuery query = new UpdateQuery("tasks");
                query.AddField("task_status", (byte)TaskStatus.Completed);
                query.AddField("task_percent_complete", 100);
                query.AddField("task_time_completed_ut", UnixTime.UnixTimeStamp());
                query.AddCondition("user_id", core.LoggedInMemberId);
                query.AddCondition("task_id", taskId);

                if (db.Query(query) == 1)
                {
                    if (!isAjax)
                    {
                        SetRedirectUri(Task.BuildTaskUri(task));
                    }
                    Ajax.ShowMessage(isAjax, "success", "Task Complete", "The task has been marked as complete.");
                }
            }
            catch (InvalidTaskException)
            {
                Ajax.ShowMessage(isAjax, "error", "Error", "An error occured while marking the task as complete, go back");
            }
        }

        private void EventInvite(string submodule)
        {
            if (submodule != "invite-event") return;

            AuthoriseRequestSid();

            long eventId = Functions.RequestLong("id", 0);

            if (eventId > 0)
            {
                Event calendarEvent = new Event(core, null, eventId);
                UpdateQuery uQuery = new UpdateQuery("event_invites");
                
                UpdateQuery uEventQuery = new UpdateQuery("events");
                uEventQuery.AddCondition("event_id", calendarEvent.EventId);

                switch (Request["mode"])
                {
                    case "accept":
                        uQuery.AddField("invite_accepted", true);
                        uQuery.AddField("invite_status", (byte)EventAttendance.Yes);

                        uEventQuery.AddField("event_attendees", new QueryOperation("event_attendees", QueryOperations.Addition, 1));
                        break;
                    case "reject":
                        uQuery.AddField("invite_accepted", false);
                        uQuery.AddField("invite_status", (byte)EventAttendance.No);
                        break;
                    case "maybe":
                        uQuery.AddField("invite_accepted", false);
                        uQuery.AddField("invite_status", (byte)EventAttendance.Maybe);
                        break;
                    default:
                        Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                        return;
                }

                uQuery.AddCondition("event_id", eventId);
                uQuery.AddCondition("item_id", loggedInMember.Id);
                uQuery.AddCondition("item_type", "USER");

                db.BeginTransaction();
                db.Query(uQuery);
                db.Query(uEventQuery);

                SetRedirectUri(calendarEvent.Uri);
                Display.ShowMessage("Invitation Accepted", "You have accepted the invitation to this event.");
                return;
            }
            else
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }
        }
    }
}
