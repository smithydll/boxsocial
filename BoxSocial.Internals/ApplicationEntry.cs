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
    /*
     * TODO: SQL
ALTER TABLE `zinzam0_zinzam`.`applications` ADD COLUMN `application_style` TINYINT(1) UNSIGNED NOT NULL AFTER `application_rating`,
 ADD COLUMN `application_script` TINYINT(1) UNSIGNED NOT NULL AFTER `application_style`;
     */
    public class ApplicationEntry : Primitive, ICommentableItem
    {
        public const string APPLICATION_FIELDS = "ap.application_id, ap.application_title, ap.application_description, ap.application_icon, ap.application_assembly_name, ap.user_id, ap.application_primitives, ap.application_date_ut, ap.application_primitive, ap.application_comments, ap.application_comment, ap.application_rating, ap.application_style, ap.application_script";
        public const string USER_APPLICATION_FIELDS = "pa.app_id, pa.app_access, pa.item_id, pa.item_type";
        public const string APPLICATION_SLUG_FIELDS = "al.slug_id, al.slug_stub, al.slug_slug_ex, al.application_id";

        private Primitive owner;
        private int applicationId;
        private int creatorId;
        private long itemId;
        private string title;
        private string description;
        private string icon;
        private string assemblyName;
        private string displayNameOwnership;
        private Boolean isPrimitive;
        private AppPrimitives primitives;
        private long dateRaw;
        private ushort permissions;
        private Access applicationAccess;
        private long comments;
        private List<string> slugExs;
        private List<string> modules;
        private bool usesComments;
        private bool usesRatings;
        private bool hasStyleSheet;
        private bool hasJavaScript;

        private bool titleChanged;
        private bool descriptionChanged;
        private bool iconChanged;
        private bool isPrimitiveChanged;
        private bool primitivesChanged;
        private bool usesCommentsChanged;
        private bool usesRatingsChanged;
        private bool hasStyleSheetChanged;
        private bool hasJavaScriptChanged;

        public int ApplicationId
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
                title = value;
                titleChanged = true;
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
                description = value;
                descriptionChanged = true;
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
                isPrimitive = value;
                isPrimitiveChanged = true;
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
                icon = value;
                iconChanged = true;
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
                hasStyleSheet = value;
                hasStyleSheetChanged = true;
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
                hasJavaScript = value;
                hasJavaScriptChanged = true;
            }
        }

        public AppPrimitives SupportedPrimitives
        {
            get
            {
                return primitives;
            }
            set
            {
                primitives = value;
                primitivesChanged = true;
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
                usesComments = value;
                usesCommentsChanged = true;
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
                usesRatings = value;
                usesRatingsChanged = true;
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

        public ApplicationEntry(Core core) : base(core)
        {
            this.owner = core.session.LoggedInMember;

            Assembly asm = Assembly.GetCallingAssembly();

            string assemblyName = asm.GetName().Name;

            SelectQuery query = new SelectQuery("applications ap");
            query.AddFields(APPLICATION_FIELDS);
            query.AddCondition("ap.application_assembly_name", assemblyName);

            DataTable assemblyTable = db.Query(query);

            if (assemblyTable.Rows.Count == 1)
            {
                loadApplicationInfo(assemblyTable.Rows[0]);

                if (this.owner == null)
                {
                    applicationAccess = new Access(db, permissions, owner);
                }
            }
            else
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, ApplicationCommentType act) : base(core)
        {
            DataTable assemblyTable = db.Query(string.Format(@"SELECT {0}
                FROM applications ap
                INNER JOIN comment_types ct ON ct.application_id = ap.application_id
                WHERE ct.type_type = '{1}';",
                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(act.Type)));

            if (assemblyTable.Rows.Count == 1)
            {
                loadApplicationInfo(assemblyTable.Rows[0]);

                if (this.owner == null)
                {
                    this.owner = new Member(core, creatorId);
                }
            }
            else
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, Primitive owner, string assemblyName) : base(core)
        {
            this.owner = owner;

            DataTable assemblyTable = core.db.Query(string.Format(@"SELECT {0}
                FROM applications ap
                WHERE ap.application_assembly_name = '{1}';",
                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(assemblyName)));

            if (assemblyTable.Rows.Count == 1)
            {
                loadApplicationInfo(assemblyTable.Rows[0]);

                if (this.owner == null)
                {
                    applicationAccess = new Access(db, permissions, owner);
                }
            }
            else
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, Primitive owner, long applicationId) : base(core)
        {
            this.owner = owner;

            DataTable assemblyTable = db.Query(string.Format(@"SELECT {0}
                FROM applications ap
                WHERE ap.application_id = {1};",
                ApplicationEntry.APPLICATION_FIELDS, applicationId));

            if (assemblyTable.Rows.Count == 1)
            {
                loadApplicationInfo(assemblyTable.Rows[0]);

                if (this.owner == null)
                {
                    applicationAccess = new Access(db, permissions, owner);
                }
            }
            else
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Core core, Primitive installee, DataRow applicationRow) : base(core)
        {
            loadApplicationInfo(applicationRow);
            loadApplicationUserInfo(applicationRow);

            if (installee != null)
            {
                applicationAccess = new Access(db, permissions, installee);
            }
        }

        public ApplicationEntry(Core core, DataRow applicationRow) : base (core)
        {

            loadApplicationInfo(applicationRow);
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
            primitives = (AppPrimitives)applicationRow["application_primitives"];
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

        public void Create()
        {
        }

        /*public ApplicationEntry Update()
        {
            return Update(false);
        }*/

        public new ApplicationEntry Update()
        {
            UpdateQuery query = new UpdateQuery("applications");
            if (titleChanged)
            {
                query.AddField("application_title", title);
            }
            if (descriptionChanged)
            {
                query.AddField("application_description", description);
            }
            if (isPrimitiveChanged)
            {
                query.AddField("application_primitive", isPrimitive);
            }
            if (primitivesChanged)
            {
                query.AddField("application_primitives", (byte)primitives);
            }
            if (usesCommentsChanged)
            {
                query.AddField("application_comment", usesComments);
            }
            if (usesRatingsChanged)
            {
                query.AddField("application_rating", usesRatings);
            }
            if (iconChanged)
            {
                query.AddField("application_icon", icon);
            }
            if (hasStyleSheetChanged)
            {
                query.AddField("application_style", hasStyleSheet);
            }
            if (hasJavaScriptChanged)
            {
                query.AddField("application_script", hasJavaScript);
            }
            query.AddCondition("application_id", Id);

            db.Query(query);

            return this;
        }

        public override string Uri
        {
            get
            {
                return Linker.AppendSid(string.Format("/application/{0}",
                    assemblyName));
            }
        }

        public override bool CanModerateComments(Member member)
        {
            return false;
        }

        public override bool IsCommentOwner(Member member)
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

        public override ushort GetAccessLevel(Member viewer)
        {
            return 0x0001;
        }

        public override void GetCan(ushort accessBits, Member viewer, out bool canRead, out bool canComment, out bool canCreate, out bool canChange)
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
                if (viewer is Member)
                {
                    Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                    Dictionary<string, string> slugs = newApplication.PageSlugs;

                    foreach (string slug in slugs.Keys)
                    {
                        string tSlug = slug;
                        Page.Create(core, (Member)viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
                    }
                }
                if (db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type, app_access) VALUES ({0}, {1}, '{2}', {3});",
                    applicationId, viewer.Id, Mysql.Escape(viewer.Type), 0x1111)) > 0)
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
                if (viewer is Member)
                {
                    Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                    Dictionary<string, string> slugs = newApplication.PageSlugs;

                    foreach (string slug in slugs.Keys)
                    {
                        SelectQuery query = new SelectQuery("user_pages");
                        query.AddFields("page_id");
                        query.AddCondition("user_id", ((Member)viewer).UserId);
                        query.AddCondition("page_title", slugs[slug]);
                        query.AddCondition("page_slug", slug);
                        query.AddCondition("page_parent_path", "");

                        if (db.Query(query).Rows.Count == 0)
                        {
                            string tSlug = slug;
                            Page.Create(core, (Member)viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
                        }
                        else
                        {
                            try
                            {
                                Page myPage = new Page(db, (Member)viewer, slug, "");

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
                                Page.Create(core, (Member)viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
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
                        return false;
                }
            }

            if (HasInstalled(viewer))
            {
                if (viewer is Member)
                {
                    Application newApplication = Application.GetApplication(core, AppPrimitives.Member, this);

                    Dictionary<string, string> slugs = newApplication.PageSlugs;

                    foreach (string slug in slugs.Keys)
                    {
                        Page page = new Page(db, (Member)viewer, slug, "");
                    }
                }
                if (db.UpdateQuery(string.Format(@"DELETE FROM primitive_apps WHERE application_id = {0} AND item_id = {1} AND item_type = '{2}';",
                    applicationId, viewer.Id, Mysql.Escape(viewer.Type))) > 0)
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

        public void SendNotification(Member receiver, string subject, string body, Template emailBody)
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

        public void SendNotification(Member receiver, string subject, string body)
        {
            if (canNotify(receiver))
            {
                Notification.Create(this, receiver, subject, body);

                if (receiver.EmailNotifications)
                {
                    Template emailTemplate = new Template(HttpContext.Current.Server.MapPath("./templates/emails/"), "notification.eml");

                    emailTemplate.ParseVariables("TO_NAME", receiver.DisplayName);
                    emailTemplate.ParseVariables("NOTIFICATION_MESSAGE", HttpUtility.HtmlDecode(Bbcode.Strip(HttpUtility.HtmlEncode(body)).Replace("<br />", "\n")));

                    Email.SendEmail(receiver.AlternateEmail, HttpUtility.HtmlDecode(Bbcode.Strip(HttpUtility.HtmlEncode(subject))), emailTemplate.ToString());
                }
            }
        }

        private bool canNotify(Member owner)
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

        public void PublishToFeed(Member owner, string title)
        {
            PublishToFeed(owner, title, "");
        }

        public void PublishToFeed(Member owner, string title, string message)
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
            query.AddField(new QueryFunction("action_id", QueryFunctions.Count, "twentyfour")); //"COUNT(action_id) as twentyfour");
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
        public Action GetMostRecentFeedAction(Member owner)
        {
            SelectQuery query = new SelectQuery("actions at");
            query.AddFields(Action.FEED_FIELDS);
            query.AddSort(SortOrder.Descending, "at.action_time_ut");
            query.AddCondition("at.action_application", Id);
            query.AddCondition("at.action_primitive_id", owner.Id);
            query.AddCondition("at.action_primitive_type", owner.Type);
            query.LimitCount = 1;

            DataTable feedTable = db.Query(query);

            if (feedTable.Rows.Count == 1)
            {
                return new Action(db, owner, feedTable.Rows[0]);
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

            Member Creator = new Member(core, page.AnApplication.CreatorId, true);

            core.template.ParseVariables("APPLICATION_NAME", HttpUtility.HtmlEncode(page.AnApplication.Title));
            core.template.ParseVariables("U_APPLICATION", HttpUtility.HtmlEncode(page.AnApplication.Uri));
            core.template.ParseVariables("DESCRIPTION", HttpUtility.HtmlEncode(page.AnApplication.Description));
            core.template.ParseVariables("CREATOR_DISPLAY_NAME", HttpUtility.HtmlEncode(Creator.DisplayName));

            if (page.AnApplication.HasInstalled(core.session.LoggedInMember))
            {
                core.template.ParseVariables("U_UNINSTALL", HttpUtility.HtmlEncode(Linker.AppendSid(string.Format("/account/dashboard/applications?mode=uninstall&id={0}",
                    page.AnApplication.ApplicationId), true)));
            }
            else
            {
                core.template.ParseVariables("U_INSTALL", HttpUtility.HtmlEncode(Linker.AppendSid(string.Format("/account/dashboard/applications?mode=install&id={0}",
                    page.AnApplication.ApplicationId), true)));
            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Application, page.AnApplication));
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
