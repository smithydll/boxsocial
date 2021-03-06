﻿/*
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
    [AccountSubModule(AppPrimitives.Member | AppPrimitives.Group | AppPrimitives.Network, "calendar", "task-complete")]
    public class AccountCalendarTaskMarkComplete : AccountSubModule, IPermissibleControlPanelSubModule
    {
        private Task task;

        public override string Title
        {
            get
            {
                return null;
            }
        }

        public override int Order
        {
            get
            {
                return - 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountCalendarTaskMarkComplete class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCalendarTaskMarkComplete(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountCalendarTaskMarkComplete_Load);
            this.Show += new EventHandler(AccountCalendarTaskMarkComplete_Show);
        }

        void AccountCalendarTaskMarkComplete_Load(object sender, EventArgs e)
        {
        }

        void AccountCalendarTaskMarkComplete_Show(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long taskId = core.Functions.FormLong("id", 0);
            bool isAjax = false;

            if (core.Http["ajax"] == "true")
            {
                isAjax = true;
            }

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
                        SetRedirectUri(Task.BuildTaskUri(core, task));
                    }
                    core.Response.ShowMessage("success", "Task Complete", "The task has been marked as complete.");
                }
            }
            catch (InvalidTaskException)
            {
                core.Response.ShowMessage("error", "Error", "An error occured while marking the task as complete, go back");
            }
        }

        public Access Access
        {
            get
            {
                if (task == null)
                {
                    task = new Task(core, core.Functions.RequestLong("id", 0));
                }
                return task.Access;
            }
        }

        public string AccessPermission
        {
            get
            {
                return "EDIT";
            }
        }
    }
}
