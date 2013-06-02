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
        [DataField("application_comments")]
        private long comments;
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

        private Primitive owner; // primitive installed the application
        private Access access; // primitive application access rights

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
                return comments;
            }
        }

        public List<string> Modules
        {
            get
            {
                return modules;
            }
        }

        public string Icon
        {
            get
            {
                return applicationIcon;
            }
            set
            {
                SetProperty("applicationIcon", value);
            }
        }

        public string Thumbnail
        {
            get
            {
                return applicationThumb;
            }
            set
            {
                SetProperty("applicationThumb", value);
            }
        }

        public string Tile
        {
            get
            {
                return applicationTile;
            }
            set
            {
                SetProperty("applicationTile", value);
            }
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
            this.owner = core.Session.LoggedInMember;

            Assembly asm = Assembly.GetCallingAssembly();

            string assemblyName = asm.GetName().Name;

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

        /*public ApplicationEntry(Core core, ApplicationCommentType act)
            : base(core)
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(ApplicationEntry));
            query.AddJoin(JoinTypes.Inner, "comment_types", "application_id", "application_id");
            query.AddCondition("type_type", act.Type);

            DataTable assemblyTable = db.Query(query);

            if (assemblyTable.Rows.Count == 1)
            {
                loadItemInfo(assemblyTable.Rows[0]);

                if (this.owner == null)
                {
                    this.owner = new User(core, creatorId);
                }
            }
            else
            {
                throw new InvalidApplicationException();
            }
        }*/

        public ApplicationEntry(Core core, Primitive owner, string assemblyName)
            : base(core)
        {
            this.owner = owner;

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

        public ApplicationEntry(Core core, Primitive owner, long applicationId)
            : base(core)
        {
            this.owner = owner;

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

        public ApplicationEntry(Core core, Primitive installee, DataRow applicationRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            loadItemInfo(applicationRow);

            // TODO: change this
            loadApplicationUserInfo(applicationRow);

            if (installee != null)
            {
                access = new Access(core, this);
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
                ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
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
                    return Hyperlink.Uri + "application/" + assemblyName + "/";
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

        public bool HasInstalled(Primitive viewer)
        {
            if (viewer != null)
            {
                SelectQuery query = PrimitiveApplicationInfo.GetSelectQueryStub(typeof(PrimitiveApplicationInfo));
                query.AddCondition("application_id", Id);
                query.AddCondition("item_id", viewer.Id);
                query.AddCondition("item_type_id", viewer.TypeId);

                DataTable viewerTable = db.Query(query);

                if (viewerTable.Rows.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /*public bool Install(Core core, Primitive viewer)
        {
            return Install(core, viewer, true);
        }*/

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

            if (!HasInstalled(owner))
            {
                Application newApplication = Application.GetApplication(core, owner.AppPrimitive, this);

                Dictionary<string, string> slugs = newApplication.GetPageSlugs(owner.AppPrimitive);

                if (slugs != null)
                {
                    foreach (string slug in slugs.Keys)
                    {
                        string tSlug = slug;
                        Page myPage = Page.Create(core, false, owner, slugs[slug], ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);

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

                newApplication.InitialisePrimitive(owner);

                InsertQuery iQuery = new InsertQuery("primitive_apps");
                iQuery.AddField("application_id", applicationId);
                iQuery.AddField("item_id", owner.Id);
                iQuery.AddField("item_type_id", owner.TypeId);
                // TODO: ACLs

                if (db.Query(iQuery) > 0)
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

            this.db = core.Db;

            if (!HasInstalled(viewer))
            {
                Install(core, viewer);
            }
            else
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, string> slugs = newApplication.GetPageSlugs(viewer.AppPrimitive);

                foreach (string slug in slugs.Keys)
                {
                    SelectQuery query = new SelectQuery("user_pages");
                    query.AddFields("page_id");
                    query.AddCondition("page_item_id", viewer.Id);
                    query.AddCondition("page_item_type_id", viewer.TypeId);
                    query.AddCondition("page_title", slugs[slug]);
                    query.AddCondition("page_slug", slug);
                    query.AddCondition("page_parent_path", string.Empty);

                    if (db.Query(query).Rows.Count == 0)
                    {
                        string tSlug = slug;
                        Page.Create(core, false, viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0, Classifications.None);
                    }
                    else
                    {
                        try
                        {
                            Page myPage = new Page(core, viewer, slug, string.Empty);

                            if (myPage.ListOnly)
                            {
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
                            Page myPage = Page.Create(core, false, viewer, slugs[slug], ref tSlug, 0, string.Empty, PageStatus.PageList, 0, Classifications.None);
							
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

            if (HasInstalled(viewer))
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, string> slugs = newApplication.GetPageSlugs(viewer.AppPrimitive);

                foreach (string slug in slugs.Keys)
                {
                    Page page = new Page(core, viewer, slug, string.Empty);
                    page.Delete();
                }

                DeleteQuery dQuery = new DeleteQuery("primitive_apps");
                dQuery.AddCondition("application_id", applicationId);
                dQuery.AddCondition("item_id", viewer.Id);
                dQuery.AddCondition("item_type_id", viewer.TypeId);

                if (db.Query(dQuery) > 0)
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
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    title, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != string.Empty)
                {
                    path += "/" + parts[i][0];
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path);
                }
            }

            return output;
        }

        public void SendNotification(User receiver, string subject, string body, Template emailBody)
        {
            if (canNotify(receiver))
            {
                Notification.Create(core, this, receiver, subject, body);

                if (receiver.UserInfo.EmailNotifications)
                {
                    core.Email.SendEmail(receiver.UserInfo.PrimaryEmail, HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(subject))), emailBody.ToString());
                }
            }
        }

        public void SendNotification(User receiver, string subject, string body)
        {
            if (canNotify(receiver))
            {
                Notification.Create(core, this, receiver, subject, body);

                if (receiver.UserInfo.EmailNotifications)
                {
                    RawTemplate emailTemplate = new RawTemplate(core.Http.TemplateEmailPath, "notification.eml");

                    emailTemplate.Parse("TO_NAME", receiver.DisplayName);
                    emailTemplate.Parse("NOTIFICATION_MESSAGE", HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(body)).Replace("<br />", "\n")));

                    core.Email.SendEmail(receiver.UserInfo.PrimaryEmail, HttpUtility.HtmlDecode(core.Bbcode.Flatten(HttpUtility.HtmlEncode(subject))), emailTemplate.ToString());
                }
            }
        }

        private bool canNotify(User owner)
        {
            SelectQuery query = new SelectQuery("notifications");
            query.AddField(new QueryFunction("notification_id", QueryFunctions.Count, "twentyfour")); //"COUNT(notification_id) as twentyfour");
            query.AddCondition("notification_primitive_id", owner.Id);
            query.AddCondition("notification_primitive_type_id", owner.TypeId);
            query.AddCondition("notification_application", applicationId);
            query.AddCondition("notification_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum ten per application per day
            // TODO: change this
            if ((long)db.Query(query).Rows[0]["twentyfour"] < 10)
            {
                return true;
            }
            return false;
        }

        public void PublishToFeed(User owner, ItemKey item, string title)
        {
            PublishToFeed(owner, item, title, string.Empty);
        }

        public void PublishToFeed(User owner, ItemKey item, string title, string message)
        {
            if (title.Length > 63)
            {
                title = title.Substring(0, 63);
            }

            if (message.Length > 511)
            {
                message = message.Substring(0, 511);
            }

            SelectQuery query = new SelectQuery("actions");
            query.AddField(new QueryFunction("action_id", QueryFunctions.Count, "twentyfour"));
            query.AddCondition("action_primitive_id", owner.ItemKey.Id);
            query.AddCondition("action_primitive_type_id", owner.ItemKey.TypeId);
            query.AddCondition("action_application", applicationId);
            query.AddCondition("action_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum five per application per day
            if ((long)db.Query(query).Rows[0]["twentyfour"] < 5)
            {
                InsertQuery iquery = new InsertQuery("actions");
                iquery.AddField("action_primitive_id", owner.ItemKey.Id);
                iquery.AddField("action_primitive_type_id", owner.ItemKey.TypeId);
                iquery.AddField("action_item_id", item.Id);
                iquery.AddField("action_item_type_id", item.TypeId);
                iquery.AddField("action_title", title);
                iquery.AddField("action_body", message);
                iquery.AddField("action_application", applicationId);
                iquery.AddField("action_time_ut", UnixTime.UnixTimeStamp());

                db.Query(iquery);
            }
        }

        public void UpdateFeedAction(Action action, string title, string message)
        {
            if (action != null)
            {
                if (title.Length > 63)
                {
                    title = title.Substring(0, 63);
                }

                if (message.Length > 511)
                {
                    message = message.Substring(0, 511);
                }

                UpdateQuery uquery = new UpdateQuery("actions");
                uquery.AddField("action_title", title);
                uquery.AddField("action_body", message);
                uquery.AddField("action_time_ut", UnixTime.UnixTimeStamp());
                uquery.AddCondition("action_application", applicationId);
                uquery.AddCondition("action_primitive_id", action.OwnerId);
                uquery.AddCondition("action_primitive_type_id", ItemKey.GetTypeId(typeof(User)));
                uquery.AddCondition("action_id", action.ActionId);

                db.Query(uquery);
            }
        }

        /// <summary>
        /// Returns an Action or null
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public Action GetMostRecentFeedAction(User owner)
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

            DataTable feedTable = db.Query(query);

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

                if (page.AnApplication.HasInstalled(viewer))
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

            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(User)), -1, "L_CREATOR", null));
            ppgs.Add(new PrimitivePermissionGroup(ItemType.GetTypeId(typeof(User)), -2, "L_EVERYONE", null));

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

        public override string StoreFile(MemoryStream file)
        {
            return core.Storage.SaveFile(core.Storage.PathCombine(core.Settings.StorageBinUserFilesPrefix, "_storage"), file);
        }
    }

    public class InvalidApplicationException : Exception
    {
    }
}
