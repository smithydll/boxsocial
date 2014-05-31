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
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum ApplicationLoadOptions : byte
    {
        Key = 0x01,
        Info = Key | 0x02,
        Common = Key | Info,
        All = Key | Info,
    }

    [Cacheable]
    [DataTable("applications", "APPLICATION")]
    [Primitive("APPLICATION", ApplicationLoadOptions.All, "application_id", "application_assembly_name")]
    [Permission("COMMENT", "Can comment on the application", PermissionTypes.Interact)]
    [Permission("RATE", "Can rate the application", PermissionTypes.Interact)]
    [Permission("UPDATE", "Can update the application", PermissionTypes.CreateAndEdit)]
    public class ApplicationEntry : Primitive, ICommentableItem, IPermissibleItem
    {
        [DataField("application_id", DataFieldKeys.Primary)]
        private long applicationId;
        [DataField("user_id")]
        private int creatorId;
        [DataField("application_title", 63)]
        private string title;
        [DataField("application_description", MYSQL_TEXT)]
        private string description;
        [DataField("application_icon", 63)]
        private string applicationIcon;
        [DataField("application_thumb", 63)]
        private string applicationThumb;
        [DataField("application_tile", 63)]
        private string applicationTile;
        [DataField("application_assembly_name", DataFieldKeys.Unique, 63)]
        private string assemblyName;
        [DataField("application_primitive")]
        private bool isPrimitive;
        [DataField("application_primitives")]
        private byte primitives;
        [DataField("application_date_ut")]
        private long dateRaw;
        [DataField("application_comment")]
        private bool usesComments;
        [DataField("application_rating")]
        private bool usesRatings;
        [DataField("application_style")]
        private bool hasStyleSheet;
        [DataField("application_script")]
        private bool hasJavaScript;
        [DataField("application_locked")]
        private bool isLocked;
        [DataField("application_update")]
        private bool updateQueued;
        [DataField("application_simple_permissions")]
        private bool simplePermissions;

        //TODO: USER_APPLICATION_FIELDS
        private ushort permissions;
        private long itemId;

        private List<string> slugExs;
        private List<string> modules;
        private string displayNameOwnership;

        private Access access; // primitive application access rights
        private Assembly assembly;

        public event CommentHandler OnCommentPosted;

        public Assembly Assembly
        {
            get
            {
                if (assembly == null)
                {
                    string assemblyPath;
                    if (IsPrimitive)
                    {
                        if (core.Http != null)
                        {
                            assemblyPath = Path.Combine(core.Http.AssemblyPath, string.Format("{0}.dll", AssemblyName));
                        }
                        else
                        {
                            assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssemblyName + ".dll");
                        }
                    }
                    else
                    {
                        if (core.Http != null)
                        {
                            assemblyPath = Path.Combine(core.Http.AssemblyPath, Path.Combine("applications", string.Format("{0}.dll", AssemblyName)));
                        }
                        else
                        {
                            assemblyPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applications"), AssemblyName + ".dll");
                        }
                    }

                    assembly = Assembly.LoadFrom(assemblyPath);
                }
                return assembly;
            }
        }

        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
        }

        public override long Id
        {
            get
            {
                return applicationId;
            }
        }

        public override string Key
        {
            get
            {
                return assemblyName;
            }
        }

        public override string Type
        {
            get
            {
                return "APPLICATION";
            }
        }

        public override AppPrimitives AppPrimitive
        {
            get
            {
                return AppPrimitives.Application;
            }
        }

        public int CreatorId
        {
            get
            {
                return creatorId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetProperty("title", value);
            }
        }

        public override string DisplayName
        {
            get
            {
                return title;
            }
        }

        public override string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (title != string.Empty) ? title : assemblyName;

                    if (displayNameOwnership.EndsWith("s"))
                    {
                        displayNameOwnership = displayNameOwnership + "'";
                    }
                    else
                    {
                        displayNameOwnership = displayNameOwnership + "'s";
                    }
                }
                return displayNameOwnership;
            }
        }

        public override string TitleNameOwnership
        {
            get
            {
                return "the application " + DisplayNameOwnership;
            }
        }

        public override string TitleName
        {
            get
            {
                return "the application " + DisplayName;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                SetProperty("description", value);
            }
        }

        public string AssemblyName
        {
            get
            {
                return assemblyName;
            }
        }

        public bool IsPrimitive
        {
            get
            {
                return isPrimitive;
            }
            set
            {
                SetProperty("isPrimitive", value);
            }
        }

        public bool IsLocked
        {
            get
            {
                return isLocked;
            }
            set
            {
                SetProperty("isLocked", value);
            }
        }

        public bool UpdateQueued
        {
            get
            {
                return updateQueued;
            }
            set
            {
                SetProperty("updateQueued", value);
            }
        }

        public ushort Permissions
        {
            get
            {
                return permissions;
            }
        }

        public long Comments
        {
            get
            {
                return Info.Comments;
            }
        }

        public List<string> Modules
        {
            get
            {
                return modules;
            }
        }

        public override string Icon
        {
            get
            {
                return applicationIcon;
            }
            /*set
            {
                SetProperty("applicationIcon", value);
            }*/
        }

        public override string Thumbnail
        {
            get
            {
                return applicationThumb;
            }
            /*set
            {
                SetProperty("applicationThumb", value);
            }*/
        }

        public override string Tile
        {
            get
            {
                return applicationTile;
            }
            /*set
            {
                SetProperty("applicationTile", value);
            }*/
        }

        public bool HasIcon
        {
            get
            {
                if (string.IsNullOrEmpty(applicationIcon))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool HasStyleSheet
        {
            get
            {
                return hasStyleSheet;
            }
            set
            {
                SetProperty("hasStyleSheet", value);
            }
        }

        public bool HasJavascript
        {
            get
            {
                return hasJavaScript;
            }
            set
            {
                SetProperty("hasJavaScript", value);
            }
        }

        public AppPrimitives SupportedPrimitives
        {
            get
            {
                return (AppPrimitives)primitives;
            }
            set
            {
                SetProperty("primitives", (byte)value);
            }
        }

        public bool UsesComments
        {
            get
            {
                return usesComments;
            }
            set
            {
                SetProperty("usesComments", value);
            }
        }

        public bool UsesRatings
        {
            get
            {
                return usesRatings;
            }
            set
            {
                SetProperty("usesRatings", value);
            }
        }

        public void LoadSlugEx(string ex)
        {
            if (slugExs == null)
            {
                slugExs = new List<string>();
            }
            slugExs.Add(ex);
        }

        public void AddModule(string module)
        {
            if (modules == null)
            {
                modules = new List<string>();
            }
            modules.Add(module);
        }

        public bool SlugMatch(string uri)
        {
            if (slugExs != null)
            {
                foreach (string slugEx in slugExs)
                {
                    Regex rex = new Regex(slugEx, RegexOptions.Compiled);
                    Match pathMatch = rex.Match(uri);
                    if (pathMatch.Success)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ApplicationEntry(Core core)
            : base(core)
        {
            Assembly asm = Assembly.GetCallingAssembly();

            string assemblyName = asm.GetName().Name;
            assembly = asm;

            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            try
            {
                LoadItem("application_assembly_name", assemblyName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, string assemblyName)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            try
            {
                LoadItem("application_assembly_name", assemblyName);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationException();
            }
        }
		
        public ApplicationEntry(Core core, long applicationId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            try
            {
                LoadItem(applicationId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, DataRow applicationRow, ApplicationLoadOptions loadOptions)
            : this(core, applicationRow)
        {
        }

        public ApplicationEntry(Core core, DataRow applicationRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            loadItemInfo(applicationRow);
        }

        private void ApplicationEntry_ItemLoad()
        {
            OnCommentPosted += new CommentHandler(ApplicationEntry_CommentPosted);
        }

        public void CommentPosted(CommentPostedEventArgs e)
        {
            if (OnCommentPosted != null)
            {
                OnCommentPosted(e);
            }
        }

        bool ApplicationEntry_CommentPosted(CommentPostedEventArgs e)
        {
            return true;
        }
        private void loadApplicationUserInfo(DataRow applicationRow)
        {
            itemId = (long)applicationRow["item_id"];
        }

        public static ApplicationEntry Create(Core core, string assembly, Application application, bool isPrimitive)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(typeof(ApplicationEntry));
            iQuery.AddField("application_assembly_name", assembly);
            iQuery.AddField("user_id", core.LoggedInMemberId);
            iQuery.AddField("application_date_ut", UnixTime.UnixTimeStamp());
            iQuery.AddField("application_title", application.Title);
            iQuery.AddField("application_description", application.Description);
            iQuery.AddField("application_primitive", isPrimitive);
            iQuery.AddField("application_primitives", (byte)application.GetAppPrimitiveSupport());
            iQuery.AddField("application_comment", application.UsesComments);
            iQuery.AddField("application_rating", application.UsesRatings);
            iQuery.AddField("application_style", !string.IsNullOrEmpty(application.StyleSheet));
            iQuery.AddField("application_script", !string.IsNullOrEmpty(application.JavaScript));
            iQuery.AddField("application_icon", string.Format(@"/images/{0}/icon.png", assembly));
            iQuery.AddField("application_thumb", string.Format(@"/images/{0}/thumb.png", assembly));
            iQuery.AddField("application_tile", string.Format(@"/images/{0}/tile.png", assembly));

            long applicationId = core.Db.Query(iQuery);

            ApplicationEntry newApplication = new ApplicationEntry(core, applicationId);

            ApplicationDeveloper developer = ApplicationDeveloper.Create(core, newApplication, core.Session.LoggedInMember);

            try
            {
                ApplicationEntry guestbookAe = core.GetApplication("GuestBook");
                guestbookAe.Install(core, newApplication);
            }
            catch
            {
            }

            return newApplication;
        }

        public override string UriStub
        {
            get
            {
                if (core.Http.Domain != Hyperlink.Domain)
                {
                    return core.Hyperlink.Uri + "application/" + assemblyName + "/";
                }
                else
                {
                    return string.Format("/application/{0}/",
                        assemblyName);
                }
            }
        }

        public override string UriStubAbsolute
        {
            get
            {
                return core.Hyperlink.AppendAbsoluteSid(UriStub);
            }
        }

        public override string Uri
        {
            get
            {
                return core.Hyperlink.AppendSid(UriStub);
            }
        }

        public string GetUri(long typeId, long id)
        {
            if (typeId == ItemKey.GetTypeId(typeof(User)))
            {
                return Uri;
            }
            else
            {
                return core.Hyperlink.AppendSid(string.Format("/application/{0}?type={1}&id={2}",
                    assemblyName, typeId, id));
            }
        }

        public override bool CanModerateComments(User member)
        {
            return false;
        }

        public override bool IsItemOwner(User member)
        {
            if (member != null)
            {
                if (member.UserId == creatorId)
                {
                    return true;
                }
            }
            return false;
        }

        public override ushort GetAccessLevel(User viewer)
        {
            return 0x0001;
        }

        public void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
        {
            if (viewer != null)
            {
                if (viewer.UserId == creatorId)
                {
                    canRead = true;
                    canComment = true;
                    canCreate = true;
                    canChange = true;
                }
                else
                {
                    canRead = true;
                    canComment = true;
                    canCreate = false;
                    canChange = false;
                }
            }
            else
            {
                canRead = true;
                canComment = false;
                canCreate = false;
                canChange = false;
            }
        }

        public bool HasInstalled(Core core, Primitive viewer)
        {
            if (viewer != null)
            {
                SelectQuery query = PrimitiveApplicationInfo.GetSelectQueryStub(typeof(PrimitiveApplicationInfo));
                query.AddCondition("application_id", Id);
                query.AddCondition("item_id", viewer.Id);
                query.AddCondition("item_type_id", viewer.TypeId);

                DataTable viewerTable = core.Db.Query(query);

                if (viewerTable.Rows.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Install(Core core, User owner)
        {
            return Install(core, owner, owner);
        }

        public bool Install(Core core, Primitive owner)
        {
            return Install(core, core.Session.LoggedInMember, owner);
        }

        // bool finaliseTransaction
        public bool Install(Core core, User viewer, Primitive owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if ((owner.AppPrimitive & SupportedPrimitives) != owner.AppPrimitive)
            {
                //HttpContext.Current.Response.Write("<br />Primitive bitmap error.");
                return false;
            }

            if (!HasInstalled(core, owner))
            {
                Application newApplication = Application.GetApplication(core, owner.AppPrimitive, this);

                Dictionary<string, PageSlugAttribute> slugs = newApplication.GetPageSlugs(owner.AppPrimitive);

                if (slugs != null)
                {
                    foreach (string slug in slugs.Keys)
                    {
                        if ((slugs[slug].Primitive & owner.AppPrimitive) == owner.AppPrimitive)
                        {
                            string tSlug = slug;
                            Page myPage = Page.Create(core, false, owner, slugs[slug].PageTitle, ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

                            if (myPage != null)
                            {
                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    if (HasIcon)
                                    {
                                        myPage.Icon = Icon;
                                        try
                                        {
                                            myPage.Update();
                                        }
                                        catch (UnauthorisedToUpdateItemException)
                                        {
                                            Console.WriteLine("Unauthorised");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                newApplication.InitialisePrimitive(owner);

                InsertQuery iQuery = new InsertQuery("primitive_apps");
                iQuery.AddField("application_id", applicationId);
                iQuery.AddField("item_id", owner.Id);
                iQuery.AddField("item_type_id", owner.TypeId);
                iQuery.AddField("app_email_notifications", true);
                // TODO: ACLs

                if (core.Db.Query(iQuery) > 0)
                {
                    return true;
                }
            }
            //HttpContext.Current.Response.Write("<br />Primitive install status error.");
            return false;
        }

        public bool UpdateInstall(Core core, Primitive viewer)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            if (!HasInstalled(core, viewer))
            {
                Install(core, viewer);
            }
            else
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, PageSlugAttribute> slugs = newApplication.GetPageSlugs(viewer.AppPrimitive);

                foreach (string slug in slugs.Keys)
                {
                    if ((slugs[slug].Primitive & viewer.AppPrimitive) == viewer.AppPrimitive)
                    {
                        SelectQuery query = new SelectQuery("user_pages");
                        query.AddFields("page_id");
                        query.AddCondition("page_item_id", viewer.Id);
                        query.AddCondition("page_item_type_id", viewer.TypeId);
                        query.AddCondition("page_title", slugs[slug].PageTitle);
                        query.AddCondition("page_slug", slug);
                        query.AddCondition("page_parent_path", string.Empty);

                        if (core.Db.Query(query).Rows.Count == 0)
                        {
                            string tSlug = slug;
                            Page myPage = Page.Create(core, false, viewer, slugs[slug].PageTitle, ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

                            if (myPage != null)
                            {
                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    if (HasIcon)
                                    {
                                        myPage.Icon = Icon;
                                        try
                                        {
                                            myPage.Update();
                                        }
                                        catch (UnauthorisedToUpdateItemException)
                                        {
                                            Console.WriteLine("Unauthorised");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                Page myPage = new Page(core, viewer, slug, string.Empty);

                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    myPage.Title = slugs[slug].PageTitle;
                                    if (!string.IsNullOrEmpty(Icon))
                                    {
                                        myPage.Icon = Icon;
                                    }

                                    myPage.Update();
                                }
                            }
                            catch (PageNotFoundException)
                            {
                                string tSlug = slug;
                                Page myPage = Page.Create(core, false, viewer, slugs[slug].PageTitle, ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

                                if (viewer is User)
                                {
                                    myPage.Access.Viewer = (User)viewer;
                                }

                                if (myPage.ListOnly)
                                {
                                    if (!string.IsNullOrEmpty(Icon))
                                    {
                                        myPage.Icon = Icon;
                                    }

                                    myPage.Update();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Delete any hanging
                        try
                        {
                            Page myPage = new Page(core, viewer, slug, string.Empty);

                            if (myPage.ListOnly)
                            {
                                myPage.Delete();
                            }
                        }
                        catch (PageNotFoundException)
                        {
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public bool Uninstall(Core core, Primitive viewer)
        {
            return Uninstall(core, viewer, false);
        }

        public bool Uninstall(Core core, Primitive viewer, bool force)
        {
            if (!force)
            {
                if (isPrimitive)
                {
                    // Groups and Networks are primitive applications
                    return false;
                }

                switch (assemblyName.ToLower())
                {
                    case "profile":
                    case "networks":
                    case "groups":
                    case "gallery":
                    case "calendar":
                    case "mail":
                        return false;
                }
            }

            if (HasInstalled(core, viewer))
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, PageSlugAttribute> slugs = newApplication.GetPageSlugs(viewer.AppPrimitive);

                foreach (string slug in slugs.Keys)
                {
                    Page page = new Page(core, viewer, slug, string.Empty);
                    page.Delete();
                }

                DeleteQuery dQuery = new DeleteQuery("primitive_apps");
                dQuery.AddCondition("application_id", applicationId);
                dQuery.AddCondition("item_id", viewer.Id);
                dQuery.AddCondition("item_type_id", viewer.TypeId);

                if (core.Db.Query(dQuery) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = string.Empty;
            string path = string.Format("/application/{0}", assemblyName);

            if (core.IsMobile)
            {
                if (parts.Count > 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        parts[parts.Count - 2][1], path + parts[parts.Count - 2][0].TrimStart(new char[] { '*' }));
                }
                if (parts.Count == 1)
                {
                    output += string.Format("<span class=\"breadcrumbs\"><strong>&#8249;</strong> <a href=\"{1}\">{0}</a></span>",
                        Hyperlink.Domain, core.Hyperlink.AppendSid(path));
                }
            }
            else
            {
                output = string.Format("<a href=\"{1}\">{0}</a>",
                        title, core.Hyperlink.AppendSid(path));

                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i][0] != string.Empty)
                    {
                        path += "/" + parts[i][0];
                        output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                            parts[i][1], core.Hyperlink.AppendSid(path));
                    }
                }
            }

            return output;
        }

        public void SendNotification(Core core, User receiver, string subject, string body, Template emailBody)
        {
            if (canNotify(core, receiver))
            {
                Notification.Create(core, this, receiver, subject, body);

                if (receiver.UserInfo.EmailNotifications)
                {
                    core.Email.SendEmail(receiver.UserInfo.PrimaryEmail, HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(subject))), emailBody);
                }
            }
        }

        public void SendNotification(Core core, User receiver, string subject, string body)
        {
            if (canNotify(core, receiver))
            {
                Notification.Create(core, this, receiver, subject, body);

                if (receiver.UserInfo.EmailNotifications)
                {
                    Template emailTemplate = new Template(core.Http.TemplateEmailPath, "notification.html");

                    emailTemplate.Parse("SITE_TITLE", core.Settings.SiteTitle);
                    emailTemplate.Parse("U_SITE", core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid(core.Hyperlink.BuildHomeUri())));
                    emailTemplate.Parse("TO_NAME", receiver.DisplayName);
                    core.Display.ParseBbcode(emailTemplate, "NOTIFICATION_MESSAGE", body);

                    core.Email.SendEmail(receiver.UserInfo.PrimaryEmail, HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(subject))), emailTemplate);
                }
            }
        }

        private bool canNotify(Core core, User owner)
        {
            SelectQuery query = new SelectQuery("notifications");
            query.AddField(new QueryFunction("notification_id", QueryFunctions.Count, "twentyfour")); //"COUNT(notification_id) as twentyfour");
            query.AddCondition("notification_primitive_id", owner.Id);
            query.AddCondition("notification_primitive_type_id", owner.TypeId);
            query.AddCondition("notification_application", applicationId);
            query.AddCondition("notification_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum ten per application per day
            // TODO: change this
            if ((long)core.Db.Query(query).Rows[0]["twentyfour"] < 20)
            {
                // this used to return true only if e-mail notifications which is should be checked separately
                return true;
            }
            return false;
        }

        public void PublishToFeed(Core core, User owner, IActionableItem item, string description)
        {
            PublishToFeed(core, owner, item, new List<ItemKey>(), description);
        }

        public void PublishToFeed(Core core, User owner, IActionableItem item, ItemKey subItem, string description)
        {
            if (subItem != null)
            {
                PublishToFeed(core, owner, item, new List<ItemKey> { subItem }, description);
            }
            else
            {
                PublishToFeed(core, owner, item, new List<ItemKey>(), description);
            }
        }

        public void PublishToFeed(Core core, User owner, IActionableItem item, List<ItemKey> subItems, string description)
        {
            ItemKey itemKey = item.ItemKey;

            SelectQuery query = new SelectQuery(typeof(Action));
            query.AddField(new QueryFunction("action_id", QueryFunctions.Count, "twentyfour"));
            query.AddCondition("action_primitive_id", owner.ItemKey.Id);
            query.AddCondition("action_primitive_type_id", owner.ItemKey.TypeId);
            query.AddCondition("action_application", applicationId);
            query.AddCondition("action_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum 48 per application per day
            if ((long)core.Db.Query(query).Rows[0]["twentyfour"] < 48)
            {
                /* Post to Twitter, Facebook, individual */
                if ((owner.UserInfo.TwitterSyndicate && owner.UserInfo.TwitterAuthenticated) || (owner.UserInfo.FacebookSyndicate && owner.UserInfo.FacebookAuthenticated))
                {
                    ItemInfo info = item.Info;
                    ItemKey sharedItem = item.ItemKey;

                    if (subItems.Count == 1)
                    {
                        sharedItem = subItems[0];
                        try
                        {
                            info = new ItemInfo(core, subItems[0]);
                        }
                        catch (InvalidIteminfoException)
                        {
                            info = ItemInfo.Create(core, subItems[0]);
                        }
                    }

                    if (sharedItem.ImplementsShareable)
                    {
                        bool publicItem = true;

                        if (item is IPermissibleItem)
                        {
                            IPermissibleItem pitem = (IPermissibleItem)item;
                            publicItem = pitem.Access.IsPublic();
                        }

                        if (item is IPermissibleSubItem)
                        {
                            IPermissibleSubItem pitem = (IPermissibleSubItem)item;
                            publicItem = pitem.PermissiveParent.Access.IsPublic();
                        }

                        if (publicItem)
                        {
                            try
                            {
                                UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
                                uQuery.AddCondition("info_item_id", sharedItem.Id);
                                uQuery.AddCondition("info_item_type_id", sharedItem.TypeId);
                                if (owner.UserInfo.TwitterSyndicate && owner.UserInfo.TwitterAuthenticated)
                                {
                                    string twitterDescription = Functions.TrimStringToWord(description, 140 - 7 - Hyperlink.Domain.Length - 3 - 11 - 1, true);

                                    Twitter t = new Twitter(core.Settings.TwitterApiKey, core.Settings.TwitterApiSecret);
                                    Tweet tweet = t.StatusesUpdate(new TwitterAccessToken(owner.UserInfo.TwitterToken, owner.UserInfo.TwitterTokenSecret), (!string.IsNullOrEmpty(twitterDescription) ? twitterDescription + " " : string.Empty) + info.ShareUri);

                                    uQuery.AddField("info_tweet_id", tweet.Id);
                                    uQuery.AddField("info_tweet_uri", tweet.Uri);
                                }

                                if (owner.UserInfo.FacebookSyndicate && owner.UserInfo.FacebookAuthenticated)
                                {
                                    Facebook fb = new Facebook(core.Settings.FacebookApiAppid, core.Settings.FacebookApiSecret);

                                    FacebookAccessToken token = fb.OAuthAppAccessToken(core, owner.UserInfo.FacebookUserId);
                                    FacebookPost post = fb.StatusesUpdate(token, description, info.ShareUri);

                                    uQuery.AddField("info_facebook_post_id", post.PostId);
                                    
                                }

                                core.Db.Query(uQuery);
                            }
                            catch (System.Net.WebException)
                            {
                                // If Twitter or Facebook are overloaded then we can't update twitter
                            }
                        }
                    }
                }

                /* Post to Box Social feed, coalesce */
                SelectQuery query2 = Action.GetSelectQueryStub(typeof(Action));
                query2.AddCondition("action_primitive_id", owner.ItemKey.Id);
                query2.AddCondition("action_primitive_type_id", owner.ItemKey.TypeId);
                query2.AddCondition("action_item_id", itemKey.Id);
                query2.AddCondition("action_item_type_id", itemKey.TypeId);
                query2.AddCondition("action_application", applicationId);
                query2.AddCondition("action_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);
                query2.AddSort(SortOrder.Descending, "action_time_ut");

                DataTable actionDataTable = core.Db.Query(query2);

                if (actionDataTable.Rows.Count > 0)
                {
                    List<ItemKey> subItemsShortList = new List<ItemKey>();
                    
                    Action action = new Action(core, owner, actionDataTable.Rows[0]);

                    SelectQuery query3 = ActionItem.GetSelectQueryStub(typeof(ActionItem));
                    query3.AddCondition("action_id", action.Id);
                    query3.LimitCount = 3;

                    DataTable actionItemDataTable = core.Db.Query(query3);

                    if (actionItemDataTable.Rows.Count < 3)
                    {
                        for (int i = 0; i < actionItemDataTable.Rows.Count; i++)
                        {
                            subItemsShortList.Add(new ItemKey((long)actionItemDataTable.Rows[i]["item_id"], (long)actionItemDataTable.Rows[i]["item_type_id"]));
                        }

                        for (int i = 0; i < subItems.Count; i++)
                        {
                            if (subItemsShortList.Count == 3) break;
                            subItemsShortList.Add(subItems[i]);
                        }

                        string body = item.GetActionBody(subItemsShortList);
                        string bodyCache = string.Empty;

                        if (!body.Contains("[user") && !body.Contains("sid=true]"))
                        {
                            bodyCache = Bbcode.Parse(HttpUtility.HtmlEncode(body), null, owner, true, string.Empty, string.Empty);
                        }

                        UpdateQuery uquery = new UpdateQuery(typeof(Action));
                        uquery.AddField("action_time_ut", UnixTime.UnixTimeStamp());
                        uquery.AddField("action_title", item.Action);
                        uquery.AddField("action_body", body);
                        uquery.AddField("action_body_cache", bodyCache);

                        if (subItemsShortList != null && subItemsShortList.Count == 1)
                        {
                            uquery.AddField("interact_item_id", subItemsShortList[0].Id);
                            uquery.AddField("interact_item_type_id", subItemsShortList[0].TypeId);
                        }
                        else
                        {
                            uquery.AddField("interact_item_id", itemKey.Id);
                            uquery.AddField("interact_item_type_id", itemKey.TypeId);
                        }

                        uquery.AddCondition("action_id", action.Id);

                        core.Db.Query(uquery);

                        if (subItems != null)
                        {
                            foreach (ItemKey subItem in subItems)
                            {
                                InsertQuery iquery = new InsertQuery(typeof(ActionItem));
                                iquery.AddField("action_id", action.Id);
                                iquery.AddField("item_id", subItem.Id);
                                iquery.AddField("item_type_id", subItem.TypeId);

                                core.Db.Query(iquery);
                            }
                        }
                    }
                }
                else
                {
                    string body = item.GetActionBody(subItems);
                    string bodyCache = string.Empty;

                    if (!body.Contains("[user") && !body.Contains("sid=true]"))
                    {
                        bodyCache = Bbcode.Parse(HttpUtility.HtmlEncode(body), null, owner, true, string.Empty, string.Empty);
                    }

                    InsertQuery iquery = new InsertQuery(typeof(Action));
                    iquery.AddField("action_primitive_id", owner.ItemKey.Id);
                    iquery.AddField("action_primitive_type_id", owner.ItemKey.TypeId);
                    iquery.AddField("action_item_id", itemKey.Id);
                    iquery.AddField("action_item_type_id", itemKey.TypeId);
                    iquery.AddField("action_title", item.Action);
                    iquery.AddField("action_body", body);
                    iquery.AddField("action_body_cache", bodyCache);
                    iquery.AddField("action_application", applicationId);
                    iquery.AddField("action_time_ut", UnixTime.UnixTimeStamp());

                    if (subItems != null && subItems.Count == 1)
                    {
                        iquery.AddField("interact_item_id", subItems[0].Id);
                        iquery.AddField("interact_item_type_id", subItems[0].TypeId);
                    }
                    else
                    {
                        iquery.AddField("interact_item_id", itemKey.Id);
                        iquery.AddField("interact_item_type_id", itemKey.TypeId);
                    }

                    long actionId = core.Db.Query(iquery);

                    if (subItems != null)
                    {
                        foreach (ItemKey subItem in subItems)
                        {
                            iquery = new InsertQuery(typeof(ActionItem));
                            iquery.AddField("action_id", actionId);
                            iquery.AddField("item_id", subItem.Id);
                            iquery.AddField("item_type_id", subItem.TypeId);

                            core.Db.Query(iquery);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns an Action or null
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public Action GetMostRecentFeedAction(Core core, User owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = Action.GetSelectQueryStub(typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.AddCondition("action_application", Id);
            query.AddCondition("action_primitive_id", owner.Id);
            query.AddCondition("action_primitive_type_id", owner.TypeId);
            query.LimitCount = 1;

            DataTable feedTable = core.Db.Query(query);

            if (feedTable.Rows.Count == 1)
            {
                return new Action(core, owner, feedTable.Rows[0]);
            }
            else
            {
                return null;
            }
        }

        public static void ShowPage(Core core, APage page)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            core.Template.SetTemplate("viewapplication.html");
            page.Signature = PageSignature.viewapplication;

            long typeId = core.Functions.RequestLong("type", 0);
            long id = core.Functions.RequestLong("id", 0);

            if (core.Session.IsLoggedIn)
            {
                Primitive viewer = core.Session.LoggedInMember;

                if (typeId > 0)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(id, typeId);
                    viewer = core.PrimitiveCache[id, typeId];
                }

                if (page.AnApplication.HasInstalled(core, viewer))
                {
                    core.Template.Parse("U_UNINSTALL", core.Hyperlink.AppendSid(string.Format("{1}dashboard/applications?mode=uninstall&id={0}",
                        page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
                }
                else
                {
                    core.Template.Parse("U_INSTALL", core.Hyperlink.AppendSid(string.Format("{1}dashboard/applications?mode=install&id={0}",
                        page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
                }

                if (core.Session.LoggedInMember.Id == page.AnApplication.CreatorId)
                {
                    core.Template.Parse("U_MANAGE", core.Hyperlink.AppendSid(page.AnApplication.AccountUriStub));
                }
            }

            User Creator = new User(core, page.AnApplication.CreatorId, UserLoadOptions.All);

            core.Template.Parse("APPLICATION_NAME", page.AnApplication.Title);
            core.Template.Parse("U_APPLICATION", page.AnApplication.Uri);
            core.Template.Parse("DESCRIPTION", page.AnApplication.Description);
            core.Template.Parse("CREATOR_DISPLAY_NAME", Creator.DisplayName);
            if (page.AnApplication.Thumbnail != null)
            {
                core.Template.Parse("I_THUMBNAIL", page.AnApplication.Thumbnail);
            }

            

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Application, page.AnApplication));
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/application/{0}/manage/",
                    Key);
            }
        }

        #region ICommentableItem Members


        public SortOrder CommentSortOrder
        {
            get
            {
                return SortOrder.Descending;
            }
        }

        public byte CommentsPerPage
        {
            get
            {
                return 10;
            }
        }

        #endregion

        public override Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }

                return access;
            }
        }

        public override bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public override List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }

        public override List<PrimitivePermissionGroup> GetPrimitivePermissionGroups()
        {
            List<PrimitivePermissionGroup> ppgs = new List<PrimitivePermissionGroup>();

            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(User)), -1, "L_CREATOR", null, string.Empty));
            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(User)), -2, "L_EVERYONE", null, string.Empty));

            return ppgs;
        }

        public override List<User> GetPermissionUsers()
        {
            List<User> users = new List<User>();

            return users;
        }

        public override List<User> GetPermissionUsers(string namePart)
        {
            List<User> users = new List<User>();

            return users;
        }

        public new IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ItemKey;
            }
        }

        public override bool GetIsMemberOfPrimitive(ItemKey viewer, ItemKey primitiveKey)
        {
            if (primitiveKey.TypeId == ItemType.GetTypeId(typeof(User)))
            {

                switch (primitiveKey.Id)
                {
                    case -1: // OWNER
                        if (CreatorId == viewer.Id)
                        {
                            return true;
                        }
                        break;
                    case -2: // EVERYONE
                        if (core.LoggedInMemberId > 0)
                        {
                            return true;
                        }
                        break;
                }
            }

            if (primitiveKey.TypeId == ItemType.GetTypeId(typeof(ApplicationEntry)))
            {
                if (primitiveKey.Id == Id)
                {
                    if (CreatorId == viewer.Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool CanEditPermissions()
        {
            if (CreatorId == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool CanEditItem()
        {
            if (CreatorId == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool CanDeleteItem()
        {
            if (CreatorId == core.LoggedInMemberId)
            {
                return true;
            }

            return false;
        }

        public override bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public override string DisplayTitle
        {
            get
            {
                return "Application: " + Title;
            }
        }

        public override string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }

        public static ItemKey ApplicationDevelopersGroupKey
        {
            get
            {
                return new ItemKey(-1, ItemType.GetTypeId(typeof(ApplicationDeveloper)));
            }
        }

        public string Noun
        {
            get
            {
                return "guest book";
            }
        }

        public override string CoverPhoto
        {
            get
            {
                return string.Empty;
            }
        }

        public override string MobileCoverPhoto
        {
            get
            {
                return string.Empty;
            }
        }
    }

    public class InvalidApplicationException : Exception
    {
    }
}
