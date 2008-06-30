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
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Calendar
{
    [AccountModule("calendar")]
    public class AccountCalendar : AccountModule
    {
        public AccountCalendar(Account account)
            : base(account)
        {
            //RegisterSubModule += new RegisterSubModuleHandler(ManageCalendar);
            //RegisterSubModule += new RegisterSubModuleHandler(NewEvent);
            //RegisterSubModule += new RegisterSubModuleHandler(NewTask);
            RegisterSubModule += new RegisterSubModuleHandler(MarkTaskComplete);
            //RegisterSubModule += new RegisterSubModuleHandler(EventInvite);
            //RegisterSubModule += new RegisterSubModuleHandler(DeleteEvent);
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

        public override int Order
        {
            get
            {
                return 9;
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
    }
}
