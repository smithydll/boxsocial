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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network, "calendar", "new-task")]
    public class AccountCalendarTaskNew : AccountSubModule
    {
        Calendar calendar;

        public override string Title
        {
            get
            {
                return core.Prose.GetString("NEW_TASK");
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountCalendarTaskNew class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCalendarTaskNew(Core core, Primitive owner)
            : base(core, owner)
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

            if (core.Http.Query["mode"] == "edit")
            {
                edit = true;
            }

            DateTimePicker dueDateTimePicker = new DateTimePicker(core, "due-date");
            dueDateTimePicker.ShowTime = true;
            dueDateTimePicker.ShowSeconds = false;

            int year = core.Functions.RequestInt("year", tz.Now.Year);
            int month = core.Functions.RequestInt("month", tz.Now.Month);
            int day = core.Functions.RequestInt("day", tz.Now.Day);

            byte percentComplete = 0;
            TaskPriority priority = TaskPriority.Normal;

            DateTime dueDate = new DateTime(year, month, day, 17, 0, 0);

            string topic = string.Empty;
            string description = string.Empty;

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

            dueDateTimePicker.Value = dueDate;

            template.Parse("S_YEAR", year.ToString());
            template.Parse("S_MONTH", month.ToString());
            template.Parse("S_DAY", day.ToString());

            template.Parse("S_DUE_DATE", dueDateTimePicker);

            template.Parse("S_TOPIC", topic);
            template.Parse("S_DESCRIPTION", description);

            ParseSelectBox("S_PERCENT_COMPLETE", "percent-complete", percentages, percentComplete.ToString());
            ParseSelectBox("S_PRIORITY", "priority", priorities, ((byte)priority).ToString());

            Save(new EventHandler(AccountCalendarTaskNew_Save));
        }

        void AccountCalendarTaskNew_Save(object sender, EventArgs e)
        {
            long taskId = 0;
            string topic = string.Empty;
            string description = string.Empty;
            byte percentComplete = core.Functions.FormByte("percent-complete", 0);
            TaskPriority priority = (TaskPriority)core.Functions.FormByte("priority", (byte)TaskPriority.Normal);
            long dueDate = tz.GetUnixTimeStamp(tz.Now);
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

                dueDate = DateTimePicker.FormDate(core, "due-date");

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
                description = string.Empty;
            }


            if (!edit)
            {
                TaskStatus status = TaskStatus.Future;

                if (percentComplete == 100)
                {
                    status = TaskStatus.Completed;
                }

                Task calendarTask = Task.Create(core, LoggedInMember, Owner, topic, description, dueDate, status, percentComplete, priority);

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
                query.AddField("task_due_date_ut", dueDate);
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

        public Access Access
        {
            get
            {
                if (calendar == null)
                {
                    calendar = new Calendar(core, Owner);
                }
                return calendar.Access;
            }
        }

        public string AccessPermission
        {
            get
            {
                return "CREATE_EVENTS";
            }
        }
    }
}
