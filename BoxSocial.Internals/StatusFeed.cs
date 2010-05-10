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

namespace BoxSocial.Internals
{
    public static class StatusFeed
    {
        public static List<StatusMessage> GetItems(Core core, User owner)
        {
            return GetItems(core, owner, 1);
        }

        public static List<StatusMessage> GetItems(Core core, User owner, int page)
        {
            List<StatusMessage> feedItems = new List<StatusMessage>();

            SelectQuery query = StatusMessage.GetSelectQueryStub(typeof(StatusMessage));
            query.AddSort(SortOrder.Descending, "status_time_ut");
            query.AddCondition("user_id", owner.Id);
            query.LimitCount = 50;
            query.LimitStart = (page - 1) * 50;

            DataTable feedTable = core.Db.Query(query);

            foreach (DataRow dr in feedTable.Rows)
            {
                feedItems.Add(new StatusMessage(core, owner, dr));
            }

            return feedItems;
        }

        public static List<StatusMessage> GetFriendItems(Core core, User owner)
        {
            return GetFriendItems(core, owner, 50, 1);
        }

        public static List<StatusMessage> GetFriendItems(Core core, User owner, int limit)
        {
            return GetFriendItems(core, owner, limit, 1);
        }

        public static List<StatusMessage> GetFriendItems(Core core, User owner, int limit, int page)
        {
            List<long> friendIds = owner.GetFriendIds();
            List<StatusMessage> feedItems = new List<StatusMessage>();

            if (friendIds.Count > 0)
            {
                SelectQuery query = StatusMessage.GetSelectQueryStub(typeof(StatusMessage));
                query.AddSort(SortOrder.Descending, "status_time_ut");
                query.AddCondition("user_id", ConditionEquality.In, friendIds);
                query.LimitCount = limit;
                query.LimitStart = (page - 1) * limit;

                // if limit is less than 10, we will only get one for each member
                if (limit < 10)
                {
                    //query.AddGrouping("user_id");
                    // WHERE current
                }

                DataTable feedTable = core.Db.Query(query);

                core.LoadUserProfiles(friendIds);
                foreach (DataRow dr in feedTable.Rows)
                {
                    feedItems.Add(new StatusMessage(core, core.PrimitiveCache[(long)dr["user_id"]], dr));
                }
            }

            return feedItems;
        }

        public static StatusMessage GetLatest(Core core, User owner)
        {
            SelectQuery query = StatusMessage.GetSelectQueryStub(typeof(StatusMessage));
            query.AddSort(SortOrder.Descending, "status_time_ut");
            query.AddCondition("user_id", owner.Id);
            query.LimitCount = 1;

            DataTable feedTable = core.Db.Query(query);

            if (feedTable.Rows.Count == 1)
            {
                return new StatusMessage(core, owner, feedTable.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static StatusMessage SaveMessage(Core core, string message)
        {
            ApplicationEntry ae = new ApplicationEntry(core, core.Session.LoggedInMember, "Profile");
            ae.PublishToFeed(core.Session.LoggedInMember, message, "");

            return StatusMessage.Create(core, core.Session.LoggedInMember, message);
        }

        /*
         * TODO: show status feed history
         */
        public static void Show(Core core, TPage page, User owner)
        {
            core.Template.SetTemplate("Profile", "viewstatusfeed");

            List<StatusMessage> items = StatusFeed.GetItems(core, owner, page.page);

            foreach (StatusMessage item in items)
            {
                VariableCollection statusMessageVariableCollection = core.Template.CreateChild("status_messages");

                statusMessageVariableCollection.Parse("STATUS_MESSAGE", item.Message);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(item.GetTime(core.Tz)));
            }

            core.Display.ParsePagination(core.Uri.BuildStatusUri(owner), page.page, (int)Math.Ceiling(owner.Info.StatusMessages / 10.0));

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "profile", "Profile" });
            breadCrumbParts.Add(new string[] { "status", "Status Feed" });

            owner.ParseBreadCrumbs(breadCrumbParts);
        }
    }
}
