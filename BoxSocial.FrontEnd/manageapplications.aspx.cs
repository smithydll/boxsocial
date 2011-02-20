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
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Caching;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class manageapplications : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
			Server.ScriptTimeout = 1000;
            string assemblyName = core.Http.Query["app"];
            string mode = core.Http.Query["mode"];

            System.Web.Caching.Cache cache = Cache;
			cache.Remove("itemFields");

            if (mode == "update")
            {
                if (core.LoggedInMemberId > 2 || core.LoggedInMemberId == 0)
                {
                    core.Functions.Generate403();
                    return;
                }

                //List<Primitive> members = new List<Primitive>();

                SelectQuery query = new SelectQuery("primitive_apps");
                query.AddFields(ApplicationEntry.GetFieldsPrefixed(typeof(ApplicationEntry)));
                query.AddFields(PrimitiveApplicationInfo.GetFieldsPrefixed(typeof(PrimitiveApplicationInfo)));
                query.AddJoin(JoinTypes.Inner, new DataField("primitive_apps", "application_id"), new DataField("applications", "application_id"));
                query.AddCondition("applications.application_assembly_name", assemblyName);

                /*SelectQuery query = new SelectQuery("primitive_apps pa");
                query.AddFields(ApplicationEntry.APPLICATION_FIELDS);
                query.AddFields(ApplicationEntry.USER_APPLICATION_FIELDS);
                query.AddFields(UserInfo.GetFieldsPrefixed(typeof(UserInfo)));
                query.AddJoin(JoinTypes.Inner, "applications ap", "ap.application_id", "pa.application_id");
                query.AddJoin(JoinTypes.Inner, "user_info ui", "pa.item_id", "ui.user_id");
                query.AddCondition("pa.item_type", "USER");*/

                DataTable userInfoTable = db.Query(query);

                foreach (DataRow dr in userInfoTable.Rows)
                {
                    dr["user_id"] = dr["item_id"];
					ItemKey itemKey = new ItemKey((long)dr["item_id"], (long)dr["item_type_id"]);
                    core.PrimitiveCache.LoadPrimitiveProfile(itemKey);
                }

                foreach (DataRow dr in userInfoTable.Rows)
                {
					ItemKey itemKey = new ItemKey((long)dr["item_id"], (long)dr["item_type_id"]);
                    Primitive member = core.PrimitiveCache[itemKey];
                    //members.Add(member);

                    ApplicationEntry ae = new ApplicationEntry(core, member, dr);

                    ae.UpdateInstall(core, member);
                }

                core.Display.ShowMessage("Application Updated", "The application has been updated for all users.");
            }
            else
            {

                string assemblyPath = "";
                bool isPrimitive = false;
                bool isInternals = false;
                switch (assemblyName)
                {
                    case "Internals":
                        assemblyPath = "BoxSocial.Internals.dll";
                        isInternals = true;
                        isPrimitive = false;
                        break;
                    case "Groups":
                    case "Networks":
                        assemblyPath = string.Format("{0}.dll", assemblyName);
                        isInternals = false;
                        isPrimitive = true;
                        break;
                    default:
                        assemblyPath = string.Format("applications/{0}.dll", assemblyName);
                        isInternals = false;
                        isPrimitive = false;
                        break;
                }

                Assembly loadApplication = Assembly.LoadFrom(Path.Combine(core.Http.AssemblyPath, assemblyPath));

                if (isInternals)
                {
                    BoxSocial.Internals.Application.InstallTables(core, loadApplication);
					BoxSocial.Internals.Application.InstallTypes(core, loadApplication, 0);
                    
                    Type[] types = loadApplication.GetTypes();
                    foreach (Type t in types)
                    {
                        //if (t.GetInterfaces().
                        List<PermissionInfo> permissions = AccessControlLists.GetPermissionInfo(t);
                        
                        foreach (PermissionInfo pi in permissions)
                        {
                            try
                            {
                                ItemType it = new ItemType(core, t.FullName);
                                try
                                {
                                    AccessControlPermission acp = new AccessControlPermission(core, it.Id, pi.Key);
                                }
                                catch (InvalidAccessControlPermissionException)
                                {
                                    AccessControlPermission.Create(core, it.Id, pi.Key, pi.Description, pi.PermissionType);
                                }
                            }
                            catch (InvalidItemTypeException)
                            {
                            }
                        }
                    }

                    core.Display.ShowMessage("Internals Updated", "Internals have been updated.");
                }
                else
                {
                    Type[] types = loadApplication.GetTypes();
                    foreach (Type type in types)
                    {
                        if (type.IsSubclassOf(typeof(Application)))
                        {
                            BoxSocial.Internals.Application newApplication = System.Activator.CreateInstance(type, new object[] { core }) as Application;

                            if (newApplication != null)
                            {
                                long updatedRaw = UnixTime.UnixTimeStamp();
                                long applicationId = 0;

                                SelectQuery query1 = Item.GetSelectQueryStub(typeof(ApplicationEntry));
                                query1.AddCondition("application_assembly_name", assemblyName);

                                /*DataTable applicationTable = db.Query(string.Format(@"SELECT {0}
                            FROM applications ap
                            WHERE application_assembly_name = '{1}'",
                                    ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(assemblyName)));*/

                                DataTable applicationTable = db.Query(query1);

                                if (applicationTable.Rows.Count == 1)
                                {
                                    ApplicationEntry updateApplication = new ApplicationEntry(core, applicationTable.Rows[0]);
                                    applicationId = updateApplication.ApplicationId;
                                    string updateKey = updateApplication.Key;

                                    if (updateApplication.CreatorId == core.LoggedInMemberId)
                                    {

                                        //
                                        // Save Icon
                                        //
                                        if (newApplication.Icon != null)
                                        {
                                            if (!Directory.Exists(Server.MapPath(string.Format(@".\images\{0}\", updateKey))))
                                            {
                                                Directory.CreateDirectory(Server.MapPath(string.Format(@".\images\{0}\", updateKey)));
                                            }

                                            newApplication.Icon.Save(Server.MapPath(string.Format(@".\images\{0}\icon.png", updateKey)), System.Drawing.Imaging.ImageFormat.Png);
                                        }

                                        //
                                        // Save StyleSheet
                                        //
                                        if (!string.IsNullOrEmpty(newApplication.StyleSheet))
                                        {
                                            if (!Directory.Exists(Server.MapPath(@".\styles\applications\")))
                                            {
                                                Directory.CreateDirectory(Server.MapPath(@".\styles\applications\"));
                                            }

                                            SaveTextFile(newApplication.StyleSheet, Server.MapPath(string.Format(@".\styles\applications\{0}.css",
                                                updateKey)));
                                        }

                                        //
                                        // Save JavaScript
                                        //
                                        if (!string.IsNullOrEmpty(newApplication.JavaScript))
                                        {
                                            SaveTextFile(newApplication.JavaScript, Server.MapPath(string.Format(@".\scripts\{0}.js",
                                                updateKey)));
                                        }

                                        UpdateQuery query = new UpdateQuery("applications");
                                        query.AddField("application_title", newApplication.Title);
                                        query.AddField("application_description", newApplication.Description);
                                        query.AddField("application_primitive", isPrimitive);
                                        query.AddField("application_primitives", (byte)newApplication.GetAppPrimitiveSupport());
                                        query.AddField("application_comment", newApplication.UsesComments);
                                        query.AddField("application_rating", newApplication.UsesRatings);
                                        query.AddField("application_style", !string.IsNullOrEmpty(newApplication.StyleSheet));
                                        query.AddField("application_script", !string.IsNullOrEmpty(newApplication.JavaScript));
                                        query.AddField("application_icon", string.Format(@"/images/{0}/icon.png", updateKey));
                                        query.AddCondition("application_assembly_name", assemblyName);

                                        db.BeginTransaction();
                                        db.Query(query);
                                    }
                                    else
                                    {
                                        core.Functions.Generate403();
                                        return;
                                    }
                                }
                                else
                                {
                                    applicationId = db.UpdateQuery(string.Format(@"INSERT INTO applications (application_assembly_name, user_id, application_date_ut, application_title, application_description, application_primitive, application_primitives, application_comment, application_rating) VALUES ('{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, {7}, {8});",
                                        Mysql.Escape(assemblyName), core.LoggedInMemberId, tz.GetUnixTimeStamp(tz.Now), Mysql.Escape(newApplication.Title), Mysql.Escape(newApplication.Description), isPrimitive, (byte)newApplication.GetAppPrimitiveSupport(), newApplication.UsesComments, newApplication.UsesRatings));

                                    try
                                    {
                                        ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                                        db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type_id) VALUES ({0}, {1}, '{2}');",
                                            profileAe.ApplicationId, applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry))));
                                    }
                                    catch
                                    {
                                    }

                                    try
                                    {
                                        ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                                        db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type_id) VALUES ({0}, {1}, '{2}');",
                                            guestbookAe.ApplicationId, applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry))));
                                    }
                                    catch
                                    {
                                    }
                                }

                                if (applicationId > 0)
                                {
                                    ApplicationInstallationInfo aii = newApplication.Install();

                                    if (aii.ApplicationSlugs != null)
                                    {
                                        foreach (ApplicationSlugInfo slug in aii.ApplicationSlugs)
                                        {
                                            if (db.UpdateQuery(string.Format(@"UPDATE application_slugs SET slug_primitives = {0}, slug_updated_ut = {1} WHERE slug_stub = '{2}' AND slug_slug_ex = '{3}' AND application_id = {4}",
                                                (byte)slug.Primitives, updatedRaw, Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId)) != 1)
                                            {
                                                /*db.UpdateQuery(string.Format(@"INSERT INTO application_slugs (slug_stub, slug_slug_ex, application_id, slug_primitives, slug_updated_ut) VALUES ('{0}', '{1}', {2}, {3}, {4});",
                                                    Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId, (byte)slug.Primitives, updatedRaw));*/
                                                ApplicationSlug.Create(core, applicationId, slug);
                                            }
                                        }
                                    }

                                    if (aii.ApplicationModules != null)
                                    {
                                        foreach (ApplicationModule module in aii.ApplicationModules)
                                        {
                                            if (db.UpdateQuery(string.Format(@"UPDATE account_modules SET module_updated_ut = {0} WHERE module_module = '{1}' AND application_id = {2};",
                                                updatedRaw, Mysql.Escape(module.Slug), applicationId)) != 1)
                                            {
                                                db.UpdateQuery(string.Format(@"INSERT INTO account_modules (module_module, application_id, module_updated_ut) VALUES ('{0}', {1}, {2});",
                                                    Mysql.Escape(module.Slug), applicationId, updatedRaw));
                                            }
                                        }
                                    }

                                    if (aii.ApplicationCommentTypes != null)
                                    {
                                        foreach (ApplicationCommentType ct in aii.ApplicationCommentTypes)
                                        {
                                            if (db.UpdateQuery(string.Format(@"UPDATE comment_types SET type_updated_ut = {0} WHERE type_type = '{1}' AND application_id = {2};",
                                                updatedRaw, Mysql.Escape(ct.Type), applicationId)) != 1)
                                            {
                                                db.UpdateQuery(string.Format(@"INSERT INTO comment_types (type_type, application_id, type_updated_ut) VALUES ('{0}', {1}, {2});",
                                                    Mysql.Escape(ct.Type), applicationId, updatedRaw));
                                            }
                                        }
                                    }

                                    /*if (aii.ApplicationItemAccessPermissions != null)
                                    {
                                        foreach (ApplicationItemAccessPermissions iap in aii.ApplicationItemAccessPermissions)
                                        {
                                            try
                                            {
                                                AccessControlPermission acp = new AccessControlPermission(core, iap.TypeId, iap.PermissionName);
                                            }
                                            catch (InvalidAccessControlPermissionException)
                                            {
                                                AccessControlPermission.Create(core, iap.TypeId, iap.PermissionName);
                                            }
                                        }
                                    }*/

                                    db.UpdateQuery(string.Format(@"DELETE FROM application_slugs WHERE application_id = {0} AND slug_updated_ut <> {1};",
                                        applicationId, updatedRaw));

                                    db.UpdateQuery(string.Format(@"DELETE FROM account_modules WHERE application_id = {0} AND module_updated_ut <> {1};",
                                        applicationId, updatedRaw));

                                    db.UpdateQuery(string.Format(@"DELETE FROM comment_types WHERE application_id = {0} AND type_updated_ut <> {1};",
                                        applicationId, updatedRaw));

									BoxSocial.Internals.Application.InstallTypes(core, loadApplication, applicationId);
                                    BoxSocial.Internals.Application.InstallTables(core, loadApplication);
                                    
                                    //List<Type> types;
                                    
                                    foreach (Type t in types)
                                    {
                                        //if (t.FindInterfaces(TypeFilter.Equals, typeof(IPermissibleItem)))
                                        List<PermissionInfo> permissions = AccessControlLists.GetPermissionInfo(t);
                                        
                                        foreach (PermissionInfo pi in permissions)
                                        {
                                            try
                                            {
                                                ItemType it = new ItemType(core, t.FullName);
                                                try
                                                {
                                                    AccessControlPermission acp = new AccessControlPermission(core, it.Id, pi.Key);
                                                }
                                                catch (InvalidAccessControlPermissionException)
                                                {
                                                    AccessControlPermission.Create(core, it.Id, pi.Key, pi.Description, pi.PermissionType);
                                                }
                                            }
                                            catch (InvalidItemTypeException)
                                            {
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    core.Display.ShowMessage("Error", "Error installing application");
                                    EndResponse();
                                }
                            }
                        }
                    }

                    core.Display.ShowMessage("Application Installed", "The application has been installed.");

                }

            }
            EndResponse();
            
        }

        /// <summary>
        /// Save a text file
        /// </summary>
        /// <param name="fileToSave"></param>
        /// <param name="fileName"></param>
        protected static void SaveTextFile(string fileToSave, string fileName)
        {
            StreamWriter myStreamWriter = File.CreateText(fileName);
            myStreamWriter.Write(fileToSave);
            myStreamWriter.Close();
        }
    }
}
