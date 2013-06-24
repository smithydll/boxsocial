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
using System.Configuration;
using System.Data;
using System.IO;
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
        private Hyperlink hyperlink;
        private Settings applicationSettings;
        private Storage storage;
        private Search search;

        internal TPage page;

        private PrimitivesCache userProfileCache;
        private NumberedItemsCache itemsCache;
        private AccessControlCache accessControlCache;

        public delegate void HookHandler(HookEventArgs e);
        public delegate void LoadHandler(Core core, object sender);
        public delegate void PageHandler(Core core, object sender);
        public delegate void SubscribeHandler(ItemSubscribedEventArgs e);
        public delegate void UnsubscribeHandler(ItemUnsubscribedEventArgs e);
        public delegate List<PrimitivePermissionGroup> PermissionGroupHandler(Core core, Primitive owner);

        public event HookHandler HeadHooks;
        public event HookHandler FootHooks;
        public event HookHandler PageHooks;
        public event LoadHandler LoadApplication;
        public event PermissionGroupHandler primitivePermissionGroupHook;

        private Dictionary<long, ItemType> primitiveTypes = new Dictionary<long, ItemType>();
        private Dictionary<long, Type> primitiveTypeCache = new Dictionary<long, Type>();
        private Dictionary<long, PrimitiveAttribute> primitiveAttributes = new Dictionary<long, PrimitiveAttribute>();
        private List<PageHandle> pages = new List<PageHandle>();
        private Dictionary<long, SubscribeHandler> subscribeHandles = new Dictionary<long, SubscribeHandler>();
        private Dictionary<long, UnsubscribeHandler> unsubscribeHandles = new Dictionary<long, UnsubscribeHandler>();

        private Dictionary<string, string> meta = new Dictionary<string, string>(StringComparer.Ordinal);

        public static bool IsUnix
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }

        public Dictionary<string, string> Meta
        {
            get
            {
                return meta;
            }
            set
            {
                meta = value;
            }
        }

        /// <summary>
        /// A cache of application entries.
        /// </summary>
        private Dictionary<string, ApplicationEntry> applicationEntryCache = new Dictionary<string, ApplicationEntry>(StringComparer.Ordinal);

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
        /// Current page numbers for paginated pages
        /// </summary>
        public int[] PageNumber
        {
            get
            {
                return page.PageNumber;
            }
        }

        /// <summary>
        /// Current top level page number for paginated pages
        /// </summary>
        public int TopLevelPageNumber
        {
            get
            {
                return page.TopLevelPageNumber;
            }
        }

        public int CommentPageNumber
        {
            get
            {
                return page.CommentPageNumber;
            }
        }

        public long[] PageOffset
        {
            get
            {
                return page.PageOffset;
            }
        }

        public long TopLevelPageOffset
        {
            get
            {
                return page.TopLevelPageOffset;
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
                if (storage == null)
                {
                    if (Settings.StorageProvider == "amazon_s3")
                    {
                        storage = new AmazonS3(WebConfigurationManager.AppSettings["amazon-key-id"], WebConfigurationManager.AppSettings["amazon-secret-key"], db);
                    }
                    else if (Settings.StorageProvider == "rackspace")
                    {
                        storage = new Rackspace(WebConfigurationManager.AppSettings["rackspace-key"], WebConfigurationManager.AppSettings["rackspace-username"], db);
                    }
                    else if (Settings.StorageProvider == "azure")
                    {
                        // provision: not supported
                    }
                    else if (Settings.StorageProvider == "local")
                    {
                        storage = new LocalStorage(Settings.StorageRootUserFiles, db);
                    }
                    else
                    {
                        storage = new LocalStorage(WebConfigurationManager.AppSettings["storage-path"], db);
                    }
                }
                return storage;
            }
        }

        public Search Search
        {
            get
            {
                if (search == null)
                {
                    search = new LuceneSearch(this);
                }
                return search;
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
                if (bbcode == null)
                {
                    bbcode = new Bbcode(this);
                }
                return bbcode;
            }
        }

        /// <summary>
        /// Gets the generic Functions class
        /// </summary>
        public Functions Functions
        {
            get
            {
                if (functions == null)
                {
                    functions = new Functions(this);
                }
                return functions;
            }
        }

        /// <summary>
        /// Gets the Display functions class
        /// </summary>
        public Display Display
        {
            get
            {
                if (display == null)
                {
                    display = new Display(this);
                }
                return display;
            }
        }

        /// <summary>
        /// Gets the Email class
        /// </summary>
        public Email Email
        {
            get
            {
                if (email == null)
                {
                    if (Settings.MailProvider == "smtp")
                    {
                        email = new Smtp(WebConfigurationManager.AppSettings["boxsocial-host"], WebConfigurationManager.AppSettings["smtp-server"], LoggedInMemberId, (Session.IsLoggedIn ? Session.LoggedInMember.UserName : string.Empty), Session.IPAddress.ToString(), WebConfigurationManager.AppSettings["email"], Settings.SiteTitle);
                    }
                    else if (Settings.MailProvider == "mailgun")
                    {
                        email = new Mailgun(WebConfigurationManager.AppSettings["mailgun-uri"], WebConfigurationManager.AppSettings["mailgun-apikey"], WebConfigurationManager.AppSettings["mailgun-domain"], WebConfigurationManager.AppSettings["email"], Settings.SiteTitle);
                    }
                }
                return email;
            }
        }

        /// <summary>
        /// Gets the Ajax Interface
        /// </summary>
        public Ajax Ajax
        {
            get
            {
                if (ajax == null)
                {
                    ajax = new Ajax(this);
                }
                return ajax;
            }
        }

        /// <summary>
        /// Gets the Hyperlink Builder
        /// </summary>
        public Hyperlink Hyperlink
        {
            get
            {
                if (hyperlink == null)
                {
                    hyperlink = new Hyperlink(this);
                }
                return hyperlink;
            }
        }

        public Settings Settings
        {
            get
            {
                if (applicationSettings == null)
                {
                    applicationSettings = new Settings(this);
                }
                return applicationSettings;
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

        public Dictionary<string, long> LoadUserProfiles(List<string> usernames)
        {
            return userProfileCache.LoadUserProfiles(usernames);
        }

        public List<PrimitivePermissionGroup> GetPrimitivePermissionGroups(Primitive owner)
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.AddRange(owner.GetPrimitivePermissionGroups());

            foreach (long typeId in primitiveTypes.Keys)
            {
                Type type = this.GetPrimitiveType(typeId);
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

        public ItemKey LoggedInMemberItemKey
        {
            get
            {
                return new ItemKey(User.GetMemberId(session.LoggedInMember), typeof(User));
            }
        }

        public bool IsAjax
        {
            get
            {
                return page.IsAjax;
            }
        }

        public bool IsMobile
        {
            get
            {
                return page.IsMobile;
            }
        }


        /// <summary>
        /// Loads the application entry for the calling application
        /// </summary>
        internal void LoadApplicationEntry(string assemblyName)
        {
            applicationEntryCache.Add(assemblyName, new ApplicationEntry(this, session.LoggedInMember, assemblyName));
        }

        public Core(TPage page, Mysql db, Template template)
        {
            HeadHooks += new HookHandler(Core_HeadHooks);
            FootHooks +=new HookHandler(Core_FootHooks);
            PageHooks += new HookHandler(Core_Hooks);
            LoadApplication += new LoadHandler(Core_LoadApplication);

            this.page = page;
            this.db = db;
            this.template = template;
			
			ItemKey.populateItemTypeCache(this);
            QueryCache.populateQueryCache();

            userProfileCache = new PrimitivesCache(this);
            itemsCache = new NumberedItemsCache(this);
            accessControlCache = new AccessControlCache(this);

            primitiveTypes = ItemKey.PrimitiveTypes;
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

        public void AdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            ItemInfo ii = null;

            if (itemKey.ImplementsCommentable)
            {
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
            else
            {
                throw new InvalidItemException();
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

        public Type GetPrimitiveType(long typeId)
        {
            ItemType iType = null;
            if (primitiveTypes.TryGetValue(typeId, out iType))
            {
                Type rType = null;
                if (primitiveTypeCache.TryGetValue(typeId, out rType))
                {
                    return rType;
                }
                else
                {
                    Type tType = null;

                    if (iType.ApplicationId > 0)
                    {
                        ItemCache.RegisterType(typeof(ApplicationEntry));
                        ItemKey applicationKey = new ItemKey(iType.ApplicationId, typeof(ApplicationEntry));
                        ItemCache.RequestItem(applicationKey);
                        //ApplicationEntry ae = new ApplicationEntry(core, ik.ApplicationId);
                        ApplicationEntry ae = (ApplicationEntry)ItemCache[applicationKey];

                        //Application a = BoxSocial.Internals.Application.GetApplication(core, AppPrimitives.Any, ae);
                        string assemblyPath;
                        if (ae.IsPrimitive)
                        {
                            if (Http != null)
                            {
                                assemblyPath = Path.Combine(Http.AssemblyPath, string.Format("{0}.dll", ae.AssemblyName));
                            }
                            else
                            {
                                assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ae.AssemblyName + ".dll");
                            }
                        }
                        else
                        {
                            if (Http != null)
                            {
                                assemblyPath = Path.Combine(Http.AssemblyPath, Path.Combine("applications", string.Format("{0}.dll", ae.AssemblyName)));
                            }
                            else
                            {
                                assemblyPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applications"), ae.AssemblyName + ".dll");
                            }
                        }
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);

                        tType = assembly.GetType(iType.TypeNamespace);
                    }
                    else
                    {
                        tType = Type.GetType(iType.TypeNamespace);
                    }
                    if (tType != null)
                    {
                        primitiveTypeCache.Add(typeId, tType);
                        return tType;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }

        public PrimitiveAttribute GetPrimitiveAttributes(long typeId)
        {
            PrimitiveAttribute pa = null;
            if (primitiveAttributes.TryGetValue(typeId, out pa))
            {
                return pa;
            }
            else
            {
                if (typeId > 0)
                {
                    Type type = GetPrimitiveType(typeId);
                    if (type != null)
                    {
                        foreach (object attr in type.GetCustomAttributes(typeof(PrimitiveAttribute), false))
                        {
                            primitiveAttributes.Add(typeId, (PrimitiveAttribute)attr);
                            return (PrimitiveAttribute)attr;
                        }
                    }
                }

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
