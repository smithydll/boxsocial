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
using BoxSocial.Networks;

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

    [DataTable("tasks")]
    [Permission("VIEW", "Can view the task", PermissionTypes.View)]
    [Permission("COMMENT", "Can comment on the task", PermissionTypes.Interact)]
    public class Task : NumberedItem, ICommentableItem, IPermissibleItem
    {
        #region Data Fields
        [DataField("task_id", DataFieldKeys.Primary)]
        private long taskId;
        [DataField("task_topic", 127)]
        private string topic;
        [DataField("task_description", MYSQL_TEXT)]
        private string description;
        [DataField("task_views")]
        private long views;
        [DataField("task_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("user_id")]
        private int userId; // creator
        [DataField("task_due_date_ut")]
        private long dueTimeRaw;
        [DataField("task_time_completed_ut")]
        private long completedTimeRaw;
        [DataField("task_category")]
        private ushort category;
        [DataField("task_status")]
        private byte status;
        [DataField("task_percent_complete")]
        private byte percentageComplete;
        [DataField("task_priority")]
        private byte priority;
        [DataField("task_simple_permissions")]
        private bool simplePermissions;
        #endregion

        private Access access;
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
                return Info.Comments;
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

        public TaskStatus Status
        {
            get
            {
                return (TaskStatus)status;
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
                return (TaskPriority)priority;
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

        public Task(Core core, long taskId)
            : this(core, null, taskId)
        {
        }

        public Task(Core core, Primitive owner, long taskId)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Task_ItemLoad);

            try
            {
                LoadItem(taskId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTaskException();
            }
        }

        public Task(Core core, Primitive owner, DataRow taskRow)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(Task_ItemLoad);

            try
            {
                loadItemInfo(taskRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidTaskException();
            }
        }

        void Task_ItemLoad()
        {
            if (percentageComplete == 100 || (TaskStatus)status == TaskStatus.Completed)
            {
                status = (byte)TaskStatus.Completed;
            }
            else if (UnixTime.UnixTimeStamp() > dueTimeRaw && (TaskStatus)status != TaskStatus.Overdue)
            {
                status = (byte)TaskStatus.Overdue;
            }
        }

        public static Task Create(Core core, User creator, Primitive owner, string topic, string description, long dueTimestamp, TaskStatus status, byte percentComplete, TaskPriority priority)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery query = new InsertQuery("tasks");
            query.AddField("user_id", creator.UserId);
            query.AddField("task_item_id", owner.Id);
            query.AddField("task_item_type_id", owner.TypeId);
            query.AddField("task_topic", topic);
            query.AddField("task_description", description);
            query.AddField("task_due_date_ut", dueTimestamp);
            query.AddField("task_views", 0);
            query.AddField("task_comments", 0);
            query.AddField("task_category", 0);
            query.AddField("task_status", (byte)status);
            query.AddField("task_percent_complete", percentComplete);
            query.AddField("task_priority", (byte)priority);
            query.AddField("task_time_completed_ut", 0);

            long taskId = core.Db.Query(query);

            Task myTask = new Task(core, owner, taskId);

            /*if (Access.FriendsCanRead(myTask.Permissions))
            {
                core.CallingApplication.PublishToFeed(creator, "created a new task", string.Format("[iurl={0}]{1}[/iurl]",
                    Task.BuildTaskUri(core, myTask), myTask.Topic));
            }*/

            return myTask;
        }

        public static void ShowAll(Core core, TPage page, Primitive owner)
        {
            page.template.SetTemplate("Calendar", "viewcalendartasks");

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.Parse("U_NEW_TASK", core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-task", true,
                    string.Format("year={0}", core.Tz.Now.Year),
                    string.Format("month={0}", core.Tz.Now.Month),
                    string.Format("day={0}", core.Tz.Now.Day)));
            }

            long startTime = core.Tz.GetUnixTimeStamp(new DateTime(core.Tz.Now.Year, core.Tz.Now.Month, core.Tz.Now.Day, 0, 0, 0)) - 60 * 60 * 24 * 7; // show tasks completed over the last week
            long endTime = startTime + 60 * 60 * 24 * 7 * (8 + 1); // skip ahead eight weeks into the future

            Calendar cal = new Calendar(core);

            List<Task> tasks = cal.GetTasks(core, owner, startTime, endTime, true);

            /* pages */
            core.Display.ParsePageList(owner, true);

            if (owner is User)
            {
                page.template.Parse("USER_ICON", ((User)owner).UserThumbnail);
                page.template.Parse("USER_COVER_PHOTO", ((User)owner).CoverPhoto);
            }

            VariableCollection taskDaysVariableCollection = null;
            string lastDay = core.Tz.ToStringPast(core.Tz.Now);

            if (tasks.Count > 0)
            {
                page.template.Parse("HAS_TASKS", "TRUE");
            }

            foreach (Task calendarTask in tasks)
            {
                DateTime taskDue = calendarTask.GetDueTime(core.Tz);

                if (taskDaysVariableCollection == null || lastDay != core.Tz.ToStringPast(taskDue))
                {
                    lastDay = core.Tz.ToStringPast(taskDue);
                    taskDaysVariableCollection = page.template.CreateChild("task_days");

                    taskDaysVariableCollection.Parse("DAY", lastDay);
                }

                VariableCollection taskVariableCollection = taskDaysVariableCollection.CreateChild("task_list");

                taskVariableCollection.Parse("DATE", taskDue.ToShortDateString() + " (" + taskDue.ToShortTimeString() + ")");
                taskVariableCollection.Parse("TOPIC", calendarTask.Topic);
                taskVariableCollection.Parse("ID", calendarTask.Id.ToString());
                taskVariableCollection.Parse("URI", Task.BuildTaskUri(core, calendarTask));
                taskVariableCollection.Parse("U_MARK_COMPLETE", Task.BuildTaskMarkCompleteUri(core, calendarTask));

                if (calendarTask.Status == TaskStatus.Overdue)
                {
                    taskVariableCollection.Parse("OVERDUE", "TRUE");
                    taskVariableCollection.Parse("CLASS", "overdue-task");
                }
                else if (calendarTask.Status == TaskStatus.Completed)
                {
                    taskVariableCollection.Parse("COMPLETE", "TRUE");
                    taskVariableCollection.Parse("CLASS", "complete-task");
                }
                else
                {
                    taskVariableCollection.Parse("CLASS", "task");
                }

                // TODO: fix this
                if (calendarTask.Priority == TaskPriority.High)
                {
                    taskDaysVariableCollection.ParseRaw("PRIORITY", "[<span class=\"high-priority\" title=\"High Priority\">H</span>]");
                }
                else if (calendarTask.Priority == TaskPriority.Low)
                {
                    taskDaysVariableCollection.ParseRaw("PRIORITY", "[<span class=\"low-priority\" title=\"Low Priority\">L</span>]");
                }
            }

            List<string[]> calendarPath = new List<string[]>();
            calendarPath.Add(new string[] { "calendar", "Calendar" });
            calendarPath.Add(new string[] { "*tasks", "Tasks" });
            owner.ParseBreadCrumbs(calendarPath);
        }

        public static void Show(Core core, TPage page, Primitive owner, long taskId)
        {
            page.template.SetTemplate("Calendar", "viewcalendartask");

            if (core.LoggedInMemberId == owner.Id && owner.Type == "USER")
            {
                page.template.Parse("U_NEW_TASK", core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-task", true,
                    string.Format("year={0}", core.Tz.Now.Year),
                    string.Format("month={0}", core.Tz.Now.Month),
                    string.Format("day={0}", core.Tz.Now.Day)));
                page.template.Parse("U_EDIT_TASK", core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-task", "edit", taskId, true));
            }

            try
            {
                Task calendarTask = new Task(core, owner, taskId);

                if (!calendarTask.Access.Can("VIEW"))
                {
                    core.Functions.Generate403();
                    return;
                }

                /* pages */
                core.Display.ParsePageList(owner, true);

                if (owner is User)
                {
                    page.template.Parse("USER_ICON", ((User)owner).UserThumbnail);
                    page.template.Parse("USER_COVER_PHOTO", ((User)owner).CoverPhoto);
                }

                page.template.Parse("TOPIC", calendarTask.Topic);
                page.template.Parse("DESCRIPTION", calendarTask.Description);
                page.template.Parse("DUE_DATE", calendarTask.GetDueTime(core.Tz).ToString());

                List<string[]> calendarPath = new List<string[]>();

                calendarPath.Add(new string[] { "calendar", "Calendar" });
                calendarPath.Add(new string[] { "*tasks", "Tasks" });
                calendarPath.Add(new string[] { "task/" + calendarTask.TaskId.ToString(), calendarTask.Topic });

                owner.ParseBreadCrumbs(calendarPath);
            }
            catch (InvalidTaskException)
            {
                core.Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
            }
        }

        public override long Id
        {
            get
            {
                return taskId;
            }
        }

        public static string BuildTaskUri(Core core, Task calendarTask)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}calendar/task/{1}",
                calendarTask.owner.UriStub, calendarTask.TaskId));
        }

        public static string BuildTasksUri(Core core, Primitive owner)
        {
            return core.Hyperlink.AppendSid(string.Format("{0}calendar/tasks",
                owner.UriStub));
        }

        public static string BuildTaskMarkCompleteUri(Core core, Task calendarTask)
        {
            return core.Hyperlink.BuildAccountSubModuleUri("calendar", "mark-complete", calendarTask.Id, true);
        }

        public override string Uri
        {
            get
            {
                return Task.BuildTaskUri(core, this);
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

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                return AccessControlLists.GetPermissions(core, this);
            }
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

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string DisplayTitle
        {
            get
            {
                return "Task: " + Topic;
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidTaskException : InvalidItemException
    {
    }
}
