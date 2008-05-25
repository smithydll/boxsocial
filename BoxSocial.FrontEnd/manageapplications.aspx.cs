using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class manageapplications : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string assemblyName = Request.QueryString["app"];
            string mode = Request.QueryString["mode"];

            if (mode == "update")
            {
                if (core.LoggedInMemberId > 2 || core.LoggedInMemberId == 0)
                {
                    Functions.Generate403();
                    return;
                }

                List<Member> members = new List<Member>();

                SelectQuery query = new SelectQuery("primitive_apps pa");
                query.AddFields(ApplicationEntry.APPLICATION_FIELDS);
                query.AddFields(ApplicationEntry.USER_APPLICATION_FIELDS);
                query.AddFields(Member.USER_INFO_FIELDS);
                query.AddJoin(JoinTypes.Inner, "applications ap", "ap.application_id", "pa.application_id");
                query.AddJoin(JoinTypes.Inner, "user_info ui", "pa.item_id", "ui.user_id");
                query.AddCondition("pa.item_type", "USER");

                DataTable userInfoTable = db.Query(query);

                foreach (DataRow dr in userInfoTable.Rows)
                {
                    dr["user_id"] = dr["item_id"];
                    Member member = new Member(core, dr, false);
                    members.Add(member);

                    ApplicationEntry ae = new ApplicationEntry(core, member, dr);

                    ae.UpdateInstall(core, member);

                    //HttpContext.Current.Response.Write(dr["user_id"].ToString() + ", ");
                }

                Display.ShowMessage("Application Updated", "The application has been updated for all users.");
            }
            else
            {

                string assemblyPath = "";
                bool isPrimitive;
                switch (assemblyName)
                {
                    case "Groups":
                    case "Networks":
                        assemblyPath = string.Format("/bin/{0}.dll", assemblyName);
                        isPrimitive = true;
                        break;
                    default:
                        assemblyPath = string.Format("/bin/applications/{0}.dll", assemblyName);
                        isPrimitive = false;
                        break;
                }

                Assembly loadApplication = Assembly.LoadFrom(HttpContext.Current.Server.MapPath(assemblyPath));

                Type[] types = loadApplication.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Application)))
                    {
                        BoxSocial.Internals.Application newApplication = System.Activator.CreateInstance(type, new object[] {core}) as Application;

                        if (newApplication != null)
                        {
                            long updatedRaw = UnixTime.UnixTimeStamp();
                            long applicationId = 0;

                            DataTable applicationTable = db.Query(string.Format(@"SELECT {0}
                            FROM applications ap
                            WHERE application_assembly_name = '{1}'",
                                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(assemblyName)));

                            if (applicationTable.Rows.Count == 1)
                            {
                                ApplicationEntry updateApplication = new ApplicationEntry(core, applicationTable.Rows[0]);
                                applicationId = updateApplication.ApplicationId;

                                if (updateApplication.CreatorId == core.LoggedInMemberId)
                                {

                                    //
                                    // Save Icon
                                    //
                                    if (newApplication.Icon != null)
                                    {
                                        if (!Directory.Exists(Server.MapPath(string.Format(@".\images\{0}\", updateApplication.Key))))
                                        {
                                            Directory.CreateDirectory(Server.MapPath(string.Format(@".\images\{0}\", updateApplication.Key)));
                                        }

                                        newApplication.Icon.Save(Server.MapPath(string.Format(@".\images\{0}\icon.png", updateApplication.Key)), System.Drawing.Imaging.ImageFormat.Png);
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
                                            updateApplication.Key)));
                                    }

                                    //
                                    // Save JavaScript
                                    //
                                    if (!string.IsNullOrEmpty(newApplication.JavaScript))
                                    {
                                        SaveTextFile(newApplication.JavaScript, Server.MapPath(string.Format(@".\scripts\{0}.js",
                                            updateApplication.Key)));
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
                                    query.AddField("application_icon", string.Format(@"/images/{0}/icon.png", updateApplication.Key));
                                    query.AddCondition("application_assembly_name", assemblyName);

                                    db.BeginTransaction();
                                    db.Query(query);
                                }
                                else
                                {
                                    Functions.Generate403();
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
                                    db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type, app_access) VALUES ({0}, {1}, '{2}', {3});",
                                        profileAe.ApplicationId, applicationId, Mysql.Escape("APPLICATION"), 0x1111));
                                }
                                catch
                                {
                                }

                                try
                                {
                                    ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                                    db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type, app_access) VALUES ({0}, {1}, '{2}', {3});",
                                        guestbookAe.ApplicationId, applicationId, Mysql.Escape("APPLICATION"), 0x1111));
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
                                    foreach (ApplicationSlug slug in aii.ApplicationSlugs)
                                    {
                                        if (db.UpdateQuery(string.Format(@"UPDATE application_slugs SET slug_primitives = {0}, slug_updated_ut = {1} WHERE slug_stub = '{2}' AND slug_slug_ex = '{3}' AND application_id = {4}",
                                            (byte)slug.Primitives, updatedRaw, Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId)) != 1)
                                        {
                                            db.UpdateQuery(string.Format(@"INSERT INTO application_slugs (slug_stub, slug_slug_ex, application_id, slug_primitives, slug_updated_ut) VALUES ('{0}', '{1}', {2}, {3}, {4});",
                                                Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId, (byte)slug.Primitives, updatedRaw));
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

                                db.UpdateQuery(string.Format(@"DELETE FROM application_slugs WHERE application_id = {0} AND slug_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                db.UpdateQuery(string.Format(@"DELETE FROM account_modules WHERE application_id = {0} AND module_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                db.UpdateQuery(string.Format(@"DELETE FROM comment_types WHERE application_id = {0} AND type_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                newApplication.InstallTables(loadApplication);

                            }
                            else
                            {
                                Display.ShowMessage("Error", "Error installing application");
                                EndResponse();
                            }
                        }
                    }
                }

                Display.ShowMessage("Application Installed", "The application has been installed.");

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
