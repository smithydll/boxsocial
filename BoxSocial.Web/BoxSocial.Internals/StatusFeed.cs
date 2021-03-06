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
            bool moreContent;
            return GetItems(core, owner, 1, 20, 0, out moreContent);
        }

        public static List<StatusMessage> GetItems(Core core, User owner, int currentPage, int perPage, long currentOffset, out bool moreContent)
        {
            double pessimism = 1.5;

            if (core == null)
            {
                throw new NullCoreException();
            }

            List<StatusMessage> feedItems = new List<StatusMessage>();
            moreContent = false;

            int bpage = currentPage;
            int limitStart = (bpage - 1) * perPage;

            SelectQuery query = StatusMessage.GetSelectQueryStub(core, typeof(StatusMessage));
            query.AddSort(SortOrder.Descending, "status_time_ut");
            query.AddCondition("user_id", owner.Id);
            /*query.LimitCount = 50;
            query.LimitStart = (page - 1) * 50;*/

            //if ((currentOffset > 0 && currentPage > 1) || currentOffset == 0)
            {
                long lastId = 0;
                QueryCondition qc1 = null;
                if (currentOffset > 0)
                {
                    qc1 = query.AddCondition("status_id", ConditionEquality.LessThan, currentOffset);
                }
                query.LimitCount = (int)(perPage * pessimism);

                while (feedItems.Count <= perPage)
                {
                    System.Data.Common.DbDataReader feedReader = core.Db.ReaderQuery(query);

                    List<IPermissibleItem> tempMessages = new List<IPermissibleItem>();

                    if (!feedReader.HasRows)
                    {
                        feedReader.Close();
                        feedReader.Dispose();

                        break;
                    }

                    while(feedReader.Read())
                    {
                        StatusMessage entry = new StatusMessage(core, owner, feedReader);
                        tempMessages.Add(entry);
                    }

                    feedReader.Close();
                    feedReader.Dispose();

                    core.AcessControlCache.CacheGrants(tempMessages);

                    foreach (IPermissibleItem message in tempMessages)
                    {
                        if (message.Access.Can("VIEW"))
                        {
                            if (feedItems.Count == perPage)
                            {
                                moreContent = true;
                                break;
                            }
                            else
                            {
                                feedItems.Add((StatusMessage)message);
                            }
                        }
                        lastId = message.Id;
                    }

                    //query.LimitStart += query.LimitCount;
                    if (qc1 == null)
                    {
                        qc1 = query.AddCondition("status_id", ConditionEquality.LessThan, lastId);
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
            /*else
            {
                DataTable feedTable = core.Db.Query(query);

                int offset = 0;
                int i = 0;

                while (i < limitStart + perPage + 1 && offset < feedTable.Rows.Count)
                {
                    List<IPermissibleItem> tempMessages = new List<IPermissibleItem>();
                    int j = 0;
                    for (j = offset; j < Math.Min(offset + perPage * 2, feedTable.Rows.Count); j++)
                    {
                        StatusMessage message = new StatusMessage(core, owner, feedTable.Rows[j]);
                        tempMessages.Add(message);
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
                                    feedItems.Add((StatusMessage)message);
                                }
                                i++;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }

                    offset = j;
                }
            }*/

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
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<long> friendIds = owner.GetFriendIds();
            List<StatusMessage> feedItems = new List<StatusMessage>();

            if (friendIds.Count > 0)
            {
                SelectQuery query = StatusMessage.GetSelectQueryStub(core, typeof(StatusMessage));
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

                System.Data.Common.DbDataReader feedReader = core.Db.ReaderQuery(query);

                core.LoadUserProfiles(friendIds);

                while(feedReader.Read())
                {
                    feedItems.Add(new StatusMessage(core, core.PrimitiveCache[(long)feedReader["user_id"]], feedReader));
                }

                feedReader.Close();
                feedReader.Dispose();
            }

            return feedItems;
        }

        public static StatusMessage GetLatest(Core core, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = StatusMessage.GetSelectQueryStub(core, typeof(StatusMessage));
            query.AddSort(SortOrder.Descending, "status_time_ut");
            query.AddCondition("user_id", owner.Id);
            query.LimitCount = 1;

            System.Data.Common.DbDataReader feedReader = core.Db.ReaderQuery(query);

            if (feedReader.HasRows)
            {
                feedReader.Read();

                StatusMessage newMessage = new StatusMessage(core, owner, feedReader);

                feedReader.Close();
                feedReader.Dispose();

                return newMessage;
            }
            else
            {
                feedReader.Close();
                feedReader.Dispose();

                return null;
            }
        }

        public static StatusMessage SaveMessage(Core core, string message)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            StatusMessage statusMessage = StatusMessage.Create(core, core.Session.LoggedInMember, message, core.Session.ApplicationId);

            AccessControlLists acl = new AccessControlLists(core, statusMessage);
            acl.SaveNewItemPermissions();

            core.Search.Index(statusMessage);

            ApplicationEntry ae = core.GetApplication("Profile");
            ae.PublishToFeed(core, core.Session.LoggedInMember, statusMessage, Functions.SingleLine(core.Bbcode.Flatten(statusMessage.Message)));

            return statusMessage;
        }

        /*
         * TODO: show status feed history
         */
        public static void Show(Core core, ShowUPageEventArgs e)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!e.Page.Owner.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            if (core.ResponseFormat == ResponseFormats.Xml)
            {
                ShowMore(core, e);
                return;
            }

            core.Template.SetTemplate("Profile", "viewstatusfeed");

            if (core.Session.IsLoggedIn && e.Page.Owner == core.Session.LoggedInMember)
            {
                core.Template.Parse("OWNER", "TRUE");

            }

            core.Template.Parse("PAGE_TITLE", e.Core.Prose.GetString("STATUS_FEED"));

            PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", e.Page.Owner.ItemKey);

            core.Template.Parse("S_STATUS_PERMISSIONS", permissionSelectBox);

            bool moreContent = false;
            long lastId = 0;
            List<StatusMessage> items = StatusFeed.GetItems(core, (User)e.Page.Owner, e.Page.TopLevelPageNumber, 20, e.Page.TopLevelPageOffset, out moreContent);

            foreach (StatusMessage item in items)
            {
                VariableCollection statusMessageVariableCollection = core.Template.CreateChild("status_messages");

                //statusMessageVariableCollection.Parse("STATUS_MESSAGE", core.Functions.Tldr(item.Message));
                core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(item.Message), e.Page.Owner, true, string.Empty, string.Empty);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(item.GetTime(core.Tz)));

                statusMessageVariableCollection.Parse("ID", item.Id.ToString());
                statusMessageVariableCollection.Parse("TYPE_ID", item.ItemKey.TypeId.ToString());
                statusMessageVariableCollection.Parse("USERNAME", item.Poster.DisplayName);
                statusMessageVariableCollection.Parse("USER_ID", item.Poster.Id);
                statusMessageVariableCollection.Parse("U_PROFILE", item.Poster.ProfileUri);
                statusMessageVariableCollection.Parse("U_QUOTE", string.Empty /*core.Hyperlink.BuildCommentQuoteUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_REPORT", string.Empty /*core.Hyperlink.BuildCommentReportUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_DELETE", string.Empty /*core.Hyperlink.BuildCommentDeleteUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_PERMISSIONS", item.Access.AclUri);
                statusMessageVariableCollection.Parse("USER_TILE", item.Poster.Tile);
                statusMessageVariableCollection.Parse("USER_ICON", item.Poster.Icon);
                statusMessageVariableCollection.Parse("URI", item.Uri);

                if (core.Session.IsLoggedIn)
                {
                    if (item.Owner.Id == core.Session.LoggedInMember.Id)
                    {
                        statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");
                    }
                }

                if (item.Info.Likes > 0)
                {
                    statusMessageVariableCollection.Parse("LIKES", string.Format(" {0:d}", item.Info.Likes));
                    statusMessageVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", item.Info.Dislikes));
                }

                if (item.Info.Comments > 0)
                {
                    statusMessageVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", item.Info.Comments));
                }

                if (item.Access.IsPublic())
                {
                    statusMessageVariableCollection.Parse("IS_PUBLIC", "TRUE");

                    if (item.ItemKey.GetType(core).Shareable)
                    {
                        statusMessageVariableCollection.Parse("SHAREABLE", "TRUE");
                        statusMessageVariableCollection.Parse("U_SHARE", item.ShareUri);

                        if (item.Info.SharedTimes > 0)
                        {
                            statusMessageVariableCollection.Parse("SHARES", string.Format(" {0:d}", item.Info.SharedTimes));
                        }
                    }
                }

                lastId = item.Id;
            }

            core.Display.ParseBlogPagination(core.Template, "PAGINATION", core.Hyperlink.BuildStatusUri((User)e.Page.Owner), 0, moreContent ? lastId : 0);
            core.Template.Parse("U_NEXT_PAGE", core.Hyperlink.BuildStatusUri((User)e.Page.Owner) + "?p=" + (core.TopLevelPageNumber + 1) + "&o=" + lastId);

            /* pages */
            core.Display.ParsePageList(e.Page.Owner, true);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "*profile", core.Prose.GetString("PROFILE") });
            breadCrumbParts.Add(new string[] { "status-feed", core.Prose.GetString("STATUS_FEED") });

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void ShowMore(Core core, ShowUPageEventArgs e)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!e.Page.Owner.Access.Can("VIEW"))
            {
                core.Functions.Generate403();
                return;
            }

            Template template = new Template("pane.statusmessage.html");
            template.Medium = core.Template.Medium;
            template.SetProse(core.Prose);

            bool moreContent = false;
            long lastId = 0;
            List<StatusMessage> items = StatusFeed.GetItems(core, (User)e.Page.Owner, e.Page.TopLevelPageNumber, 20, e.Page.TopLevelPageOffset, out moreContent);

            foreach (StatusMessage item in items)
            {
                VariableCollection statusMessageVariableCollection = template.CreateChild("status_messages");

                core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(item.Message), e.Page.Owner, true, string.Empty, string.Empty);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(item.GetTime(core.Tz)));

                statusMessageVariableCollection.Parse("ID", item.Id.ToString());
                statusMessageVariableCollection.Parse("TYPE_ID", item.ItemKey.TypeId.ToString());
                statusMessageVariableCollection.Parse("USERNAME", item.Poster.DisplayName);
                statusMessageVariableCollection.Parse("USER_ID", item.Poster.Id);
                statusMessageVariableCollection.Parse("U_PROFILE", item.Poster.ProfileUri);
                statusMessageVariableCollection.Parse("U_QUOTE", string.Empty /*core.Hyperlink.BuildCommentQuoteUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_REPORT", string.Empty /*core.Hyperlink.BuildCommentReportUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_DELETE", string.Empty /*core.Hyperlink.BuildCommentDeleteUri(item.Id)*/);
                statusMessageVariableCollection.Parse("U_PERMISSIONS", item.Access.AclUri);
                statusMessageVariableCollection.Parse("USER_TILE", item.Poster.Tile);
                statusMessageVariableCollection.Parse("USER_ICON", item.Poster.Icon);
                statusMessageVariableCollection.Parse("URI", item.Uri);

                if (core.Session.IsLoggedIn)
                {
                    if (item.Owner.Id == core.Session.LoggedInMember.Id)
                    {
                        statusMessageVariableCollection.Parse("IS_OWNER", "TRUE");
                    }
                }

                if (item.Info.Likes > 0)
                {
                    statusMessageVariableCollection.Parse("LIKES", string.Format(" {0:d}", item.Info.Likes));
                    statusMessageVariableCollection.Parse("DISLIKES", string.Format(" {0:d}", item.Info.Dislikes));
                }

                if (item.Info.Comments > 0)
                {
                    statusMessageVariableCollection.Parse("COMMENTS", string.Format(" ({0:d})", item.Info.Comments));
                }

                if (item.Access.IsPublic())
                {
                    statusMessageVariableCollection.Parse("IS_PUBLIC", "TRUE");
                    if (item.ItemKey.GetType(core).Shareable)
                    {
                        statusMessageVariableCollection.Parse("SHAREABLE", "TRUE");
                        statusMessageVariableCollection.Parse("U_SHARE", item.ShareUri);

                        if (item.Info.SharedTimes > 0)
                        {
                            statusMessageVariableCollection.Parse("SHARES", string.Format(" {0:d}", item.Info.SharedTimes));
                        }
                    }
                }
                lastId = item.Id;
            }

            string loadMoreUri = core.Hyperlink.BuildStatusUri((User)e.Page.Owner) + "?p=" + (core.TopLevelPageNumber + 1) + "&o=" + lastId;
            core.Response.SendRawText(moreContent ? loadMoreUri : "noMoreContent", template.ToString());
        }
    }
}
