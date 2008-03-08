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

/*
 * DONE:
 * 
 * ALTER TABLE `zinzam0_zinzam`.`user_info` ADD COLUMN `user_status_messages` BIGINT NOT NULL AFTER `user_last_visit_ut`;
 * 
 */

namespace BoxSocial.Internals
{
    public static class StatusFeed
    {
        public static List<StatusMessage> GetItems(Core core, Member owner)
        {
            return GetItems(core, owner, 1);
        }

        public static List<StatusMessage> GetItems(Core core, Member owner, int page)
        {
            List<StatusMessage> feedItems = new List<StatusMessage>();

            SelectQuery query = new SelectQuery("user_status_messages usm");
            query.AddFields(StatusMessage.STATUS_MESSAGE_FIELDS);
            query.AddSort(SortOrder.Descending, "usm.status_time_ut");
            query.AddCondition("user_id", owner.Id);
            query.LimitCount = 50;
            query.LimitStart = (page - 1) * 50;

            DataTable feedTable = core.db.SelectQuery(query);

            foreach (DataRow dr in feedTable.Rows)
            {
                feedItems.Add(new StatusMessage(core.db, owner, dr));
            }

            return feedItems;
        }

        public static StatusMessage GetLatest(Core core, Member owner)
        {
            SelectQuery query = new SelectQuery("user_status_messages usm");
            query.AddFields(StatusMessage.STATUS_MESSAGE_FIELDS);
            query.AddSort(SortOrder.Descending, "usm.status_time_ut");
            query.AddCondition("user_id", owner.Id);
            query.LimitCount = 1;

            DataTable feedTable = core.db.SelectQuery(query);

            if (feedTable.Rows.Count == 1)
            {
                return new StatusMessage(core.db, owner, feedTable.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static StatusMessage SaveMessage(Core core, string message)
        {
            ApplicationEntry ae = new ApplicationEntry(core.db, core.session.LoggedInMember, "Profile");
            ae.PublishToFeed(core.session.LoggedInMember, message, "");

            return StatusMessage.Create(core.db, core.session.LoggedInMember, message);
        }

        /*
         * TODO: show status feed history
         */
        public static void Show(Core core, TPage page, Member owner)
        {
            core.template.SetTemplate("Profile", "viewstatusfeed");

            List<StatusMessage> items = StatusFeed.GetItems(core, owner, page.page);

            foreach (StatusMessage item in items)
            {
                VariableCollection statusMessageVariableCollection = core.template.CreateChild("status_messages");

                statusMessageVariableCollection.ParseVariables("STATUS_MESSAGE", HttpUtility.HtmlEncode(item.Message));
                statusMessageVariableCollection.ParseVariables("STATUS_UPDATED", HttpUtility.HtmlEncode(core.tz.DateTimeToString(item.GetTime(core.tz))));
            }

            core.template.ParseVariables("PAGINATION", Display.GeneratePagination(Linker.BuildStatusUri(owner), page.page, (int)Math.Ceiling(owner.StatusMessages / 10.0)));
            core.template.ParseVariables("BREADCRUMBS", Functions.GenerateBreadCrumbs(owner.UserName, "profile/status"));
        }
    }
}
