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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Feed
    {

        public static List<Action> GetItems(Core core, Member owner)
        {
            List<Action> feedItems = new List<Action>();

            SelectQuery query = new SelectQuery("actions at");
            query.AddFields(Action.FEED_FIELDS);
            query.AddSort(SortOrder.Descending, "at.action_time_ut");
            query.LimitCount = 64;

            List<long> friendIds = owner.GetFriendIds(16);

            if (friendIds.Count > 0)
            {
                core.LoadUserProfiles(friendIds);

                query.AddCondition("action_primitive_id", ConditionEquality.In, friendIds);
                query.AddCondition("action_primitive_type", "USER");

                DataTable feedTable = core.db.Query(query);

                foreach (DataRow dr in feedTable.Rows)
                {
                    feedItems.Add(new Action(core.db, owner, dr));
                }
            }

            return feedItems;
        }

        public static void Show(Core core, TPage page, Member owner)
        {
            Template template = new Template("viewfeed.html");

            List<Action> feedActions = Feed.GetItems(core, owner);

            if (feedActions.Count > 0)
            {
                template.ParseVariables("HAS_FEED_ITEMS", "TRUE");
                VariableCollection feedDateVariableCollection = null;
                string lastDay = core.tz.ToStringPast(core.tz.Now);

                foreach (Action feedAction in feedActions)
                {
                    DateTime feedItemDay = feedAction.GetTime(core.tz);

                    if (feedDateVariableCollection == null || lastDay != core.tz.ToStringPast(feedItemDay))
                    {
                        lastDay = core.tz.ToStringPast(feedItemDay);
                        feedDateVariableCollection = template.CreateChild("feed_days_list");

                        feedDateVariableCollection.ParseVariables("DAY", HttpUtility.HtmlEncode(lastDay));
                    }

                    VariableCollection feedItemVariableCollection = feedDateVariableCollection.CreateChild("feed_item");

                    feedItemVariableCollection.ParseVariables("TITLE", Bbcode.Parse(HttpUtility.HtmlEncode(feedAction.Title)));
                    feedItemVariableCollection.ParseVariables("TEXT", Bbcode.Parse(HttpUtility.HtmlEncode(feedAction.Body), core.session.LoggedInMember, core.UserProfiles[feedAction.OwnerId]));
                }
            }

            core.AddMainPanel(template);
        }
    }
}
