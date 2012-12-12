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
using System.Configuration;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class Core
    {
        private Http http;
        private Mysql db;
        private Template template;
        private SessionState session;
        private int pageNo;
        private AppDomain coreDomain;
        private string pagePath;
        private GroupCollection pagePathParts;
        private UnixTime tz;
        private Prose prose;
        private Bbcode bbcode;
        private Functions functions;
        private Display display;
        private Email email;
        private Ajax ajax;
        private Linker uri;
        private Settings applicationSettings;
        private Storage storage;

        internal TPage page;

        private PrimitivesCache userProfileCache;
        private NumberedItemsCache itemsCache;
        private AccessControlCache accessControlCache;

        public delegate void HookHandler(HookEventArgs e);
        public delegate void LoadHandler(Core core, object sender);
        public delegate void PageHandler(Core core, object sender);
        public delegate bool CommentHandler(ItemKey itemKey, User viewer);
        public delegate void CommentCountHandler(ItemKey itemKey, int adjustment);
        public delegate void CommentPostedHandler(CommentPostedEventArgs e);
        public delegate void RatingHandler(ItemRatedEventArgs e);
        public delegate void LikeHandler(ItemLikedEventArgs e);
        public delegate void SubscribeHandler(ItemSubscribedEventArgs e);
        public delegate void UnsubscribeHandler(ItemUnsubscribedEventArgs e);
        public delegate List<PrimitivePermissionGroup> PermissionGroupHandler(Core core, Primitive owner);

        public event HookHandler HeadHooks;
        public event HookHandler FootHooks;
        public event HookHandler PageHooks;
        public event LoadHandler LoadApplication;
        public event PermissionGroupHandler primitivePermissionGroupHook;

        private Dictionary<long, Type> primitiveTypes = new Dictionary<long, Type>();
        private Dictionary<long, PrimitiveAttribute> primitiveAttributes = new Dictionary<long, PrimitiveAttribute>();
        private List<PageHandle> pages = new List<PageHandle>();
        private Dictionary<long, CommentHandle> commentHandles = new Dictionary<long, CommentHandle>();
        private Dictionary<long, RatingHandler> ratingHandles = new Dictionary<long, RatingHandler>();
        private Dictionary<long, LikeHandler> likeHandles = new Dictionary<long, LikeHandler>();
        private Dictionary<long, SubscribeHandler> subscribeHandles = new Dictionary<long, SubscribeHandler>();
        private Dictionary<long, UnsubscribeHandler> unsubscribeHandles = new Dictionary<long, UnsubscribeHandler>();

        /// <summary>
        /// A cache of application entries.
        /// </summary>
        private Dictionary<string, ApplicationEntry> applicationEntryCache = new Dictionary<string, ApplicationEntry>();

        /// <summary>
        /// The applicaton domain from which the web application is executed.
        /// </summary>
        public AppDomain CoreDomain
        {
            get
            {
                return coreDomain;
            }
            internal set
            {
                coreDomain = value;
            }
        }

        /// <summary>
        /// Current path path
        /// </summary>
        public string PagePath
        {
            get
            {
                return pagePath;
            }
            /*internal*/ set /* TODO: make internal*/
            {
                pagePath = value;
            }
        }

        /// <summary>
        /// A collection of the structure of the current page
        /// </summary>
        public GroupCollection PagePathParts
        {
            get
            {
                return pagePathParts;
            }
            internal set
            {
                pagePathParts = value;
            }
        }

        /// <summary>
        /// Current page number for paginated pages
        /// </summary>
        public int PageNo
        {
            get
            {
                return pageNo;
            }
            internal set
            {
                pageNo = value;
            }
        }

        /// <summary>
        /// Gets the http object
        /// </summary>
        public Http Http
        {
            get
            {
                return http;
            }
            internal set
            {
                http = value;
            }
        }

        /// <summary>
        /// Gets the database abstraction object
        /// </summary>
        public Mysql Db
        {
            get
            {
                return db;
            }
        }

        /// <summary>
        /// Gets the page template
        /// </summary>
        public Template Template
        {
            get
            {
                return template;
            }
            internal set
            {
                template = value;
            }
        }

        /// <summary>
        /// Gets the current user session
        /// </summary>
        public SessionState Session
        {
            get
            {
                return session;
            }
            internal set
            {
                session = value;
            }
        }

        public Storage Storage
        {
            get
            {
                return storage;
            }
            internal set
            {
                storage = value;
            }
        }

        /// <summary>
        /// Gets the current timezone
        /// </summary>
        public UnixTime Tz
        {
            get
            {
                return tz;
            }
            internal set
            {
                tz = value;
            }
        }

        /// <summary>
        /// Gets the Language prose class
        /// </summary>
        public Prose Prose
        {
            get
            {
                return prose;
            }
            internal set
            {
                prose = value;
            }
        }

        /// <summary>
        /// Gets the BBcode class
        /// </summary>
        public Bbcode Bbcode
        {
            get
            {
                return bbcode;
            }
            internal set
            {
                bbcode = value;
            }
        }

        /// <summary>
        /// Gets the generic Functions class
        /// </summary>
        public Functions Functions
        {
            get
            {
                return functions;
            }
            internal set
            {
                functions = value;
            }
        }

        /// <summary>
        /// Gets the Display functions class
        /// </summary>
        public Display Display
        {
            get
            {
                return display;
            }
            internal set
            {
                display = value;
            }
        }

        /// <summary>
        /// Gets the Email class
        /// </summary>
        public Email Email
        {
            get
            {
                return email;
            }
            internal set
            {
                email = value;
            }
        }

        /// <summary>
        /// Gets the Ajax Interface
        /// </summary>
        public Ajax Ajax
        {
            get
            {
                return ajax;
            }
            internal set
            {
                ajax = value;
            }
        }

        /// <summary>
        /// Gets the Uri Builder
        /// </summary>
        public Linker Uri
        {
            get
            {
                return uri;
            }
            internal set
            {
                uri = value;
            }
        }

        public Settings Settings
        {
            get
            {
                return applicationSettings;
            }
            internal set
            {
                applicationSettings = value;
            }
        }

        /// <summary>
        /// Creates a new session and associates it with Core
        /// </summary>
        /// <param name="user"></param>
        /// <remarks>Use by Installer to initiate a session into Core</remarks>
        public void CreateNewSession(User user)
        {
            session = new SessionState(this, user);
        }

        /// <summary>
        /// Returns a list of user profiles cached in memory.
        /// </summary>
        public PrimitivesCache PrimitiveCache
        {
            get
            {
                return userProfileCache;
            }
        }

        /// <summary>
        /// Returns items cached in memory.
        /// </summary>
        public NumberedItemsCache ItemCache
        {
            get
            {
                return itemsCache;
            }
        }

        public AccessControlCache AcessControlCache
        {
            get
            {
                return accessControlCache;
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

        public List<PrimitivePermissionGroup> GetPrimitivePermissionGroups(Primitive owner)
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.AddRange(owner.GetPrimitivePermissionGroups());

            foreach (Type type in primitiveTypes.Values)
            {
                if (type.GetMethod(type.Name + "_GetPrimitiveGroups", new Type[] {typeof(Core), typeof(Primitive)}) != null)
                {
                    ppgs.AddRange((List<PrimitivePermissionGroup>)type.InvokeMember(type.Name + "_GetPrimitiveGroups", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { this, owner }));
                }
            }

            return ppgs;
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

        public ApplicationEntry CallingApplication
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                string assemblyName = Assembly.GetCallingAssembly().GetName().Name;

                if (!ApplicationEntries.ContainsKey(assemblyName))
                {
                    LoadApplicationEntry(assemblyName);
                }

                return ApplicationEntries[assemblyName];
            }
        }

        //public void InstallWebConfigSettings()
        //{
        //    Configuration config = WebConfigurationManager.OpenWebConfiguration("~");

            
        //}

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
			
			ItemKey.populateItemTypeCache(this);

            userProfileCache = new PrimitivesCache(this);
            itemsCache = new NumberedItemsCache(this);
            accessControlCache = new AccessControlCache(this);

            AddPrimitiveType(typeof(User));
            AddPrimitiveType(typeof(ApplicationEntry));
            FindAllPrimitivesLoaded();

            RegisterCoreCommentHandles();
            RegisterCoreLikeHandles();
        }

        private void RegisterCoreCommentHandles()
        {
            RegisterCommentHandle(ItemKey.GetTypeId(typeof(StatusMessage)), statusMessageCanPostComment, statusMessageCanDeleteComment, statusMessageAdjustCommentCount, statusMessageCommentPosted, statusMessageCommentDeleted);
        }

        private void RegisterCoreLikeHandles()
        {
            RegisterLikeHandle(ItemKey.GetTypeId(typeof(Comment)), commentLiked);
            RegisterLikeHandle(ItemKey.GetTypeId(typeof(StatusMessage)), statusLiked);
        }

        private void commentLiked(ItemLikedEventArgs e)
        {
            if (e.Likeing == LikeType.Like)
            {
                Db.UpdateQuery(string.Format("UPDATE comments SET comment_likes = comment_likes + {1} WHERE comment_id = {0};",
                    e.ItemId, 1));
            }
        }

        private void statusLiked(ItemLikedEventArgs e)
        {
            if (e.Likeing == LikeType.Like)
            {
                Db.UpdateQuery(string.Format("UPDATE user_status_messages SET status_likes = status_likes + {1} WHERE status_id = {0};",
                    e.ItemId, 1));
            }
        }

        private bool statusMessageCanPostComment(ItemKey itemKey, User member)
        {
            try
            {
                StatusMessage message = new StatusMessage(this, itemKey.Id);

                if (message.Access.Can("COMMENT"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                throw new InvalidItemException();
            }
        }

        private bool statusMessageCanDeleteComment(ItemKey itemKey, User member)
        {
            StatusMessage message = new StatusMessage(this, itemKey.Id);

            if (message.Owner.Id == member.UserId)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void statusMessageCommentPosted(CommentPostedEventArgs e)
        {
        }

        private void statusMessageCommentDeleted(CommentPostedEventArgs e)
        {
        }

        private void statusMessageAdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            Db.UpdateQuery(string.Format("UPDATE user_status_messages SET comments = comments + {1} WHERE status_id = {0};",
                itemKey.Id, adjustment));
        }

        public void DisposeOf()
        {
            userProfileCache = null;
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

        public void InvokeApplication(AppPrimitives primitive, object sender)
        {
            InvokeApplication(primitive, sender, false);
        }

        public void InvokeApplication(AppPrimitives primitive, object sender, bool staticPage)
        {
            LoadApplication(this, sender);

            pages.Sort();
            foreach (PageHandle page in pages)
            {
                if (staticPage == page.StaticPage)
                {
                    if ((page.Primitives & primitive) == primitive || primitive == AppPrimitives.Any)
                    {
                        Regex rex = new Regex(page.Expression, RegexOptions.Compiled);
                        //HttpContext.Current.Response.Write("<br />" + page.Expression + " &nbsp; " + PagePath);
                        Match pathMatch = rex.Match(PagePath);
                        if (pathMatch.Success)
                        {
                            //HttpContext.Current.Response.Write(" **match** ");
                            PagePathParts = pathMatch.Groups;
                            page.Execute(this, sender);
                            return;
                        }
                    }
                }
            }
            Functions.Generate404();
        }

        public bool CanPostComment(ItemKey itemKey)
        {
            if (commentHandles.ContainsKey(itemKey.TypeId))
            {
                return commentHandles[itemKey.TypeId].CanPostComment(itemKey, session.LoggedInMember);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public bool CanDeleteComment(ItemKey itemKey)
        {
            if (commentHandles.ContainsKey(itemKey.TypeId))
            {
                return commentHandles[itemKey.TypeId].CanDeleteComment(itemKey, session.LoggedInMember);
            }
            else
            {
                throw new InvalidItemException();
            }
        }

        public void AdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            if (commentHandles.ContainsKey(itemKey.TypeId))
            {
                commentHandles[itemKey.TypeId].AdjustCommentCount(itemKey, adjustment);
            }
            else
            {
                throw new InvalidItemException();
            }

            ItemInfo ii = null;

            try
            {
                ii = new ItemInfo(this, itemKey);
            }
            catch (InvalidIteminfoException)
            {
                ii = ItemInfo.Create(this, itemKey);
            }

            ii.AdjustComments(adjustment);
            ii.Update();
        }

        public void CommentPosted(ItemKey itemKey, Comment comment, User poster)
        {
            if (commentHandles.ContainsKey(itemKey.TypeId))
            {
                commentHandles[itemKey.TypeId].CommentPosted(comment, poster, itemKey);
            }
            else
            {
                if (!itemKey.ImplementsCommentable)
                {
                    throw new InvalidItemException();
                }
            }
        }

        public void CommentDeleted(ItemKey itemKey, Comment comment, User poster)
        {
            if (commentHandles.ContainsKey(itemKey.TypeId))
            {
                commentHandles[itemKey.TypeId].CommentDeleted(comment, poster, itemKey);
            }
            else
            {
                if (!itemKey.ImplementsCommentable)
                {
                    throw new InvalidItemException();
                }
            }
        }

        public void ItemRated(ItemKey itemKey, int rating, User rater)
        {
            if (ratingHandles.ContainsKey(itemKey.TypeId))
            {
                ratingHandles[itemKey.TypeId](new ItemRatedEventArgs(rating, rater, itemKey));
            }
            else
            {
                if (!itemKey.ImplementsRateable)
                {
                    throw new InvalidItemException();
                }
            }
        }

        public void ItemLiked(ItemKey itemKey, LikeType like, User liker)
        {
            if (likeHandles.ContainsKey(itemKey.TypeId))
            {
                
                likeHandles[itemKey.TypeId](new ItemLikedEventArgs(like, liker, itemKey));
            }
            else
            {
                if (!itemKey.ImplementsLikeable)
                {
                    throw new InvalidItemException();
                }
            }
        }

        public void ItemSubscribed(ItemKey itemKey, User subscriber)
        {
            if (subscribeHandles.ContainsKey(itemKey.TypeId))
            {
                subscribeHandles[itemKey.TypeId](new ItemSubscribedEventArgs(subscriber, itemKey));
            }
            else
            {
                if (!itemKey.ImplementsSubscribeable)
                {
                    throw new InvalidItemException();
                }
            }
        }

        public void ItemUnsubscribed(ItemKey itemKey, User subscriber)
        {
            if (unsubscribeHandles.ContainsKey(itemKey.TypeId))
            {
                unsubscribeHandles[itemKey.TypeId](new ItemUnsubscribedEventArgs(subscriber, itemKey));
            }
            else
            {
                if (!itemKey.ImplementsSubscribeable)
                {
                    throw new InvalidItemException();
                }
            }
        }

        public void RegisterApplicationPage(AppPrimitives primitives, string expression, Core.PageHandler pageHandle, bool staticPage)
        {
            // register with a moderately high priority leaving room for higher priority registration
            // it doesn't matter if two pages have the same priority
            RegisterApplicationPage(primitives, expression, pageHandle, 8, staticPage);
        }

        public void RegisterApplicationPage(AppPrimitives primitives, string expression, Core.PageHandler pageHandle, int order, bool staticPage)
        {
            pages.Add(new PageHandle(primitives, expression, pageHandle, order, staticPage));
        }

        public void RegisterCommentHandle(long itemTypeId, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount)
        {
            RegisterCommentHandle(itemTypeId, canPostComment, canDeleteComment, adjustCommentCount, null, null);
        }

        public void RegisterCommentHandle(long itemTypeId, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount, Core.CommentPostedHandler commentPosted)
        {
            RegisterCommentHandle(itemTypeId, canPostComment, canDeleteComment, adjustCommentCount, commentPosted, null);
        }

        public void RegisterCommentHandle(long itemTypeId, Core.CommentHandler canPostComment, Core.CommentHandler canDeleteComment, Core.CommentCountHandler adjustCommentCount, Core.CommentPostedHandler commentPosted, Core.CommentPostedHandler commentDeleted)
        {
            if (!commentHandles.ContainsKey(itemTypeId))
            {
                commentHandles.Add(itemTypeId, new CommentHandle(itemTypeId, canPostComment, canDeleteComment, adjustCommentCount, commentPosted, commentDeleted));
            }
        }

        public void RegisterRatingHandle(long itemTypeId, Core.RatingHandler itemRated)
        {
            ratingHandles.Add(itemTypeId, itemRated);
        }

        public void RegisterLikeHandle(long itemTypeId, Core.LikeHandler itemliked)
        {
            if (!likeHandles.ContainsKey(itemTypeId))
            {
                likeHandles.Add(itemTypeId, itemliked);
            }
        }

        public void RegisterSubscribeHandle(long itemTypeId, Core.SubscribeHandler itemSubscribed)
        {
            subscribeHandles.Add(itemTypeId, itemSubscribed);
        }

        public void RegisterUnsubscribeHandle(long itemTypeId, Core.UnsubscribeHandler itemUnsubscribed)
        {
            unsubscribeHandles.Add(itemTypeId, itemUnsubscribed);
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
            long primitiveTypeId = ItemKey.GetTypeId(type);

            if (type.IsSubclassOf(typeof(Primitive)))
            {
                foreach (object attr in type.GetCustomAttributes(typeof(PrimitiveAttribute), false))
                {

                    if (primitiveTypeId > 0)
                    {
                        if (!primitiveTypes.ContainsKey(primitiveTypeId))
                        {
                            primitiveAttributes.Add(primitiveTypeId, (PrimitiveAttribute)attr);
                            primitiveTypes.Add(primitiveTypeId, type);
                        }
                        typeAdded = true;
                    }
                }

                if (!typeAdded)
                {
                    if (!primitiveTypes.ContainsKey(primitiveTypeId))
                    {
                        primitiveTypes.Add(primitiveTypeId, type);
                    }
                }
            }
        }

        internal void FindAllPrimitivesLoaded()
        {
            AssemblyName[] assemblies = Assembly.Load(new AssemblyName("BoxSocial.FrontEnd")).GetReferencedAssemblies();

            foreach (AssemblyName an in assemblies)
            {
                try
                {
                    if (an.FullName.StartsWith("BoxSocial.IO"))
                    {
                        continue;
                    }
                    Assembly asm = Assembly.Load(an);
                    Type[] types = asm.GetTypes();
                    foreach (Type type in types)
                    {
                        if (type.IsSubclassOf(typeof(Primitive)))
                        {
                            AddPrimitiveType(type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    HttpContext.Current.Response.Write("Panic, error loading: " + an.FullName);
                    HttpContext.Current.Response.End();
                }
            }
        }

        public Type GetPrimitiveType(long typeId)
        {
            if (primitiveTypes.ContainsKey(typeId))
            {
                return primitiveTypes[typeId];
            }
            else
            {
                return null;
            }
        }

        public PrimitiveAttribute GetPrimitiveAttributes(long typeId)
        {
            if (primitiveAttributes.ContainsKey(typeId))
            {
                return primitiveAttributes[typeId];
            }
            else
            {
                return null;
            }
        }

        public bool IsPrimitiveType(long typeId)
        {
            if (primitiveTypes.ContainsKey(typeId))
            {
                return true;
            }
            else
            {
                return false;
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

        public static void CheckCoreIsNotNull(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }
        }
    }

    public class NullCoreException : Exception
    {
    }
}
