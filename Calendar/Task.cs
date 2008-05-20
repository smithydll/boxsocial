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

    public enum TaskStatus : byte
    {
        Future = 0,
        Overdue = 1,
        Completed = 2,
    }

    public enum TaskPriority : byte
    {
        Normal = 0,
        Low = 1,
        High = 2,
    }

    /*
     * CREATE TABLE `zinzam0_zinzam`.`tasks` (
  `task_id` BIGINT NOT NULL AUTO_INCREMENT,
  `task_topic` VARCHAR(127) NOT NULL,
  `task_description` TEXT DEFAULT NULL,
  `task_views` BIGINT NOT NULL,
  `task_comments` BIGINT NOT NULL,
  `tasl_access` SMALLINT UNSIGNED NOT NULL,
  `user_id` INTEGER NOT NULL,
  `task_due_date_ut` BIGINT NOT NULL,
  `task_category` SMALLINT NOT NULL,
  `task_item_id` BIGINT NOT NULL,
  `task_item_type` VARCHAR(15) NOT NULL,
  PRIMARY KEY (`task_id`)
)
ENGINE = InnoDB;
     
     ALTER TABLE `zinzam0_zinzam`.`tasks` MODIFY COLUMN `task_category` SMALLINT(6) UNSIGNED NOT NULL,
 MODIFY COLUMN `task_item_type` VARCHAR(31) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL;
      
     ALTER TABLE `zinzam0_zinzam`.`tasks` CHANGE COLUMN `tasl_access` `task_access` SMALLINT(5) UNSIGNED NOT NULL;

     ALTER TABLE `zinzam0_zinzam`.`tasks` ADD COLUMN `task_status` TINYINT UNSIGNED NOT NULL AFTER `task_item_type`,
 ADD COLUMN `task_percent_complete` TINYINT UNSIGNED NOT NULL AFTER `task_status`,
 ADD COLUMN `task_priority` TINYINT UNSIGNED NOT NULL AFTER `task_percent_complete`;
     * 
     *
     ALTER TABLE `zinzam0_zinzam`.`tasks` ADD COLUMN `task_time_completed_ut` BIGINT NOT NULL AFTER `task_priority`;


     */
    public class Task : Item, ICommentableItem
    {
        public const string TASK_INFO_FIELDS = "tk.task_id, tk.task_topic, tk.task_description, tk.task_views, tk.task_comments, tk.task_access, tk.user_id, tk.task_due_date_ut, tk.task_category, tk.task_item_id, tk.task_item_type, tk.task_status, tk.task_percent_complete, tk.task_priority, tk.task_time_completed_ut";

        [DataField("task_id")]
        private long taskId;
        [DataField("task_topic")]
        private string topic;
        [DataField("task_description")]
        private string description;
        [DataField("task_views")]
        private long views;
        [DataField("task_comments")]
        private long comments;
        [DataField("task_access")]
        private ushort permissions;
        [DataField("task_item_id")]
        private long ownerId;
        [DataField("task_item_type")]
        private string ownerType;
        [DataField("user_id")]
        private int userId; // creator
        [DataField("task_due_date_ut")]
        private long dueTimeRaw;
        [DataField("task_time_completed_ut")]
        private long completedTimeRaw;
        [DataField("task_category")]
        private ushort category;
        private TaskStatus status;
        [DataField("task_percent_complete")]
        private byte percentageComplete;
        private TaskPriority priority;
        private Access taskAccess;
        private Primitive owner;

        public long TaskId
        {
            get
            {
                return taskId;
            }
        }

        public string Topic
        {
            get
            {
                return topic;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public long Views
        {
            get
            {
                return views;
            }
        }

        public long Comments
        {
            get
            {
                return comments;
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public Access TaskAccess
        {
            get
            {
                return taskAccess;
            }
        }

        public TaskStatus Status
        {
            get
            {
                return status;
            }
        }

        public byte PercentageComplete
        {
            get
            {
                return percentageComplete;
            }
        }

        public TaskPriority Priority
        {
            get
            {
                return priority;
            }
        }

        public long DueTimeRaw
        {
            get
            {
                return dueTimeRaw;
            }
        }

        public DateTime GetDueTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(dueTimeRaw);
        }

        public DateTime GetCompletedTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(completedTimeRaw);
        }

        public Task(Core core, Primitive owner, long taskId) : base(core)
        {
            this.owner = owner;

            DataTable tasksTable = db.Query(string.Format("SELECT {0} FROM tasks tk WHERE tk.user_id = {1} AND tk.task_id = {2};",
                Task.TASK_INFO_FIELDS, owner.Id, taskId));

            if (tasksTable.Rows.Count == 1)
            {
                loadTaskInfo(tasksTable.Rows[0]);
            }
            else
            {
                throw new InvalidTaskException();
            }
        }

        public Task(Core core, Primitive owner, DataRow taskRow) : base(core)
        {
            this.owner = owner;

            loadTaskInfo(taskRow);
        }

        private void loadTaskInfo(DataRow taskRow)
        {
            taskId = (long)taskRow["task_id"];
            topic = (string)taskRow["task_topic"];
            if (!(taskRow["task_description"] is DBNull))
            {
                description = (string)taskRow["task_description"];
            }
            views = (long)taskRow["task_views"];
            comments = (long)taskRow["task_comments"];
            permissions = (ushort)taskRow["task_access"];
            // ownerId
            userId = (int)taskRow["user_id"];
            dueTimeRaw = (long)taskRow["task_due_date_ut"];
            completedTimeRaw = (long)taskRow["task_time_completed_ut"];
            // category
            status = (TaskStatus)(byte)taskRow["task_status"];
            percentageComplete = (byte)taskRow["task_percent_complete"];
            priority = (TaskPriority)(byte)taskRow["task_priority"];

            if (percentageComplete == 100 || status == TaskStatus.Completed)
            {
                status = TaskStatus.Completed;
            }
            else if (UnixTime.UnixTimeStamp() > dueTimeRaw && status != TaskStatus.Overdue)
            {
                status = TaskStatus.Overdue;
            }

            taskAccess = new Access(db, permissions, owner);
        }

        public static Task Create(Core core, Member creator, Primitive owner, string topic, string description, long dueTimestamp, ushort permissions, TaskStatus status, byte percentComplete, TaskPriority priority)
        {
            InsertQuery query = new InsertQuery("tasks");
            query.AddField("user_id", creator.UserId);
            query.AddField("task_item_id", owner.Id);
            query.AddField("task_item_type", owner.Type);
            query.AddField("task_topic", topic);
            query.AddField("task_description", description);
            query.AddField("task_due_date_ut", dueTimestamp);
            query.AddField("task_access", permissions);
            query.AddField("task_views", 0);
            query.AddField("task_comments", 0);
            query.AddField("task_category", 0);
            query.AddField("task_status", (byte)status);
            query.AddField("task_percent_complete", percentComplete);
            query.AddField("task_priority", (byte)priority);
            query.AddField("task_time_completed_ut", 0);

            long taskId = core.db.Query(query);

            Task myTask = new Task(core, owner, taskId);

            if (Access.FriendsCanRead(myTask.Permissions))
            {
                AppInfo.Entry.PublishToFeed(creator, "created a new task", string.Format("[iurl={0}]{1}[/iurl]",
                    Task.BuildTaskUri(myTask), myTask.Topic));
            }

            return myTask;
        }

        public static void ShowAll(Core core, TPage page, Primitive owner)
        {
            page.template.SetTemplate("Calendar", "viewcalendartasks");

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.ParseVariables("U_NEW_TASK", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("calendar", "new-task", true,
                    string.Format("year={0}", core.tz.Now.Year),
                    string.Format("month={0}", core.tz.Now.Month),
                    string.Format("day={0}", core.tz.Now.Day))));
            }

            long startTime = core.tz.GetUnixTimeStamp(new DateTime(core.tz.Now.Year, core.tz.Now.Month, core.tz.Now.Day, 0, 0, 0)) - 60 * 60 * 24 * 7; // show tasks completed over the last week
            long endTime = startTime + 60 * 60 * 24 * 7 * (8 + 1); // skip ahead eight weeks into the future

            Calendar cal = new Calendar(core.db);

            List<Task> tasks = cal.GetTasks(core, owner, startTime, endTime, true);

            VariableCollection taskDaysVariableCollection = null;
            string lastDay = core.tz.ToStringPast(core.tz.Now);

            if (tasks.Count > 0)
            {
                page.template.ParseVariables("HAS_TASKS", "TRUE");
            }

            foreach (Task calendarTask in tasks)
            {
                DateTime taskDue = calendarTask.GetDueTime(core.tz);

                if (taskDaysVariableCollection == null || lastDay != core.tz.ToStringPast(taskDue))
                {
                    lastDay = core.tz.ToStringPast(taskDue);
                    taskDaysVariableCollection = page.template.CreateChild("task_days");

                    taskDaysVariableCollection.ParseVariables("DAY", HttpUtility.HtmlEncode(lastDay));
                }

                VariableCollection taskVariableCollection = taskDaysVariableCollection.CreateChild("task_list");

                taskVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(taskDue.ToShortDateString() + " (" + taskDue.ToShortTimeString() + ")"));
                taskVariableCollection.ParseVariables("TOPIC", HttpUtility.HtmlEncode(calendarTask.Topic));
                taskVariableCollection.ParseVariables("ID", HttpUtility.HtmlEncode(calendarTask.Id.ToString()));
                taskVariableCollection.ParseVariables("URI", HttpUtility.HtmlEncode(Task.BuildTaskUri(calendarTask)));
                taskVariableCollection.ParseVariables("U_MARK_COMPLETE", HttpUtility.HtmlEncode(Task.BuildTaskMarkCompleteUri(calendarTask)));

                if (calendarTask.Status == TaskStatus.Overdue)
                {
                    taskVariableCollection.ParseVariables("OVERDUE", "TRUE");
                    taskVariableCollection.ParseVariables("CLASS", "overdue-task");
                }
                else if (calendarTask.Status == TaskStatus.Completed)
                {
                    taskVariableCollection.ParseVariables("COMPLETE", "TRUE");
                    taskVariableCollection.ParseVariables("CLASS", "complete-task");
                }
                else
                {
                    taskVariableCollection.ParseVariables("CLASS", "task");
                }

                if (calendarTask.Priority == TaskPriority.High)
                {
                    taskDaysVariableCollection.ParseVariables("PRIORITY", "[<span class=\"high-priority\" title=\"High Priority\">H</span>]");
                }
                else if (calendarTask.Priority == TaskPriority.Low)
                {
                    taskDaysVariableCollection.ParseVariables("PRIORITY", "[<span class=\"low-priority\" title=\"Low Priority\">L</span>]");
                }
            }

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", "Calendar" });
            calendarPath.Add(new string[] { "*tasks", "Tasks" });
            page.template.ParseVariables("BREADCRUMBS", owner.GenerateBreadCrumbs(calendarPath));
        }

        public static void Show(Core core, TPage page, Primitive owner, long taskId)
        {
            page.template.SetTemplate("Calendar", "viewcalendartask");

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.ParseVariables("U_NEW_TASK", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("calendar", "new-task", true,
                    string.Format("year={0}", core.tz.Now.Year),
                    string.Format("month={0}", core.tz.Now.Month),
                    string.Format("day={0}", core.tz.Now.Day))));
                page.template.ParseVariables("U_EDIT_TASK", HttpUtility.HtmlEncode(AccountModule.BuildModuleUri("calendar", "new-task", true,
                    "mode=edit",
                    string.Format("id={0}", taskId))));
            }

            try
            {
                Task calendarTask = new Task(core, owner, taskId);

                calendarTask.TaskAccess.SetSessionViewer(core.session);

                if (!calendarTask.TaskAccess.CanRead)
                {
                    Functions.Generate403();
                    return;
                }

                page.template.ParseVariables("TOPIC", HttpUtility.HtmlEncode(calendarTask.Topic));
                page.template.ParseVariables("DESCRIPTION", HttpUtility.HtmlEncode(calendarTask.Description));
                page.template.ParseVariables("DUE_DATE", HttpUtility.HtmlEncode(calendarTask.GetDueTime(core.tz).ToString()));

                List<string[]> calendarPath = new List<string[]>();
                calendarPath.Add(new string[] { "calendar", "Calendar" });
                calendarPath.Add(new string[] { "*tasks", "Tasks" });
                calendarPath.Add(new string[] { "task/" + calendarTask.TaskId.ToString(), calendarTask.Topic });
                page.template.ParseVariables("BREADCRUMBS", owner.GenerateBreadCrumbs(calendarPath));
            }
            catch
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
            }
        }

        public override long Id
        {
            get
            {
                return taskId;
            }
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public static string BuildTaskUri(Task calendarTask)
        {
            return Linker.AppendSid(string.Format("{0}/calendar/task/{1}",
                calendarTask.owner.Uri, calendarTask.TaskId));
        }

        public static string BuildTasksUri(Primitive owner)
        {
            return Linker.AppendSid(string.Format("{0}/calendar/tasks",
                owner.Uri));
        }

        public static string BuildTaskMarkCompleteUri(Task calendarTask)
        {
            return AccountModule.BuildModuleUri("calendar", "mark-complete", true, string.Format("id={0}", calendarTask.Id));
        }

        public override string Uri
        {
            get
            {
                return Task.BuildTaskUri(this);
            }
        }

        #region ICommentableItem Members


        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Ascending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion
    }

    public class InvalidTaskException : Exception
    {
    }
}
