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

        public static List<Action> GetItems(Core core, User owner, int currentPage, int perPage, long currentOffset, out bool moreContent)
        {
            double pessimism = 2.0;

            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Action> feedItems = new List<Action>();
            moreContent = false;

            SelectQuery query = Action.GetSelectQueryStub(typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.LimitCount = 64;

            List<long> friendIds = owner.GetFriendIds(100);
            if (core.Session.IsLoggedIn)
            {
                friendIds.Add(core.LoggedInMemberId);
            }

            // TODO: Add subscriptions to feed

            if (friendIds.Count > 0)
            {
                core.LoadUserProfiles(friendIds);

                query.AddCondition("action_primitive_id", ConditionEquality.In, friendIds);
                query.AddCondition("action_primitive_type_id", ItemKey.GetTypeId(typeof(User)));

                {
                    long lastId = 0;
                    QueryCondition qc1 = null;
                    if (currentOffset > 0)
                    {
                        qc1 = query.AddCondition("action_id", ConditionEquality.LessThan, currentOffset);
                    }
                    query.LimitCount = (int)(perPage * pessimism);

                    while (feedItems.Count <= perPage)
                    {
                        DataTable feedTable = core.Db.Query(query);

                        List<IPermissibleItem> tempMessages = new List<IPermissibleItem>();
                        List<Action> tempActions = new List<Action>();

                        if (feedTable.Rows.Count == 0)
                        {
                            break;
                        }

                        foreach (DataRow row in feedTable.Rows)
                        {
                            Action action = new Action(core, owner, row);
                            tempActions.Add(action);
                            core.ItemCache.RequestItem(action.ActionItemKey);
                        }

                        foreach (Action action in tempActions)
                        {
                            tempMessages.Add(action.PermissiveParent);
                        }

                        core.AcessControlCache.CacheGrants(tempMessages);

                        foreach (Action action in tempActions)
                        {
                            if (action.PermissiveParent.Access.Can("VIEW"))
                            {
                                if (feedItems.Count == perPage)
                                {
                                    moreContent = true;
                                    break;
                                }
                                else
                                {
                                    feedItems.Add(action);
                                }
                            }
                            lastId = action.Id;
                        }

                        //query.LimitStart += query.LimitCount;
                        if (qc1 == null)
                        {
                            qc1 = query.AddCondition("action_id", ConditionEquality.LessThan, lastId);
                        }
                        else
                        {
                            qc1.Value = lastId;
                        }
                        query.LimitCount = (int)(query.LimitCount * pessimism);

                        if (moreContent)
                        {
                            break;
                        }
                    }
                }

                //DataTable feedTable = core.Db.Query(query);

                /*foreach (DataRow dr in feedTable.Rows)
                {
                    feedItems.Add(new Action(core, owner, dr));
                }*/

                //int offset = 0;
                //int i = 0;

                //while (i < limitStart + perPage + 1 && offset < feedTable.Rows.Count)
                /*{
                    List<IPermissibleItem> tempMessages = new List<IPermissibleItem>();
                    List<Action> tempActions = new List<Action>();
                    int j = 0;
                    for (j = offset; j < Math.Min(offset + perPage * 2, feedTable.Rows.Count); j++)
                    {
                        Action action = new Action(core, owner, feedTable.Rows[j]);
                        tempActions.Add(action);
                    }

                    if (tempMessages.Count > 0)
                    {
                        core.AcessControlCache.CacheGrants(tempMessages);

                        foreach (IPermissibleItem message in tempMessages)
                        {
                            if (message.Access.Can("VIEW"))
                            {
                                if (i >= limitStart + perPage)
                                {
                                    moreContent = true;
                                    break;
                                }
                                if (i >= limitStart)
                                {
                                    feedItems.Add((Action)message);
                                }
                                i++;
                            }
                        }
                    }
                    else
                    {
                        //break;
                    }

                    offset = j;
                }*/
            }

            return feedItems;
        }

        public static void Show(Core core, TPage page, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (core.IsAjax)
            {
                ShowMore(core, page, owner);
                return;
            }

            Template template = new Template(core.Http.TemplatePath, "viewfeed.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", owner.ItemKey);

            template.Parse("S_STATUS_PERMISSIONS", permissionSelectBox);

            bool moreContent = false;
            long lastId = 0;
            List<Action> feedActions = Feed.GetItems(core, owner, page.TopLevelPageNumber, 20, page.TopLevelPageOffset, out moreContent);

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
                    core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

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

                    //Access access = new Access(core, feedAction.ActionItemKey, true);
                    if (feedAction.PermissiveParent.Access.IsPublic())
                    {
                        feedItemVariableCollection.Parse("IS_PUBLIC", "TRUE");
                        feedItemVariableCollection.Parse("SHAREABLE", "TRUE");
                        //feedItemVariableCollection.Parse("U_SHARE", feedAction.ShareUri);

                        if (feedAction.Info.SharedTimes > 0)
                        {
                            feedItemVariableCollection.Parse("SHARES", string.Format(" {0:d}", feedAction.Info.SharedTimes));
                        }
                    }

                    if (feedAction.Owner is User)
                    {
                        feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).UserTile);
                        feedItemVariableCollection.Parse("USER_ICON", ((User)feedAction.Owner).UserIcon);
                    }

                    lastId = feedAction.Id;
                }
            }

            core.Display.ParseBlogPagination(template, "PAGINATION", core.Hyperlink.BuildHomeUri(), 0, moreContent ? lastId : 0);
            template.Parse("U_NEXT_PAGE", core.Hyperlink.BuildHomeUri() + "?p=" + (core.TopLevelPageNumber + 1) + "&o=" + lastId);

            core.AddMainPanel(template);
        }

        public static void ShowMore(Core core, TPage page, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Template template = new Template("pane.feeditem.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            bool moreContent = false;
            long lastId = 0;
            List<Action> feedActions = Feed.GetItems(core, owner, page.TopLevelPageNumber, 20, page.TopLevelPageOffset, out moreContent);

            foreach (Action feedAction in feedActions)
            {
                VariableCollection feedItemVariableCollection = template.CreateChild("feed_days_list.feed_item");

                core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.Title);
                core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

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

                //Access access = new Access(core, feedAction.ActionItemKey, true);
                if (feedAction.PermissiveParent.Access.IsPublic())
                {
                    feedItemVariableCollection.Parse("IS_PUBLIC", "TRUE");
                    feedItemVariableCollection.Parse("SHAREABLE", "TRUE");
                    //feedItemVariableCollection.Parse("U_SHARE", feedAction.ShareUri);

                    if (feedAction.Info.SharedTimes > 0)
                    {
                        feedItemVariableCollection.Parse("SHARES", string.Format(" {0:d}", feedAction.Info.SharedTimes));
                    }
                }

                if (feedAction.Owner is User)
                {
                    feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).UserTile);
                    feedItemVariableCollection.Parse("USER_ICON", ((User)feedAction.Owner).UserIcon);
                }

                lastId = feedAction.Id;
            }

            string loadMoreUri = core.Hyperlink.BuildHomeUri() + "?p=" + (core.TopLevelPageNumber + 1) + "&o=" + lastId;
            core.Ajax.SendRawText(moreContent ? loadMoreUri : "noMoreContent", template.ToString());
        }
    }
}
