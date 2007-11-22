/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id: AccountBlog.cs,v 1.1 2007/11/18 00:22:42 Bakura\lachlan Exp $
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
using System.Text.RegularExpressions;
using System.Web;
using Lachlan.Web;

namespace BoxSocial.Internals
{
    public sealed class Core
    {
        public Mysql db;
        public Template template;
        public SessionState session;
        public int PageNo;
        public AppDomain CoreDomain;
        public string PagePath;
        public GroupCollection PagePathParts;
        public TimeZone tz;

        // TODO: remove
        public TPage page;

        public delegate void HookHandler(Core core, object sender);
        public delegate void LoadHandler(Core core, object sender);
        public delegate void PageHandler(Core core, object sender);
        public delegate bool CommentHandler(long itemId, Member viewer);
        public delegate void CommentCountHandler(long itemId, int adjustment);

        public event HookHandler PageHooks;
        public event LoadHandler LoadApplication;

        private List<PageHandle> pages = new List<PageHandle>();
        private Dictionary<string, CommentHandle> commentHandles = new Dictionary<string, CommentHandle>();

        /// <summary>
        /// A cache of user profiles including icons.
        /// </summary>
        private Dictionary<long, Member> userProfileCache = new Dictionary<long, Member>();

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<long, Member> UserProfiles
        {
            get
            {
                return userProfileCache;
            }
        }

        public long LoggedInMemberId
        {
            get
            {
                return Member.GetMemberId(session.LoggedInMember);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userIds"></param>
        public void LoadUserProfiles(List<long> userIds)
        {
            string idList = "";
            bool first = true;
            foreach (int id in userIds)
            {
                if (!userProfileCache.ContainsKey(id))
                {
                    if (first)
                    {
                        idList = id.ToString();
                        first = false;
                    }
                    else
                    {
                        idList = string.Format("{0}, {1}",
                            idList, id);
                    }
                }
            }

            if (!string.IsNullOrEmpty(idList))
            {
                DataTable usersTable = db.SelectQuery(string.Format("SELECT {1}, {2}, {3} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id INNER JOIN user_profile up ON uk.user_id = up.user_id LEFT JOIN countries c ON c.country_iso = up.profile_country LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE uk.user_id IN ({0})",
                    idList, Member.USER_INFO_FIELDS, Member.USER_PROFILE_FIELDS, Member.USER_ICON_FIELDS));

                foreach (DataRow userRow in usersTable.Rows)
                {
                    Member newUser = new Member(db, userRow, true, true);
                    userProfileCache.Add(newUser.Id, newUser);
                }
            }
        }

        public void LoadUserProfile(long userId)
        {
            if (!userProfileCache.ContainsKey(userId))
            {
                Member newUser = new Member(db, userId, true);
                userProfileCache.Add(newUser.Id, newUser);
            }
        }

        public Core(Mysql db, Template template)
        {
            PageHooks += new HookHandler(Core_Hooks);
            LoadApplication += new LoadHandler(Core_LoadApplication);

            this.db = db;
            this.template = template;
        }

        void Core_LoadApplication(Core core, object sender)
        {
            
        }

        void Core_Hooks(Core core, object sender)
        {
            
        }

        public void InvokeHooks(object sender)
        {
            PageHooks(this, sender);
        }

        public void InvokeNaked(object sender)
        {
            LoadApplication(this, sender);
        }

        public void InvokeApplication(object sender)
        {
            LoadApplication(this, sender);

            pages.Sort();

            foreach (PageHandle page in pages)
            {
                Match pathMatch = Regex.Match(PagePath, page.Expression);
                if (pathMatch.Success)
                {
                    PagePathParts = pathMatch.Groups;
                    page.Execute(this, sender);
                    return;
                }
            }
            Functions.Generate404(this);
        }

        public bool CanPostComment(string itemType, long itemId)
        {
            if (commentHandles.ContainsKey(itemType))
            {
                return commentHandles[itemType].CanPostComment(itemId, session.LoggedInMember);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public bool CanDeleteComment(string itemType, long itemId)
        {
            if (commentHandles.ContainsKey(itemType))
            {
                return commentHandles[itemType].CanDeleteComment(itemId, session.LoggedInMember);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public void AdjustCommentCount(string itemType, long itemId, int adjustment)
        {
            if (commentHandles.ContainsKey(itemType))
            {
                commentHandles[itemType].AdjustCommentCount(itemId, adjustment);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public void RegisterApplicationPage(string expression, Core.PageHandler pageHandle)
        {
            // register with a moderately high priority leaving room for higher priority registration
            // it doesn't matter if two pages have the same priority
            RegisterApplicationPage(expression, pageHandle, 8);
        }

        public void RegisterApplicationPage(string expression, Core.PageHandler pageHandle, int order)
        {
            pages.Add(new PageHandle(expression, pageHandle, order));
        }

        public void RegisterCommentHandle(string token, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount)
        {
            commentHandles.Add(token, new CommentHandle(token, canPostComment, canDeleteComment, adjustCommentCount));
        }

        public void EndResponse()
        {
            page.EndResponse();
        }

        /*public Access GetAccessFromItem(long itemId, string tableName, string columnPrefix)
        {
            DataTable itemTable = db.SelectQuery(string.Format("SELECT {2}item_id, {2}item_type, {2}access FROM {1} WHERE gi.{2}id = {0};",
                itemId, tableName, columnPrefix));

            if (itemTable.Rows.Count == 1)
            {
                Primitive owner = null;
                switch ((string)itemTable.Rows[0][columnPrefix + "item_type"])
                {
                    case "USER":
                        owner = new Member(db, (long)itemTable.Rows[0][columnPrefix + "item_id"]);
                        break;
                    case "GROUP":
                        owner = new ZinZam.Groups.Group(db, (long)itemTable.Rows[0][columnPrefix + "item_id"]);
                        break;
                    case "NETWORK":
                        owner = new Network(db, (long)itemTable.Rows[0][columnPrefix + "item_id"]);
                        break;
                    default:
                        throw new InvalidItemException();
                        break;
                }

                Access photoAccess = new Access(db, (ushort)itemTable.Rows[0][columnPrefix + "access"], owner);

                return photoAccess;
            }
        }*/
    }

    public class InvalidItemException : Exception
    {
    }
}
