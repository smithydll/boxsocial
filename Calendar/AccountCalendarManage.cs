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
    [AccountSubModule("calendar", "calendar", true)]
    public class AccountCalendarManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("MANAGE_CALENDAR");
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountCalendarManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountCalendarManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountCalendarManage_Load);
            this.Show += new EventHandler(AccountCalendarManage_Show);
        }

        void AccountCalendarManage_Load(object sender, EventArgs e)
        {
        }

        void AccountCalendarManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_calendar_manage");

            template.Parse("U_NEW_EVENT", core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-event", true));
            template.Parse("U_NEW_TASK", core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-task", true));

            Calendar calendar = new Calendar(core);
            List<Event> events = calendar.GetEvents(core, Owner, UnixTime.UnixTimeStamp() - 24 * 60 * 60, UnixTime.UnixTimeStamp() + 30 * 24 * 60 * 60);

            foreach (Event ev in events)
            {
                VariableCollection blogVariableCollection = template.CreateChild("blog_list");

                DateTime startTime = ev.GetStartTime(tz);

                blogVariableCollection.Parse("SUBJECT", ev.Subject);
                blogVariableCollection.Parse("START_TIME", tz.DateTimeToString(startTime));

                blogVariableCollection.Parse("U_VIEW", ev.Uri);

                blogVariableCollection.Parse("U_EDIT", core.Hyperlink.BuildAccountSubModuleUri("calendar", "new-event", "edit", ev.Id, true));
                blogVariableCollection.Parse("U_EDIT_PERMISSIONS", Access.BuildAclUri(core, ev));
                blogVariableCollection.Parse("U_DELETE", core.Hyperlink.BuildAccountSubModuleUri("calendar", "delete-event", "edit", ev.Id, true));
            }

            List<Task> tasks = calendar.GetTasks(core, Owner, UnixTime.UnixTimeStamp() - 24 * 60 * 60, UnixTime.UnixTimeStamp() + 30 * 24 * 60 * 60, true);


        }
    }
}
