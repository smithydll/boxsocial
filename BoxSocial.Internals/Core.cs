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
        public Prose prose;

        internal TPage page;

        public delegate void HookHandler(HookEventArgs e);
        public delegate void LoadHandler(Core core, object sender);
        public delegate void PageHandler(Core core, object sender);
        public delegate bool CommentHandler(long itemId, User viewer);
        public delegate void CommentCountHandler(long itemId, int adjustment);
        public delegate void CommentPostedHandler(CommentPostedEventArgs e);
        public delegate void RatingHandler(ItemRatedEventArgs e);

        public event HookHandler HeadHooks;
        public event HookHandler FootHooks;
        public event HookHandler PageHooks;
        public event LoadHandler LoadApplication;

        Dictionary<string, Type> primitiveTypes = new Dictionary<string, Type>();
        Dictionary<string, PrimitiveAttribute> primitiveAttributes = new Dictionary<string, PrimitiveAttribute>();
        private List<PageHandle> pages = new List<PageHandle>();
        private Dictionary<string, CommentHandle> commentHandles = new Dictionary<string, CommentHandle>();
        private Dictionary<string, RatingHandler> ratingHandles = new Dictionary<string, RatingHandler>();

        /// <summary>
        /// A cache of application entries.
        /// </summary>
        private Dictionary<string, ApplicationEntry> applicationEntryCache = new Dictionary<string, ApplicationEntry>();

        private PrimitivesCache userProfileCache;

        /// <summary>
        /// Returns a list of user profiles cached in memory.
        /// </summary>
        public PrimitivesCache UserProfiles
        {
            get
            {
                return userProfileCache;
            }
        }

        public void LoadUserProfile(long userId)
        {
            userProfileCache.LoadUserProfile(userId);
        }

        public long LoadUserProfile(string username)
        {
            return userProfileCache.LoadUserProfile(username);
        }

        public void LoadUserProfiles(List<long> userIds)
        {
            userProfileCache.LoadUserProfiles(userIds);
        }

        public List<long> LoadUserProfiles(List<string> usernames)
        {
            return userProfileCache.LoadUserProfiles(usernames);
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
                return User.GetMemberId(session.LoggedInMember);
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
        /// Loads the application entry for the calling application
        /// </summary>
        internal void LoadApplicationEntry(string assemblyName)
        {
            applicationEntryCache.Add(assemblyName, new ApplicationEntry(this, session.LoggedInMember, assemblyName));
        }

        public Core(Mysql db, Template template)
        {
            HeadHooks += new HookHandler(Core_HeadHooks);
            FootHooks +=new HookHandler(Core_FootHooks);
            PageHooks += new HookHandler(Core_Hooks);
            LoadApplication += new LoadHandler(Core_LoadApplication);

            this.db = db;
            this.template = template;

            userProfileCache = new PrimitivesCache(this);

            prose = new Prose();
            prose.Initialise("en");

            template.SetProse(prose);

            AddPrimitiveType(typeof(User));
            FindAllPrimitivesLoaded();
        }

        void Core_LoadApplication(Core core, object sender)
        {
            
        }

        void Core_HeadHooks(HookEventArgs eventArgs)
        {

        }

        void Core_FootHooks(HookEventArgs eventArgs)
        {

        }

        void Core_Hooks(HookEventArgs eventArgs)
        {
            
        }

        public void InvokeHeadHooks(HookEventArgs eventArgs)
        {
            HeadHooks(eventArgs);
        }

        public void InvokeFootHooks(HookEventArgs eventArgs)
        {
            FootHooks(eventArgs);
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

        public void CommentPosted(string itemType, long itemId, Comment comment, User poster)
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

        public void CommentDeleted(string itemType, long itemId, Comment comment, User poster)
        {
            if (commentHandles.ContainsKey(itemType))
            {
                commentHandles[itemType].CommentDeleted(comment, poster, itemType, itemId);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public void ItemRated(string itemType, long itemId, int rating, User rater)
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

        public void RegisterCommentHandle(string token, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount)
        {
            RegisterCommentHandle(token, canPostComment, canDeleteComment, adjustCommentCount, null, null);
        }

        public void RegisterCommentHandle(string token, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount, Core.CommentPostedHandler commentPosted)
        {
            RegisterCommentHandle(token, canPostComment, canDeleteComment, adjustCommentCount, commentPosted, null);
        }

        public void RegisterCommentHandle(string token, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount, Core.CommentPostedHandler commentPosted, Core.CommentPostedHandler commentDeleted)
        {
            commentHandles.Add(token, new CommentHandle(token, canPostComment, canDeleteComment, adjustCommentCount, commentPosted, commentDeleted));
        }

        public void RegisterRatingHandle(string token, Core.RatingHandler itemRated)
        {
            ratingHandles.Add(token, itemRated);
        }

        private VariableCollection createHeadPanel()
        {
            return template.CreateChild("head_hook");
        }

        private VariableCollection createFootPanel()
        {
            return template.CreateChild("foot_hook");
        }

        private VariableCollection createMainPanel()
        {
            return template.CreateChild("app_panel");
        }

        private VariableCollection createSidePanel()
        {
            return template.CreateChild("app_panel_side");
        }

        public void AddHeadPanel(Template t)
        {
            VariableCollection panelVariableCollection = createHeadPanel();

            panelVariableCollection.ParseRaw("BODY", t.ToString());
        }

        public void AddFootPanel(Template t)
        {
            VariableCollection panelVariableCollection = createFootPanel();

            panelVariableCollection.ParseRaw("BODY", t.ToString());
        }

        public void AddMainPanel(Template t)
        {
            VariableCollection panelVariableCollection = createMainPanel();

            panelVariableCollection.ParseRaw("BODY", t.ToString());
        }

        public void AddSidePanel(Template t)
        {
            VariableCollection panelVariableCollection = createSidePanel();

            panelVariableCollection.ParseRaw("BODY", t.ToString());
        }

        public void AddPageAssembly(Assembly assembly)
        {
            template.AddPageAssembly(assembly);
        }

        public void AddPrimitiveType(Type type)
        {
            bool typeAdded = false;
            if (type.IsSubclassOf(typeof(Primitive)))
            {
                foreach (object attr in type.GetCustomAttributes(false))
                {
                    if (attr.GetType() == typeof(PrimitiveAttribute))
                    {
                        if (((PrimitiveAttribute)attr).Type != null)
                        {
                            if (!primitiveTypes.ContainsKey(((PrimitiveAttribute)attr).Type))
                            {
                                primitiveAttributes.Add(((PrimitiveAttribute)attr).Type, (PrimitiveAttribute)attr);
                                primitiveTypes.Add(((PrimitiveAttribute)attr).Type, type);
								// TODO: remove
								primitiveAttributes.Add(type.FullName, (PrimitiveAttribute)attr);
								primitiveTypes.Add(type.FullName, type);
                            }
                            typeAdded = true;
                        }
                    }
                }

                if (!typeAdded)
                {
                    if (!primitiveTypes.ContainsKey(type.FullName))
                    {
                        primitiveTypes.Add(type.FullName, type);
                    }
                }
            }
        }

        internal void FindAllPrimitivesLoaded()
        {
            AssemblyName[] assemblies = Assembly.Load(new AssemblyName("BoxSocial.FrontEnd")).GetReferencedAssemblies();

            foreach (AssemblyName an in assemblies)
            {
                Type[] types = Assembly.Load(an).GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Primitive)))
                    {
                        AddPrimitiveType(type);
                    }
                }
            }
        }

        public Type GetPrimitiveType(string ownerType)
        {
            if (primitiveTypes.ContainsKey(ownerType))
            {
                return primitiveTypes[ownerType];
            }
            else
            {
                return null;
            }
        }

        public PrimitiveAttribute GetPrimitiveAttributes(string ownerType)
        {
            if (primitiveAttributes.ContainsKey(ownerType))
            {
                return primitiveAttributes[ownerType];
            }
            else
            {
                return null;
            }
        }

        public void EndResponse()
        {
            page.EndResponse();
        }

        /*public Access GetAccessFromItem(long itemId, string tableName, string columnPrefix)
        {
            DataTable itemTable = db.Query(string.Format("SELECT {2}item_id, {2}item_type, {2}access FROM {1} WHERE gi.{2}id = {0};",
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

                Access photoAccess = new Access(core, (ushort)itemTable.Rows[0][columnPrefix + "access"], owner);

                return photoAccess;
            }
        }*/
    }
}
