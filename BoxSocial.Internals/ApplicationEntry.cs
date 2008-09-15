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
    [DataTable("applications")]
    public class ApplicationEntry : Primitive, ICommentableItem
    {
        public const string APPLICATION_FIELDS = "ap.application_id, ap.application_title, ap.application_description, ap.application_icon, ap.application_assembly_name, ap.user_id, ap.application_primitives, ap.application_date_ut, ap.application_primitive, ap.application_comments, ap.application_comment, ap.application_rating, ap.application_style, ap.application_script";
        public const string USER_APPLICATION_FIELDS = "pa.app_id, pa.app_access, pa.item_id, pa.item_type";
        public const string APPLICATION_SLUG_FIELDS = "al.slug_id, al.slug_stub, al.slug_slug_ex, al.application_id";

        [DataField("application_id", DataFieldKeys.Primary)]
        private long applicationId;
        [DataField("user_id")]
        private int creatorId;
        [DataField("application_title", 63)]
        private string title;
        [DataField("application_description", MYSQL_TEXT)]
        private string description;
        [DataField("application_icon", 63)]
        private string icon;
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

        //TODO: USER_APPLICATION_FIELDS
        private ushort permissions;
        private long itemId;

        private List<string> slugExs;
        private List<string> modules;
        private string displayNameOwnership;

        private Primitive owner; // primitive installed the application
        private Access applicationAccess; // primitive application access rights

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
                    displayNameOwnership = (title != "") ? title : assemblyName;

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

        public Access ApplicationAccess
        {
            get
            {
                return applicationAccess;
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
                return icon;
            }
            set
            {
                SetProperty("icon", value);
            }
        }

        public bool HasIcon
        {
            get
            {
                if (string.IsNullOrEmpty(icon))
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
                    Match pathMatch = Regex.Match(uri, slugEx);
                    if (pathMatch.Success)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public ApplicationEntry(Core core)
            : base(core)
        {
            this.owner = core.session.LoggedInMember;

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

        public ApplicationEntry(Core core, ApplicationCommentType act)
            : base(core)
        {
            DataTable assemblyTable = db.Query(string.Format(@"SELECT {0}
                FROM applications ap
                INNER JOIN comment_types ct ON ct.application_id = ap.application_id
                WHERE ct.type_type = '{1}';",
                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(act.Type)));

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
        }

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
                applicationAccess = new Access(core, permissions, installee);
            }
        }

        public ApplicationEntry(Core core, DataRow applicationRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ApplicationEntry_ItemLoad);

            loadItemInfo(applicationRow);
        }

        private void ApplicationEntry_ItemLoad()
        {
            if (owner != null)
            {
                applicationAccess = new Access(core, permissions, owner);
            }
        }

        private void loadApplicationInfo(DataRow applicationRow)
        {
            applicationId = (int)applicationRow["application_id"];
            creatorId = (int)applicationRow["user_id"];
            if (!(applicationRow["application_title"] is DBNull))
            {
                title = (string)applicationRow["application_title"];
            }
            if (!(applicationRow["application_description"] is DBNull))
            {
                description = (string)applicationRow["application_description"];
            }
            if (!(applicationRow["application_icon"] is DBNull))
            {
                icon = (string)applicationRow["application_icon"];
            }
            assemblyName = (string)applicationRow["application_assembly_name"];
            isPrimitive = ((byte)applicationRow["application_primitive"] > 0) ? true : false;
            primitives = (byte)applicationRow["application_primitives"];
            dateRaw = (long)applicationRow["application_date_ut"];
            comments = (long)applicationRow["application_comments"];
            usesComments = ((byte)applicationRow["application_comment"] > 0) ? true : false;
            usesRatings = ((byte)applicationRow["application_rating"] > 0) ? true : false;
            hasStyleSheet = ((byte)applicationRow["application_style"] > 0) ? true : false;
            hasJavaScript = ((byte)applicationRow["application_script"] > 0) ? true : false;
        }

        private void loadApplicationUserInfo(DataRow applicationRow)
        {
            itemId = (long)applicationRow["item_id"];
            permissions = (ushort)applicationRow["app_access"];
        }

        public static ApplicationEntry Create()
        {
            // TODO:

            throw new NotImplementedException();
        }

        public override string UriStub
        {
            get
            {
                if (HttpContext.Current.Request.Url.Host.ToLower() != Linker.Domain)
                {
                    return Linker.Uri + "application/" + assemblyName + "/";
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
                return Linker.AppendAbsoluteSid(UriStub);
            }
        }

        public override string Uri
        {
            get
            {
                return Linker.AppendSid(UriStub);
            }
        }

        public string GetUri(string type, long id)
        {
            if (type == "USER")
            {
                return Uri;
            }
            else
            {
                return Linker.AppendSid(string.Format("/application/{0}?type={1}&id={2}",
                    assemblyName, type, id));
            }
        }

        public override bool CanModerateComments(User member)
        {
            return false;
        }

        public override bool IsCommentOwner(User member)
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

        public override void GetCan(ushort accessBits, User viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
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
                DataTable viewerTable = db.Query(string.Format("SELECT item_id, item_type FROM primitive_apps WHERE application_id = {0} AND item_id = {1} AND item_type = '{2}'",
                    applicationId, viewer.Id, Mysql.Escape(viewer.Type)));

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

        // bool finaliseTransaction
        public bool Install(Core core, Primitive viewer)
        {
            if (!HasInstalled(viewer))
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, string> slugs = newApplication.PageSlugs;

                foreach (string slug in slugs.Keys)
                {
                    string tSlug = slug;
                    Page.Create(core, viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
                }

                InsertQuery iQuery = new InsertQuery("primitive_apps");
                iQuery.AddField("application_id", applicationId);
                iQuery.AddField("item_id", viewer.Id);
                iQuery.AddField("item_type", viewer.Type);
                iQuery.AddField("app_access", 0x1111);

                if (db.Query(iQuery) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool UpdateInstall(Core core, Primitive viewer)
        {
            this.db = core.db;

            if (!HasInstalled(viewer))
            {
                Install(core, viewer);
            }
            else
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, string> slugs = newApplication.PageSlugs;

                foreach (string slug in slugs.Keys)
                {
                    SelectQuery query = new SelectQuery("user_pages");
                    query.AddFields("page_id");
                    query.AddCondition("page_item_id", viewer.Id);
                    query.AddCondition("page_item_type", viewer.Type);
                    query.AddCondition("page_title", slugs[slug]);
                    query.AddCondition("page_slug", slug);
                    query.AddCondition("page_parent_path", "");

                    if (db.Query(query).Rows.Count == 0)
                    {
                        string tSlug = slug;
                        Page.Create(core, viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
                    }
                    else
                    {
                        try
                        {
                            Page myPage = new Page(core, viewer, slug, "");

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
                            Page.Create(core, viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
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
                        return false;
                }
            }

            if (HasInstalled(viewer))
            {
                Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                Dictionary<string, string> slugs = newApplication.PageSlugs;

                foreach (string slug in slugs.Keys)
                {
                    Page page = new Page(core, viewer, slug, "");
                }

                DeleteQuery dQuery = new DeleteQuery("primitive_apps");
                dQuery.AddCondition("application_id", applicationId);
                dQuery.AddCondition("item_id", viewer.Id);
                dQuery.AddCondition("item_type", viewer.Type);

                if (db.Query(dQuery) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override string GenerateBreadCrumbs(List<string[]> parts)
        {
            string output = "";
            string path = string.Format("/application/{0}", assemblyName);
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    title, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
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
                Notification.Create(this, receiver, subject, body);

                if (receiver.EmailNotifications)
                {
                    Email.SendEmail(receiver.AlternateEmail, subject, emailBody.ToString());
                }
            }
        }

        public void SendNotification(User receiver, string subject, string body)
        {
            if (canNotify(receiver))
            {
                Notification.Create(this, receiver, subject, body);

                if (receiver.EmailNotifications)
                {
                    RawTemplate emailTemplate = new RawTemplate(HttpContext.Current.Server.MapPath("./templates/emails/"), "notification.eml");

                    emailTemplate.Parse("TO_NAME", receiver.DisplayName);
                    emailTemplate.Parse("NOTIFICATION_MESSAGE", HttpUtility.HtmlDecode(Bbcode.Strip(HttpUtility.HtmlEncode(body)).Replace("<br />", "\n")));

                    Email.SendEmail(receiver.AlternateEmail, HttpUtility.HtmlDecode(Bbcode.Strip(HttpUtility.HtmlEncode(subject))), emailTemplate.ToString());
                }
            }
        }

        private bool canNotify(User owner)
        {
            SelectQuery query = new SelectQuery("notifications");
            query.AddField(new QueryFunction("notification_id", QueryFunctions.Count, "twentyfour")); //"COUNT(notification_id) as twentyfour");
            query.AddCondition("notification_primitive_id", owner.Id);
            query.AddCondition("notification_primitive_type", owner.Type);
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

        public void PublishToFeed(User owner, string title)
        {
            PublishToFeed(owner, title, "");
        }

        public void PublishToFeed(User owner, string title, string message)
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
            query.AddCondition("action_primitive_id", owner.Id);
            query.AddCondition("action_primitive_type", owner.Type);
            query.AddCondition("action_application", applicationId);
            query.AddCondition("action_time_ut", ConditionEquality.GreaterThan, UnixTime.UnixTimeStamp() - 60 * 60 * 24);

            // maximum five per application per day
            if ((long)db.Query(query).Rows[0]["twentyfour"] < 5)
            {
                InsertQuery iquery = new InsertQuery("actions");
                iquery.AddField("action_primitive_id", owner.Id);
                iquery.AddField("action_primitive_type", owner.Type);
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
                uquery.AddCondition("action_primitive_type", "USER");
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
            SelectQuery query = Action.GetSelectQueryStub(typeof(Action));
            query.AddSort(SortOrder.Descending, "action_time_ut");
            query.AddCondition("action_application", Id);
            query.AddCondition("action_primitive_id", owner.Id);
            query.AddCondition("action_primitive_type", owner.Type);
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
            core.template.SetTemplate("viewapplication.html");
            page.Signature = PageSignature.viewapplication;

            string type = HttpContext.Current.Request.QueryString["type"];
            long id = Functions.RequestLong("id", 0);

            Primitive viewer = core.session.LoggedInMember;

            if (!string.IsNullOrEmpty(type))
            {
                core.UserProfiles.LoadPrimitiveProfile(type, id);
                viewer = core.UserProfiles[type, id];
            }

            User Creator = new User(core, page.AnApplication.CreatorId, UserLoadOptions.All);

            core.template.Parse("APPLICATION_NAME", page.AnApplication.Title);
            core.template.Parse("U_APPLICATION", page.AnApplication.Uri);
            core.template.Parse("DESCRIPTION", page.AnApplication.Description);
            core.template.Parse("CREATOR_DISPLAY_NAME", Creator.DisplayName);

            if (page.AnApplication.HasInstalled(viewer))
            {
                core.template.Parse("U_UNINSTALL", Linker.AppendSid(string.Format("{1}dashboard/applications?mode=uninstall&id={0}",
                    page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
            }
            else
            {
                core.template.Parse("U_INSTALL", Linker.AppendSid(string.Format("{1}dashboard/applications?mode=install&id={0}",
                    page.AnApplication.ApplicationId, viewer.AccountUriStub), true));
            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Application, page.AnApplication));
        }

        public override string AccountUriStub
        {
            get
            {
                return string.Format("/application/{0}/account/",
                    Key);
            }
        }

        public override string Namespace
        {
            get
            {
                return Type;
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
    }

    public class InvalidApplicationException : Exception
    {
    }
}
