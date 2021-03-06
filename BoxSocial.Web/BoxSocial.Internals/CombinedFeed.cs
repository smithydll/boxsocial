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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public static class CombinedFeed
    {
        public static List<Action> GetNewerItems(Core core, User owner, long newerThanOffset)
        {
            List<Action> feedItems = new List<Action>(10);

            SelectQuery query = Action.GetSelectQueryStub(core, typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.LimitCount = 20;

            List<long> friendIds = new List<long> { owner.Id };

            QueryCondition qc1 = query.AddCondition("action_id", ConditionEquality.GreaterThan, newerThanOffset);

            List<IPermissibleItem> tempMessages = new List<IPermissibleItem>(10);
            List<Action> tempActions = new List<Action>(10);

            System.Data.Common.DbDataReader feedReader = core.Db.ReaderQuery(query);

            if (!feedReader.HasRows)
            {
                feedReader.Close();
                feedReader.Dispose();
                return feedItems;
            }

            while (feedReader.Read())
            {
                Action action = new Action(core, owner, feedReader);
                tempActions.Add(action);
            }

            feedReader.Close();
            feedReader.Dispose();

            foreach (Action action in tempActions)
            {
                core.ItemCache.RequestItem(new ItemKey(action.ActionItemKey.GetType(core).ApplicationId, ItemType.GetTypeId(core, typeof(ApplicationEntry))));
            }

            foreach (Action action in tempActions)
            {
                core.ItemCache.RequestItem(action.ActionItemKey);
                if (!action.ActionItemKey.Equals(action.InteractItemKey))
                {
                    core.ItemCache.RequestItem(action.InteractItemKey);
                }
            }

            foreach (Action action in tempActions)
            {
                tempMessages.Add(action.PermissiveParent);
            }

            if (tempMessages.Count > 0)
            {
                core.AcessControlCache.CacheGrants(tempMessages);
            }

            foreach (Action action in tempActions)
            {
                if (action.PermissiveParent.Access.Can("VIEW"))
                {
                    if (feedItems.Count == 10)
                    {
                        break;
                    }
                    else
                    {
                        feedItems.Add(action);
                    }
                }
            }

            return feedItems;
        }

        public static List<Action> GetItems(Core core, User owner, int currentPage, int perPage, long currentOffset, out bool moreContent)
        {
            double pessimism = 2.0;

            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Action> feedItems = new List<Action>();
            moreContent = false;

            SelectQuery query = Action.GetSelectQueryStub(core, typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.LimitCount = 64;

            List<long> friendIds = new List<long> { owner.Id };

            if (friendIds.Count > 0)
            {
                core.LoadUserProfiles(friendIds);

                query.AddCondition("action_primitive_id", ConditionEquality.In, friendIds);
                query.AddCondition("action_primitive_type_id", ItemKey.GetTypeId(core, typeof(User)));

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
                            if (!action.ActionItemKey.Equals(action.InteractItemKey))
                            {
                                core.ItemCache.RequestItem(action.InteractItemKey);
                            }
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
            }

            return feedItems;
        }

        public static void Show(Core core, TPage page, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!owner.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            if (core.ResponseFormat == ResponseFormats.Xml)
            {
                ShowMore(core, page, owner);
                return;
            }

            core.Template.SetTemplate("Profile", "viewfeed");

            if (core.Session.IsLoggedIn && owner == core.Session.LoggedInMember)
            {
                core.Template.Parse("OWNER", "TRUE");

            }

            core.Template.Parse("PAGE_TITLE", core.Prose.GetString("FEED"));

            PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", owner.ItemKey);

            core.Template.Parse("S_STATUS_PERMISSIONS", permissionSelectBox);

            bool moreContent;
            long lastId = 0;
            bool first = true;

            List<Action> feedActions = CombinedFeed.GetItems(core, owner, page.TopLevelPageNumber, 20, page.TopLevelPageOffset, out moreContent);

            foreach (Action feedAction in feedActions)
            {
                if (first)
                {
                    first = false;
                    core.Template.Parse("NEWEST_ID", feedAction.Id.ToString());
                }

                VariableCollection feedItemVariableCollection = core.Template.CreateChild("feed_days_list.feed_item");

                core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.FormattedTitle);
                core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

                feedItemVariableCollection.Parse("USER_DISPLAY_NAME", feedAction.Owner.DisplayName);

                feedItemVariableCollection.Parse("ID", feedAction.ActionItemKey.Id);
                feedItemVariableCollection.Parse("TYPE_ID", feedAction.ActionItemKey.TypeId);

                if (feedAction.ActionItemKey.GetType(core).Likeable)
                {
                    feedItemVariableCollection.Parse("LIKEABLE", "TRUE");

                    if (feedAction.Info.Likes > 0)
                    {
                        feedItemVariableCollection.Parse("LIKES", string.Format(" {0:d}", feedAction.Info.Likes));
                        feedItemVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", feedAction.Info.Dislikes));
                    }
                }

                if (feedAction.ActionItemKey.GetType(core).Commentable)
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
                    if (feedAction.ActionItemKey.GetType(core).Shareable)
                    {
                        feedItemVariableCollection.Parse("SHAREABLE", "TRUE");
                        //feedItemVariableCollection.Parse("U_SHARE", feedAction.ShareUri);

                        if (feedAction.Info.SharedTimes > 0)
                        {
                            feedItemVariableCollection.Parse("SHARES", string.Format(" {0:d}", feedAction.Info.SharedTimes));
                        }
                    }
                }
                else
                {
                    feedItemVariableCollection.Parse("IS_PUBLIC", "FALSE");
                    feedItemVariableCollection.Parse("SHAREABLE", "FALSE");
                }

                if (feedAction.Owner is User)
                {
                    feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).Tile);
                    feedItemVariableCollection.Parse("USER_ICON", ((User)feedAction.Owner).Icon);
                }

                lastId = feedAction.Id;
            }

            core.Display.ParseBlogPagination(core.Template, "PAGINATION", core.Hyperlink.BuildCombinedFeedUri((User)owner), 0, moreContent ? lastId : 0);
            core.Template.Parse("U_NEXT_PAGE", core.Hyperlink.BuildCombinedFeedUri((User)owner) + "?p=" + (core.TopLevelPageNumber + 1) + "&o=" + lastId);

            /* pages */
            core.Display.ParsePageList(owner, true);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "*profile", core.Prose.GetString("PROFILE") });
            breadCrumbParts.Add(new string[] { "feed", core.Prose.GetString("FEED") });

            owner.ParseBreadCrumbs(breadCrumbParts);
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
            List<Action> feedActions = CombinedFeed.GetItems(core, owner, page.TopLevelPageNumber, 20, page.TopLevelPageOffset, out moreContent);

            foreach (Action feedAction in feedActions)
            {
                VariableCollection feedItemVariableCollection = template.CreateChild("feed_days_list.feed_item");

                core.Display.ParseBbcode(feedItemVariableCollection, "TITLE", feedAction.FormattedTitle);
                core.Display.ParseBbcode(feedItemVariableCollection, "TEXT", feedAction.Body, core.PrimitiveCache[feedAction.OwnerId], true, string.Empty, string.Empty);

                feedItemVariableCollection.Parse("USER_DISPLAY_NAME", feedAction.Owner.DisplayName);

                feedItemVariableCollection.Parse("ID", feedAction.ActionItemKey.Id);
                feedItemVariableCollection.Parse("TYPE_ID", feedAction.ActionItemKey.TypeId);

                if (feedAction.ActionItemKey.GetType(core).Likeable)
                {
                    feedItemVariableCollection.Parse("LIKEABLE", "TRUE");

                    if (feedAction.Info.Likes > 0)
                    {
                        feedItemVariableCollection.Parse("LIKES", string.Format(" {0:d}", feedAction.Info.Likes));
                        feedItemVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", feedAction.Info.Dislikes));
                    }
                }

                if (feedAction.ActionItemKey.GetType(core).Commentable)
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
                    if (feedAction.ActionItemKey.GetType(core).Shareable)
                    {
                        feedItemVariableCollection.Parse("SHAREABLE", "TRUE");
                        //feedItemVariableCollection.Parse("U_SHARE", feedAction.ShareUri);

                        if (feedAction.Info.SharedTimes > 0)
                        {
                            feedItemVariableCollection.Parse("SHARES", string.Format(" {0:d}", feedAction.Info.SharedTimes));
                        }
                    }
                }

                if (feedAction.Owner is User)
                {
                    feedItemVariableCollection.Parse("USER_TILE", ((User)feedAction.Owner).Tile);
                    feedItemVariableCollection.Parse("USER_ICON", ((User)feedAction.Owner).Icon);
                }

                lastId = feedAction.Id;
            }

            string loadMoreUri = core.Hyperlink.BuildHomeUri() + "?p=" + (core.TopLevelPageNumber + 1) + "&o=" + lastId;
            core.Response.SendRawText(moreContent ? loadMoreUri : "noMoreContent", template.ToString());
        }

        public static void ShowMore(Core core, User owner)
        {
            long newestId = core.Functions.RequestLong("newest-id", 0);
            long oldestId = core.Functions.RequestLong("oldest-id", 0);
            long newerId = 0;

            bool moreContent = false;
            long lastId = 0;

            List<Action> feedActions = null;

            if (newestId > 0)
            {
                feedActions = CombinedFeed.GetNewerItems(core, owner, newestId);
            }
            else
            {
                feedActions = CombinedFeed.GetItems(core, owner, 1, 20, oldestId, out moreContent);
            }

            if (feedActions != null)
            {
                JsonSerializer js;
                StringWriter jstw;
                JsonTextWriter jtw;

                js = new JsonSerializer();
                jstw = new StringWriter();
                jtw = new JsonTextWriter(jstw);

                js.NullValueHandling = NullValueHandling.Ignore;

                core.Http.WriteJson(js, feedActions);
            }

            if (core.Db != null)
            {
                core.Db.CloseConnection();
            }

            core.Http.End();
        }
    }
}
