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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class Core
    {
        internal static Mysql DB;

        public Mysql db;
        internal Template template;
        public SessionState session;
        public int PageNo;
        public AppDomain CoreDomain;
        public string PagePath;
        public GroupCollection PagePathParts;
        public UnixTime tz;

        internal TPage page;

        public delegate void HookHandler(HookEventArgs e);
        public delegate void LoadHandler(Core core, object sender);
        public delegate void PageHandler(Core core, object sender);
        public delegate bool CommentHandler(long itemId, Member viewer);
        public delegate void CommentCountHandler(long itemId, int adjustment);
        public delegate void CommentPostedHandler(CommentPostedEventArgs e);
        public delegate void RatingHandler(ItemRatedEventArgs e);

        public event HookHandler PageHooks;
        public event LoadHandler LoadApplication;

        private List<PageHandle> pages = new List<PageHandle>();
        private Dictionary<string, CommentHandle> commentHandles = new Dictionary<string, CommentHandle>();
        private Dictionary<string, RatingHandler> ratingHandles = new Dictionary<string, RatingHandler>();

        /// <summary>
        /// A cache of user profiles including icons.
        /// </summary>
        private Dictionary<long, Member> userProfileCache = new Dictionary<long, Member>();
        private Dictionary<string, long> userNameCache = new Dictionary<string, long>();

        /// <summary>
        /// A cache of application entries.
        /// </summary>
        private Dictionary<string, ApplicationEntry> applicationEntryCache = new Dictionary<string, ApplicationEntry>();

        /// <summary>
        /// Returns a list of user profiles cached in memory.
        /// </summary>
        public Dictionary<long, Member> UserProfiles
        {
            get
            {
                return userProfileCache;
            }
        }

        /// <summary>
        /// Returns a list of application entries cached in memory.
        /// </summary>
        public Dictionary<string, ApplicationEntry> ApplicationEntries
        {
            get
            {
                return applicationEntryCache;
            }
        }

        public long LoggedInMemberId
        {
            get
            {
                return Member.GetMemberId(session.LoggedInMember);
            }
        }

        public bool IsAjax
        {
            get
            {
                return page.IsAjax;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userIds"></param>
        public void LoadUserProfiles(List<long> userIds)
        {
            List<long> idList = new List<long>();
            foreach (long id in userIds)
            {
                if (!userProfileCache.ContainsKey(id))
                {
                    idList.Add(id);
                }
            }

            if (idList.Count > 0)
            {
                SelectQuery query = new SelectQuery("user_keys uk");
                query.AddFields(Member.USER_INFO_FIELDS, Member.USER_PROFILE_FIELDS, Member.USER_ICON_FIELDS);
                query.AddJoin(JoinTypes.Inner, "user_info ui", "uk.user_id", "ui.user_id");
                query.AddJoin(JoinTypes.Inner, "user_profile up", "uk.user_id", "up.user_id");
                query.AddJoin(JoinTypes.Left, "countries c", "up.profile_country", "c.country_iso");
                query.AddJoin(JoinTypes.Left, "gallery_items gi", "ui.user_icon", "gi.gallery_item_id");
                query.AddCondition("uk.user_id", ConditionEquality.In, idList);

                DataTable usersTable = db.SelectQuery(query);

                foreach (DataRow userRow in usersTable.Rows)
                {
                    Member newUser = new Member(db, userRow, true, true);
                    userProfileCache.Add(newUser.Id, newUser);
                    userNameCache.Add(newUser.UserName, newUser.Id);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"></param>
        public void LoadUserProfile(long userId)
        {
            if (!userProfileCache.ContainsKey(userId))
            {
                Member newUser = new Member(db, userId, true);
                userProfileCache.Add(newUser.Id, newUser);
                userNameCache.Add(newUser.UserName, newUser.Id);
            }
        }

        public long LoadUserProfile(string username)
        {
            if (userNameCache.ContainsKey(username))
            {
                return userNameCache[username];
            }

            Member newUser = new Member(db, username, true);
            if (!userProfileCache.ContainsKey(newUser.Id))
            {
                userProfileCache.Add(newUser.Id, newUser);
            }

            return newUser.Id;
        }

        /// <summary>
        /// Loads the application entry for the calling application
        /// </summary>
        internal void LoadApplicationEntry(string assemblyName)
        {
            applicationEntryCache.Add(assemblyName, new ApplicationEntry(db, session.LoggedInMember, assemblyName));
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

        void Core_Hooks(HookEventArgs eventArgs)
        {
            
        }

        public void InvokeHooks(HookEventArgs eventArgs)
        {
            PageHooks(eventArgs);
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
            Functions.Generate404();
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

        public void CommentPosted(string itemType, long itemId, Comment comment, Member poster)
        {
            if (commentHandles.ContainsKey(itemType))
            {
                commentHandles[itemType].CommentPosted(comment, poster, itemType, itemId);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public void ItemRated(string itemType, long itemId, int rating, Member rater)
        {
            if (ratingHandles.ContainsKey(itemType))
            {
                ratingHandles[itemType](new ItemRatedEventArgs(rating, rater, itemType, itemId));
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

        public void RegisterCommentHandle(string token, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount, Core.CommentPostedHandler commentPosted)
        {
            commentHandles.Add(token, new CommentHandle(token, canPostComment, canDeleteComment, adjustCommentCount, commentPosted));
        }

        public void RegisterRatingHandle(string token, Core.RatingHandler itemRated)
        {
            ratingHandles.Add(token, itemRated);
        }

        private VariableCollection createMainPanel()
        {
            return template.CreateChild("app_panel");
        }

        private VariableCollection createSidePanel()
        {
            return template.CreateChild("app_panel_side");
        }

        public void AddMainPanel(Template t)
        {
            VariableCollection panelVariableCollection = createMainPanel();

            panelVariableCollection.ParseVariables("BODY", t.ToString());
        }

        public void AddSidePanel(Template t)
        {
            VariableCollection panelVariableCollection = createSidePanel();

            panelVariableCollection.ParseVariables("BODY", t.ToString());
        }

        public void AddPageAssembly(Assembly assembly)
        {
            template.AddPageAssembly(assembly);
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
