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
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<StatusMessage> feedItems = new List<StatusMessage>();
            bool moreContent = false;

            int bpage = page;
            int perPage = 20;
            int limitStart = (bpage - 1) * perPage;

            SelectQuery query = StatusMessage.GetSelectQueryStub(typeof(StatusMessage));
            query.AddSort(SortOrder.Descending, "status_time_ut");
            query.AddCondition("user_id", owner.Id);
            /*query.LimitCount = 50;
            query.LimitStart = (page - 1) * 50;*/

            DataTable feedTable = core.Db.Query(query);

            /*foreach (DataRow dr in feedTable.Rows)
            {
                feedItems.Add(new StatusMessage(core, owner, dr));
            }

            return feedItems;*/

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
            if (core == null)
            {
                throw new NullCoreException();
            }

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
            if (core == null)
            {
                throw new NullCoreException();
            }

            StatusMessage statusMessage = StatusMessage.Create(core, core.Session.LoggedInMember, message);

            AccessControlLists acl = new AccessControlLists(core, statusMessage);
            acl.SaveNewItemPermissions();

            ApplicationEntry ae = new ApplicationEntry(core, core.Session.LoggedInMember, "Profile");
            ae.PublishToFeed(core.Session.LoggedInMember, statusMessage.ItemKey, "updated " + core.Session.LoggedInMember.Preposition + " status", core.Bbcode.FromStatusCode(message));

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

            core.Template.SetTemplate("Profile", "viewstatusfeed");

            if (core.Session.IsLoggedIn && e.Page.Owner == core.Session.LoggedInMember)
            {
                core.Template.Parse("OWNER", "TRUE");

            }

            core.Template.Parse("U_PROFILE", ((User)e.Page.Owner).ProfileUri);
            core.Template.Parse("USER_COVER_PHOTO", ((User)e.Page.Owner).CoverPhoto);

            PermissionGroupSelectBox permissionSelectBox = new PermissionGroupSelectBox(core, "permissions", e.Page.Owner.ItemKey);

            core.Template.Parse("S_STATUS_PERMISSIONS", permissionSelectBox);

            List<StatusMessage> items = StatusFeed.GetItems(core, (User)e.Page.Owner, e.Page.TopLevelPageNumber);

            foreach (StatusMessage item in items)
            {
                VariableCollection statusMessageVariableCollection = core.Template.CreateChild("status_messages");

                //statusMessageVariableCollection.Parse("STATUS_MESSAGE", core.Functions.Tldr(item.Message));
                core.Display.ParseBbcode(statusMessageVariableCollection, "STATUS_MESSAGE", core.Bbcode.FromStatusCode(item.Message), e.Page.Owner);
                statusMessageVariableCollection.Parse("STATUS_UPDATED", core.Tz.DateTimeToString(item.GetTime(core.Tz)));

                statusMessageVariableCollection.Parse("ID", item.Id.ToString());
                statusMessageVariableCollection.Parse("TYPE_ID", item.ItemKey.TypeId.ToString());
                statusMessageVariableCollection.Parse("USERNAME", item.Poster.DisplayName);
                statusMessageVariableCollection.Parse("U_PROFILE", item.Poster.ProfileUri);
                statusMessageVariableCollection.Parse("U_QUOTE", core.Uri.BuildCommentQuoteUri(item.Id));
                statusMessageVariableCollection.Parse("U_REPORT", core.Uri.BuildCommentReportUri(item.Id));
                statusMessageVariableCollection.Parse("U_DELETE", core.Uri.BuildCommentDeleteUri(item.Id));
                statusMessageVariableCollection.Parse("U_PERMISSIONS", item.Access.AclUri);
                statusMessageVariableCollection.Parse("USER_TILE", item.Poster.UserTile);
                statusMessageVariableCollection.Parse("USER_ICON", item.Poster.UserIcon);
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
                    statusMessageVariableCollection.Parse("SHAREABLE", "TRUE");
                    statusMessageVariableCollection.Parse("U_SHARE", item.ShareUri);

                    if (item.Info.SharedTimes > 0)
                    {
                        statusMessageVariableCollection.Parse("SHARES", string.Format(" {0:d}", item.Info.SharedTimes));
                    }
                }
            }

            core.Display.ParsePagination(core.Uri.BuildStatusUri((User)e.Page.Owner), e.Page.TopLevelPageNumber, (int)Math.Ceiling(((User)e.Page.Owner).UserInfo.StatusMessages / 10.0));

            /* pages */
            core.Display.ParsePageList(e.Page.Owner, true);

            List<string[]> breadCrumbParts = new List<string[]>();

            breadCrumbParts.Add(new string[] { "*profile", "Profile" });
            breadCrumbParts.Add(new string[] { "status-feed", "Status Feed" });

            e.Page.Owner.ParseBreadCrumbs(breadCrumbParts);
        }

        public static void ShowMore(Core core, ShowUPageEventArgs e)
        {
        }
    }
}
