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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [AccountSubModule("calendar", "new-task")]
    public class AccountCalendarTaskNew : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "New Task";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        public AccountCalendarTaskNew()
        {
            this.Load += new EventHandler(AccountCalendarTaskNew_Load);
            this.Show += new EventHandler(AccountCalendarTaskNew_Show);
        }

        void AccountCalendarTaskNew_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit", new ModuleModeHandler(AccountCalendarTaskNew_Show));
            AddSaveHandler("edit", new EventHandler(AccountCalendarTaskNew_Save));
        }

        void AccountCalendarTaskNew_Show(object sender, EventArgs e)
        {
            SetTemplate("account_calendar_task_new");

            bool edit = false;
            ushort taskAccess = 0;

            if (core.Http.Query["mode"] == "edit")
            {
                edit = true;
            }

            int year = core.Functions.RequestInt("year", tz.Now.Year);
            int month = core.Functions.RequestInt("month", tz.Now.Month);
            int day = core.Functions.RequestInt("day", tz.Now.Day);

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
                months.Add(i.ToString(), core.Functions.IntToMonth(i));
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
                int id = core.Functions.RequestInt("id", -1);

                if (id < 1)
                {
                    DisplayGenericError();
                }

                try
                {
                    Task calendarTask = new Task(core, Owner, id);

                    template.Parse("EDIT", "TRUE");
                    template.Parse("ID", calendarTask.TaskId.ToString());

                    dueDate = calendarTask.GetDueTime(core.Tz);

                    topic = calendarTask.Topic;
                    description = calendarTask.Description;

                    percentComplete = calendarTask.PercentageComplete;
                    priority = calendarTask.Priority;
                }
                catch
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }
            }

            template.Parse("S_YEAR", year.ToString());
            template.Parse("S_MONTH", month.ToString());
            template.Parse("S_DAY", day.ToString());

            ParseSelectBox("S_DUE_YEAR", "due-year", years, dueDate.Year.ToString());

            ParseSelectBox("S_DUE_MONTH", "due-month", months, dueDate.Month.ToString());

            ParseSelectBox("S_DUE_DAY", "due-day", days, dueDate.Day.ToString());

            ParseSelectBox("S_DUE_HOUR", "due-hour", hours, dueDate.Hour.ToString());

            ParseSelectBox("S_DUE_MINUTE", "due-minute", minutes, dueDate.Minute.ToString());

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");

            //ParsePermissionsBox("S_TASK_PERMS", taskAccess, permissions);

            template.Parse("S_TOPIC", topic);
            template.Parse("S_DESCRIPTION", description);

            ParseSelectBox("S_PERCENT_COMPLETE", "percent-complete", percentages, percentComplete.ToString());
            ParseSelectBox("S_PRIORITY", "priority", priorities, ((byte)priority).ToString());

            Save(new EventHandler(AccountCalendarTaskNew_Save));
        }

        void AccountCalendarTaskNew_Save(object sender, EventArgs e)
        {
            long taskId = 0;
            string topic = "";
            string description = "";
            byte percentComplete = core.Functions.FormByte("percent-complete", 0);
            TaskPriority priority = (TaskPriority)core.Functions.FormByte("priority", (byte)TaskPriority.Normal);
            DateTime dueDate = tz.Now;
            bool edit = false;

            AuthoriseRequestSid();

            if (core.Http.Form["mode"] == "edit")
            {
                edit = true;
            }

            try
            {
                topic = core.Http.Form["topic"];
                description = core.Http.Form["description"];

                dueDate = new DateTime(
                    int.Parse(core.Http.Form["due-year"]),
                    int.Parse(core.Http.Form["due-month"]),
                    int.Parse(core.Http.Form["due-day"]),
                    int.Parse(core.Http.Form["due-hour"]),
                    int.Parse(core.Http.Form["due-minute"]),
                    0);

                if (edit)
                {
                    taskId = long.Parse(core.Http.Form["id"]);
                }
            }
            catch
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
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

                Task calendarTask = Task.Create(core, LoggedInMember, Owner, topic, description, tz.GetUnixTimeStamp(dueDate), status, percentComplete, priority);

                SetRedirectUri(Task.BuildTaskUri(core, calendarTask));
                core.Display.ShowMessage("Task Created", "You have successfully created a new task.");
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
                query.AddField("task_percent_complete", percentComplete);
                query.AddField("task_status", (byte)status);
                query.AddField("task_priority", (byte)priority);
                query.AddCondition("user_id", LoggedInMember.UserId);
                query.AddCondition("task_id", taskId);

                db.Query(query);

                Task calendarTask = new Task(core, Owner, taskId);

                SetRedirectUri(Task.BuildTaskUri(core, calendarTask));
                core.Display.ShowMessage("Task Saved", "You have successfully saved your changes to the task.");
            }
        }
    }
}
