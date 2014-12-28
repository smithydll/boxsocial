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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
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
        private SmsGateway sms;
        private Ajax ajax;
        private Hyperlink hyperlink;
        private Settings applicationSettings;
        private Storage storage;
        private JobQueue queue;
        private Search search;
        private BoxSocial.IO.Cache cache;
        private List<Emoticon> emoticons;

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
        public event HookHandler PrimitiveHeadHooks;
        public event HookHandler FootHooks;
        public event HookHandler PageHooks;
        public event HookHandler PostHooks;
        public event LoadHandler LoadApplication;
        public event PermissionGroupHandler primitivePermissionGroupHook;

        private Dictionary<long, ItemType> primitiveTypes = new Dictionary<long, ItemType>(8);
        private Dictionary<long, Type> primitiveTypeCache = new Dictionary<long, Type>(8);
        private Dictionary<long, PrimitiveAttribute> primitiveAttributes = new Dictionary<long, PrimitiveAttribute>(8);
        private List<PageHandle> pages = new List<PageHandle>(16);
        private Dictionary<long, SubscribeHandler> subscribeHandles = new Dictionary<long, SubscribeHandler>();
        private Dictionary<long, UnsubscribeHandler> unsubscribeHandles = new Dictionary<long, UnsubscribeHandler>();

        private Dictionary<string, string> meta = new Dictionary<string, string>(4, StringComparer.Ordinal);

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
        private Dictionary<string, ApplicationEntry> applicationEntryCache = new Dictionary<string, ApplicationEntry>(16, StringComparer.Ordinal);

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

        public string TemplatePath
        {
            get
            {
                if (Http == null)
                {
                    if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates")))
                    {
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");
                    }
                    else
                    {
                        return Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, "templates");
                    }
                }
                else
                {
                    return Http.TemplatePath;
                }
            }
        }

        public string TemplateEmailPath
        {
            get
            {
                if (Http == null)
                {
                    if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates")))
                    {
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", "emails");
                    }
                    else
                    {
                        return Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, "templates", "emails");
                    }
                }
                else
                {
                    return Http.TemplateEmailPath;
                }
            }
        }

        public string LanguagePath
        {
            get
            {
                if (Http == null)
                {
                    if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language")))
                    {
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language");
                    }
                    else
                    {
                        return Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, "language");
                    }
                }
                else
                {
                    return Http.LanguagePath;
                }
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
                if (HttpContext.Current != null && HttpContext.Current.Request != null && http == null)
                {
                    http = new Http();
                }
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
                        storage = new RackspaceCloudFiles(WebConfigurationManager.AppSettings["rackspace-key"], WebConfigurationManager.AppSettings["rackspace-username"], db);

                        string location = WebConfigurationManager.AppSettings["rackspace-location"];
                        if (!string.IsNullOrEmpty(location))
                        {
                            ((RackspaceCloudFiles)storage).SetLocation(location);
                        }
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

        public JobQueue Queue
        {
            get
            {
                if (queue == null)
                {
                    switch (WebConfigurationManager.AppSettings["queue-provider"])
                    {
                        case "amazon_sqs":
                            queue = new AmazonSQS(WebConfigurationManager.AppSettings["amazon-key-id"], WebConfigurationManager.AppSettings["amazon-secret-key"]);
                            break;
                        case "rackspace":
                            queue = new RackspaceCloudQueues(WebConfigurationManager.AppSettings["rackspace-key"], WebConfigurationManager.AppSettings["rackspace-username"]);

                            string location = WebConfigurationManager.AppSettings["rackspace-location"];
                            if (!string.IsNullOrEmpty(location))
                            {
                                ((RackspaceCloudQueues)queue).SetLocation(location);
                            }
                            break;
                        case "database":
                        default:
                            //queue = new DatabaseQueue(db);
                            break;
                    }
                }

                return queue;
            }
        }

        public Search Search
        {
            get
            {
                if (search == null)
                {
                    if (Settings.SearchProvider == "solr")
                    {
                        search = new SolrSearch(this, WebConfigurationManager.AppSettings["solr-server"]);
                    }
                    else
                    {
                        search = new LuceneSearch(this);
                    }
                }
                return search;
            }
        }

        public void CloseSearch()
        {
            if (search != null)
            {
                search.Dispose();
                search = null;
            }
        }

        public BoxSocial.IO.Cache Cache
        {
            get
            {
                if (cache == null)
                {
                    if (Settings.SearchProvider == "memcached")
                    {
                        cache = new Memcached();
                    }
                    else
                    {
                        cache = new LocalCache(Http);
                    }
                }
                return cache;
            }
        }

        public List<Emoticon> Emoticons
        {
            get
            {
                if (emoticons == null)
                {
                    object o = Cache.GetCached("Emoticons");

                    List<HibernateItem> cachedEmoticons = null;

                    if (o != null && o is List<HibernateItem>)
                    {
                        cachedEmoticons = (List<HibernateItem>)o;
                        emoticons = new List<Emoticon>();

                        foreach (HibernateItem item in cachedEmoticons)
                        {
                            emoticons.Add(new Emoticon(this, item));
                        }

                        return emoticons;
                    }

                    cachedEmoticons = new List<HibernateItem>();
                    emoticons = new List<Emoticon>();

                    SelectQuery query = Emoticon.GetSelectQueryStub(this, typeof(Emoticon));
                    System.Data.Common.DbDataReader emoticonsReader = db.ReaderQuery(query);

                    while(emoticonsReader.Read())
                    {
                        emoticons.Add(new Emoticon(this, emoticonsReader));
                        cachedEmoticons.Add(new HibernateItem(emoticonsReader));
                    }

                    emoticonsReader.Close();
                    emoticonsReader.Dispose();

                    if (Cache != null)
                    {
                        Cache.SetCached("Emoticons", cachedEmoticons, new TimeSpan(12, 0, 0), CacheItemPriority.High);
                    }
                }
                return emoticons;
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
                if (prose == null)
                {
                    prose = new Prose();
                    prose.Initialise(this, "en");
                }
                return prose;
            }
            internal set
            {
                prose = value;
            }
        }

        public void CloseProse()
        {
            if (prose != null)
            {
                prose.Close();
                prose = null;
            }
        }

        public void CloseCache()
        {
            if (cache != null)
            {
                cache.Close();
                cache = null;
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

        public SmsGateway Sms
        {
            get
            {
                if (sms == null)
                {
                    if (Settings.SmsProvider == "http")
                    {
                        sms = new HttpSmsGateway(Settings.SmsHttpGateway);
                    }
                }
                return sms;
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

        private Dictionary<string, ItemKey> loadedAssemblies = null;

        public ApplicationEntry GetApplication(long applicationId)
        {
            ItemKey ik = new ItemKey(applicationId, ItemType.GetTypeId(this, typeof(ApplicationEntry)));
            ItemCache.RequestItem(ik); // Not normally needed, but in-case the persisted NumberedItems cache is purged
            ApplicationEntry ae = (ApplicationEntry)ItemCache[ik];

            if (Prose != null)
            {
                Prose.AddApplication(ae.Key);
            }

            return ae;
        }

        public ApplicationEntry GetApplication(string name)
        {
            loadAssemblies();

            if (loadedAssemblies.ContainsKey(name))
            {
                ItemKey ik = loadedAssemblies[name];
                ItemCache.RequestItem(ik); // Not normally needed, but in-case the persisted NumberedItems cache is purged
                ApplicationEntry ae = (ApplicationEntry)ItemCache[ik];

                if (Prose != null && ae.ApplicationType == ApplicationType.Native)
                {
                    Prose.AddApplication(ae.Key);
                }

                return ae;
            }
            else
            {
                ApplicationEntry ae = new ApplicationEntry(this, name);

                if (loadedAssemblies != null)
                {
                    if (!loadedAssemblies.ContainsKey(name))
                    {
                        loadedAssemblies.Add(name, ae.ItemKey);
                    }

                    Cache.SetCached("Applications", loadedAssemblies, new TimeSpan(1, 0, 0), CacheItemPriority.Default);
                }

                if (Prose != null && ae.ApplicationType == ApplicationType.Native)
                {
                    Prose.AddApplication(ae.Key);
                }

                return ae;
            }
        }

        public List<string> GetLoadedAssemblyNames()
        {
            loadAssemblies();

            List<string> keys = new List<string>();
            foreach (string key in loadedAssemblies.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }

        public List<Assembly> GetLoadedAssemblies()
        {
            loadAssemblies();

            List<Assembly> asms = new List<Assembly>();

            foreach (string key in loadedAssemblies.Keys)
            {
                ItemKey ik = loadedAssemblies[key];
                ItemCache.RequestItem(ik); // Not normally needed, but in-case the persisted NumberedItems cache is purged
                asms.Add(((ApplicationEntry)ItemCache[ik]).Assembly);
            }

            return asms;
        }

        private void loadAssemblies()
        {
            if (loadedAssemblies == null)
            {
                object o = Cache.GetCached("Applications");

                if (o != null && o is Dictionary<string, ItemKey>)
                {
                    loadedAssemblies = (Dictionary<string, ItemKey>)o;
                }
                else
                {
                    loadedAssemblies = new Dictionary<string, ItemKey>(16, StringComparer.Ordinal);
                }

                AssemblyName[] assemblies = Assembly.Load(new AssemblyName("BoxSocial.FrontEnd")).GetReferencedAssemblies();
                List<string> applicationNames = new List<string>();

                foreach (AssemblyName an in assemblies)
                {
                    if (!loadedAssemblies.ContainsKey(an.Name))
                    {
                        applicationNames.Add(an.Name);
                    }
                }

                SelectQuery query = Item.GetSelectQueryStub(this, typeof(ApplicationEntry));
                query.AddCondition("application_assembly_name", ConditionEquality.In, applicationNames);

                System.Data.Common.DbDataReader applicationReader = db.ReaderQuery(query);

                ItemCache.RegisterType(typeof(ApplicationEntry));

                while (applicationReader.Read())
                {
                    ApplicationEntry ae = new ApplicationEntry(this, applicationReader);
                    ItemCache.RegisterItem(ae);
                    loadedAssemblies.Add(ae.AssemblyName, ae.ItemKey);

                    if (Prose != null)
                    {
                        Prose.AddApplication(ae.Key);
                    }
                }

                applicationReader.Close();
                applicationReader.Dispose();

                if (loadedAssemblies != null)
                {
                    Cache.SetCached("Applications", loadedAssemblies, new TimeSpan(1, 0, 0), CacheItemPriority.Default);
                }
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
                return new ItemKey(User.GetMemberId(session.LoggedInMember), ItemKey.GetTypeId(this, typeof(User)));
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
                if (page != null)
                {
                    return page.IsMobile;
                }
                else
                {
                    return false;
                }
            }
        }

        public Forms.DisplayMedium Medium
        {
            get
            {
                if (page != null)
                {
                    return page.Medium;
                }
                else
                {
                    return Forms.DisplayMedium.Desktop;
                }
            }
        }


        /// <summary>
        /// Loads the application entry for the calling application
        /// </summary>
        internal void LoadApplicationEntry(string assemblyName)
        {
            applicationEntryCache.Add(assemblyName, GetApplication(assemblyName));
        }

        public Core(TPage page, Mysql db, Template template)
        {
            HeadHooks += new HookHandler(Core_HeadHooks);
            PrimitiveHeadHooks += new HookHandler(Core_PrimitiveHeadHooks);
            FootHooks +=new HookHandler(Core_FootHooks);
            PageHooks += new HookHandler(Core_Hooks);
            PostHooks += new HookHandler(Core_PostHooks);
            LoadApplication += new LoadHandler(Core_LoadApplication);

            this.page = page;
            this.db = db;
            this.template = template;
			
			ItemKey.populateItemTypeCache(this);
            //QueryCache.populateQueryCache();

            userProfileCache = new PrimitivesCache(this);
            itemsCache = new NumberedItemsCache(this);
            accessControlCache = new AccessControlCache(this);

            primitiveTypes = ItemKey.GetPrimitiveTypes(this);
        }

        public Core(OPage page, Mysql db)
        {
            LoadApplication += new LoadHandler(Core_LoadApplication);

            this.db = db;

            ItemKey.populateItemTypeCache(this);
            //QueryCache.populateQueryCache();

            userProfileCache = new PrimitivesCache(this);
            itemsCache = new NumberedItemsCache(this);
            accessControlCache = new AccessControlCache(this);

            primitiveTypes = ItemKey.GetPrimitiveTypes(this);
        }

        public Core(Mysql db)
        {
            this.db = db;
            ItemKey.populateItemTypeCache(this);

            userProfileCache = new PrimitivesCache(this);
            itemsCache = new NumberedItemsCache(this);
            accessControlCache = new AccessControlCache(this);

            primitiveTypes = ItemKey.GetPrimitiveTypes(this);
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

        void Core_PrimitiveHeadHooks(HookEventArgs eventArgs)
        {

        }

        void Core_FootHooks(HookEventArgs eventArgs)
        {

        }

        void Core_PostHooks(HookEventArgs e)
        {

        }

        void Core_Hooks(HookEventArgs eventArgs)
        {
            
        }

        public void InvokeHeadHooks(HookEventArgs eventArgs)
        {
            HeadHooks(eventArgs);
        }

        public void InvokePrimitiveHeadHooks(HookEventArgs eventArgs)
        {
            PrimitiveHeadHooks(eventArgs);
        }

        public void InvokeFootHooks(HookEventArgs eventArgs)
        {
            FootHooks(eventArgs);
        }

        public void InvokePostHooks(HookEventArgs eventArgs)
        {
            PostHooks(eventArgs);
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

#if DEBUG
            Stopwatch httpTimer = new Stopwatch();
            httpTimer.Start();
#endif
            pages.Sort();
            foreach (PageHandle page in pages)
            {
                if (staticPage == page.StaticPage)
                {
                    if ((page.Primitives & primitive) == primitive || primitive == AppPrimitives.Any)
                    {
                        Regex rex = new Regex(page.Expression);
                        //HttpContext.Current.Response.Write("<br />" + page.Expression + " &nbsp; " + PagePath);
                        Match pathMatch = rex.Match(PagePath);
                        if (pathMatch.Success)
                        {
                            //HttpContext.Current.Response.Write(" **match** ");
                            PagePathParts = pathMatch.Groups;
#if DEBUG
                            httpTimer.Stop();
                            HttpContext.Current.Response.Write(string.Format("<!-- Invoke {1} in {0} -->\r\n", httpTimer.ElapsedTicks / 10000000.0, PagePath));
#endif
                            page.Execute(this, sender);
                            return;
                        }
                    }
                }
            }
            Functions.Generate404();
        }

        public bool InvokeJob(Job job)
        {
            if (job.ApplicationId == 0)
            {
                switch (job.Function)
                {
                    case "publishTweet":
                        return Twitter.PublishTweet(this, job);
                    case "publishTumblr":
                        return Tumblr.PublishPost(this, job);
                    case "publishFacebook":
                        return Facebook.PublishPost(this, job);
                }
            }
            else
            {
                ApplicationEntry ae = GetApplication(job.ApplicationId);

                Application jobApplication = Application.GetApplication(this, AppPrimitives.Any, ae);

                if (jobApplication != null)
                {
                    return jobApplication.ExecuteJob(job);
                }
            }

            return false;
        }

        public void InvokeApplicationCall(ApplicationEntry ae, string callName)
        {
            if (ae == null)
            {
                // Internal calls

                switch (callName)
                {
                    case "feed":
                        break;
                }
            }
            else
            {
                Application callApplication = Application.GetApplication(this, AppPrimitives.Any, ae);

                if (callApplication != null)
                {
                    callApplication.ExecuteCall(callName);
                }
            }
        }

        public void AdjustCommentCount(ItemKey itemKey, int adjustment)
        {
            ItemInfo ii = null;

            if (itemKey.GetType(this).Commentable)
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
                if (!itemKey.GetType(this).Subscribeable)
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
                if (!itemKey.GetType(this).Subscribeable)
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

        private VariableCollection createPrimitiveHeadPanel()
        {
            return template.CreateChild("primitive_head_hook");
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

        private VariableCollection createPostPanel()
        {
            return template.CreateChild("app_panel_post");
        }

        public void AddHeadPanel(Template t)
        {
            VariableCollection panelVariableCollection = createHeadPanel();

            panelVariableCollection.ParseRaw("BODY", t.ToString());
        }

        public void AddPrimitiveHeadPanel(Template t)
        {
            VariableCollection panelVariableCollection = createPrimitiveHeadPanel();

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

        public void AddPostPanel(string title, Template t)
        {
            VariableCollection panelVariableCollection = createPostPanel();

            panelVariableCollection.ParseRaw("TITLE", title);
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
                        ItemKey applicationKey = new ItemKey(iType.ApplicationId, ItemType.GetTypeId(this, typeof(ApplicationEntry)));
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
                        Assembly assembly = Application.LoadedAssemblies[ae.Id];

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

        private List<long> loadedApplicationIds = new List<long>();
        internal bool LoadedApplication(ApplicationEntry ae)
        {
            if (loadedApplicationIds.Contains(ae.Id))
            {
                return true;
            }
            else
            {
                loadedApplicationIds.Add(ae.Id);
                return false;
            }
        }
    }

    public class NullCoreException : Exception
    {
    }
}
