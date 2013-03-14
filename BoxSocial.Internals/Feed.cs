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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Feed
    {

        public static List<Action> GetItems(Core core, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Action> feedItems = new List<Action>();

            SelectQuery query = Action.GetSelectQueryStub(typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.LimitCount = 64;

            List<long> friendIds = owner.GetFriendIds(16);

            if (friendIds.Count > 0)
            {
                core.LoadUserProfiles(friendIds);

                query.AddCondition("action_primitive_id", ConditionEquality.In, friendIds);
                query.AddCondition("action_primitive_type_id", ItemKey.GetTypeId(typeof(User)));

                DataTable feedTable = core.Db.Query(query);

                foreach (DataRow dr in feedTable.Rows)
                {
                    feedItems.Add(new Action(core, owner, dr));
                }
            }

            return feedItems;
        }

        public static void Show(Core core, TPage page, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Template template = new Template(core.Http.TemplatePath, "viewfeed.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            List<Action> feedActions = Feed.GetItems(core, owner);

            if (feedActions.Count > 0)
            {
                template.Parse("HAS_FEED_ITEMS", "TRUE");
                VariableCollection feedDateVariableCollection = null;
                string lastDay = core.Tz.ToStringPast(core.Tz.Now);

                foreach (Action feedAction in feedActions)
                {
                    DateTime feedItemDay = feedAction.GetTime(core.Tz);

                    if (feedDateVariableCollection == null || lastDay != core.Tz.ToStringPast(feedItemDay))
                    {
                        lastDay = core.Tz.ToStringPast(feedItemDay);
                        feedDateVariableCollection = template.CreateChild("feed_days_list");

                        feedDateVariableCollection.Parse("DAY", lastDay);
                    }

                    VariableCollection feedItemVariableCollection = feedDateVariableCollection.CreateChild("feed_item");

                    core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.Title);
                    core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId]);

                    feedItemVariableCollection.Parse("USER_DISPLAY_NAME", feedAction.Owner.DisplayName);

                    feedItemVariableCollection.Parse("ID", feedAction.ActionItemKey.Id);
                    feedItemVariableCollection.Parse("TYPE_ID", feedAction.ActionItemKey.TypeId);

                    if (feedAction.ActionItemKey.ImplementsLikeable)
                    {
                        feedItemVariableCollection.Parse("LIKEABLE", "TRUE");

                        if (feedAction.Info.Likes > 0)
                        {
                            feedItemVariableCollection.Parse("LIKES", string.Format(" {0:d}", feedAction.Info.Likes));
                            feedItemVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", feedAction.Info.Dislikes));
                        }
                    }

                    if (feedAction.ActionItemKey.ImplementsCommentable)
                    {
                        feedItemVariableCollection.Parse("COMMENTABLE", "TRUE");

                        if (feedAction.Info.Comments > 0)
                        {
                            feedItemVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", feedAction.Info.Comments));
                        }
                    }

                    if (feedAction.Owner is User)
                    {
                        feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).UserTile);
                    }
                }
            }

            core.AddMainPanel(template);
        }
    }
}
