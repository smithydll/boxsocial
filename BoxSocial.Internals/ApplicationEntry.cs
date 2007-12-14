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
     * DONE: ALTER TABLE `zinzam0_zinzam`.`user_pages` ADD COLUMN `page_list_only` BOOLEAN NOT NULL AFTER `page_classification`;
     * ALTER TABLE `zinzam0_zinzam`.`user_pages` MODIFY COLUMN `page_list_only` TINYINT(1) UNSIGNED NOT NULL;
     */
    public class ApplicationEntry : Primitive
    {
        public const string APPLICATION_FIELDS = "ap.application_id, ap.application_title, ap.application_description, ap.application_icon, ap.application_assembly_name, ap.user_id, ap.application_primitives, ap.application_date_ut, ap.application_primitive, ap.application_comments";
        public const string USER_APPLICATION_FIELDS = "pa.app_id, pa.app_access, pa.item_id, pa.item_type";
        public const string APPLICATION_SLUG_FIELDS = "al.slug_id, al.slug_stub, al.slug_slug_ex, al.application_id";

        private Mysql db;
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
        }

        public string DisplayName
        {
            get
            {
                return title;
            }
        }

        public string DisplayNameOwnership
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

        public string Description
        {
            get
            {
                return description;
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

        public ApplicationEntry(Mysql db, ApplicationCommentType act)
        {
            this.db = db;

            DataTable assemblyTable = db.SelectQuery(string.Format(@"SELECT {0}
                FROM applications ap
                INNER JOIN comment_types ct ON ct.application_id = ap.application_id
                WHERE ct.type_type = '{1}';",
                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(act.Type)));

            if (assemblyTable.Rows.Count == 1)
            {
                loadApplicationInfo(assemblyTable.Rows[0]);

                if (this.owner == null)
                {
                    this.owner = new Member(db, creatorId);
                }
            }
            else
            {
                throw new InvalidApplicationException();
            }
        }

        public ApplicationEntry(Mysql db, Primitive owner, string assemblyName)
        {
            this.db = db;
            this.owner = owner;

            DataTable assemblyTable = db.SelectQuery(string.Format(@"SELECT {0}
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

        public ApplicationEntry(Mysql db, Primitive owner, long applicationId)
        {
            this.db = db;
            this.owner = owner;

            DataTable assemblyTable = db.SelectQuery(string.Format(@"SELECT {0}
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

        public ApplicationEntry(Mysql db, Primitive installee, DataRow applicationRow)
        {
            loadApplicationInfo(applicationRow);
            loadApplicationUserInfo(applicationRow);

            if (installee != null)
            {
                applicationAccess = new Access(db, permissions, installee);
            }
        }

        public ApplicationEntry(Mysql db, DataRow applicationRow)
        {
            this.db = db;

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
        }

        private void loadApplicationUserInfo(DataRow applicationRow)
        {
            itemId = (long)applicationRow["item_id"];
            permissions = (ushort)applicationRow["app_access"];
        }

        public override string Uri
        {
            get
            {
                return ZzUri.AppendSid(string.Format("/application/{0}",
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
                DataTable viewerTable = db.SelectQuery(string.Format("SELECT item_id, item_type FROM primitive_apps WHERE application_id = {0} AND item_id = {1} AND item_type = '{2}'",
                    applicationId, viewer.Id, Mysql.Escape(viewer.Type)));

                if (viewerTable.Rows.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Install(Core core, Primitive viewer)
        {
            return Install(core, viewer, true);
        }

        public bool Install(Core core, Primitive viewer, bool finaliseTransaction)
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
                    applicationId, viewer.Id, Mysql.Escape(viewer.Type), 0x1111), !finaliseTransaction) > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool UpdateInstall(Core core, Primitive viewer)
        {
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

                        if (db.SelectQuery(query).Rows.Count == 0)
                        {
                            string tSlug = slug;
                            Page.Create(core, (Member)viewer, slugs[slug], ref tSlug, 0, "", PageStatus.PageList, 0x1111, 0, Classifications.None);
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

        public static void ShowPage(Core core, APage page)
        {
            core.template.SetTemplate("viewapplication.html");
            page.Signature = PageSignature.viewapplication;

            Member Creator = new Member(core.db, page.AnApplication.CreatorId, true);

            core.template.ParseVariables("APPLICATION_NAME", HttpUtility.HtmlEncode(page.AnApplication.Title));
            core.template.ParseVariables("U_APPLICATION", HttpUtility.HtmlEncode(page.AnApplication.Uri));
            core.template.ParseVariables("DESCRIPTION", HttpUtility.HtmlEncode(page.AnApplication.Description));
            core.template.ParseVariables("CREATOR_DISPLAY_NAME", HttpUtility.HtmlEncode(Creator.DisplayName));

            if (page.AnApplication.HasInstalled(core.session.LoggedInMember))
            {
                core.template.ParseVariables("U_UNINSTALL", HttpUtility.HtmlEncode(ZzUri.AppendSid(string.Format("/account/dashboard/applications?mode=uninstall&id={0}",
                    page.AnApplication.ApplicationId), true)));
            }
            else
            {
                core.template.ParseVariables("U_INSTALL", HttpUtility.HtmlEncode(ZzUri.AppendSid(string.Format("/account/dashboard/applications?mode=install&id={0}",
                    page.AnApplication.ApplicationId), true)));
            }

            core.InvokeHooks(new HookEventArgs(core, AppPrimitives.Application, page.AnApplication));
        }
    }

    public class InvalidApplicationException : Exception
    {
    }
}
