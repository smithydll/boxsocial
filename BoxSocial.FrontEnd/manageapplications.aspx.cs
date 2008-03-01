using System;
using System.Collections;
using System.Data;
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
                    BoxSocial.Internals.Application newApplication = System.Activator.CreateInstance(type, new object[0]) as Application;

                    if (newApplication != null)
                    {
                        long updatedRaw = UnixTime.UnixTimeStamp() ;
                        long applicationId = 0;

                        DataTable applicationTable = db.SelectQuery(string.Format(@"SELECT {0}
                            FROM applications ap
                            WHERE application_assembly_name = '{1}'",
                            ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(assemblyName)));

                        if (applicationTable.Rows.Count == 1)
                        {
                            ApplicationEntry updateApplication = new ApplicationEntry(core.db, applicationTable.Rows[0]);
                            applicationId = updateApplication.ApplicationId;

                            if (updateApplication.CreatorId == session.LoggedInMember.UserId)
                            {
                                db.UpdateQuery(string.Format(@"UPDATE applications SET application_title = '{1}', application_description = '{2}', application_primitive = {3}, application_primitives = {4}, application_comment = {5} WHERE application_assembly_name = '{0}'",
                                    Mysql.Escape(assemblyName), Mysql.Escape(newApplication.Title), Mysql.Escape(newApplication.Description), isPrimitive, (byte)newApplication.GetAppPrimitiveSupport(), newApplication.UsesComments), true);
                            }
                            else
                            {
                                Functions.Generate403();
                                return;
                            }
                        }
                        else
                        {
                            applicationId = db.UpdateQuery(string.Format(@"INSERT INTO applications (application_assembly_name, user_id, application_date_ut, application_title, application_description, application_primitive, application_primitives, application_comment) VALUES ('{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, {7});",
                                Mysql.Escape(assemblyName), core.LoggedInMemberId, tz.GetUnixTimeStamp(tz.Now), Mysql.Escape(newApplication.Title), Mysql.Escape(newApplication.Description), isPrimitive, (byte)newApplication.GetAppPrimitiveSupport(), newApplication.UsesComments), true);

                            try
                            {
                                ApplicationEntry profileAe = new ApplicationEntry(db, null, "Profile");
                                db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type, app_access) VALUES ({0}, {1}, '{2}', {3});",
                                    profileAe.ApplicationId, applicationId, Mysql.Escape("APPLICATION"), 0x1111), true);
                            }
                            catch
                            {
                            }

                            try
                            {
                                ApplicationEntry guestbookAe = new ApplicationEntry(db, null, "GuestBook");
                                db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type, app_access) VALUES ({0}, {1}, '{2}', {3});",
                                    guestbookAe.ApplicationId, applicationId, Mysql.Escape("APPLICATION"), 0x1111), true);
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
                                        (byte)slug.Primitives, updatedRaw, Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId), true) != 1)
                                    {
                                        db.UpdateQuery(string.Format(@"INSERT INTO application_slugs (slug_stub, slug_slug_ex, application_id, slug_primitives, slug_updated_ut) VALUES ('{0}', '{1}', {2}, {3}, {4});",
                                            Mysql.Escape(slug.Stub), Mysql.Escape(slug.SlugEx), applicationId, (byte)slug.Primitives, updatedRaw), true);
                                    }
                                }
                            }

                            if (aii.ApplicationModules != null)
                            {
                                foreach (ApplicationModule module in aii.ApplicationModules)
                                {
                                    if (db.UpdateQuery(string.Format(@"UPDATE account_modules SET module_updated_ut = {0} WHERE module_module = '{1}' AND application_id = {2};",
                                        updatedRaw, Mysql.Escape(module.Slug), applicationId), true) != 1)
                                    {
                                        db.UpdateQuery(string.Format(@"INSERT INTO account_modules (module_module, application_id, module_updated_ut) VALUES ('{0}', {1}, {2});",
                                            Mysql.Escape(module.Slug), applicationId, updatedRaw), true);
                                    }
                                }
                            }

                            if (aii.ApplicationCommentTypes != null)
                            {
                                foreach (ApplicationCommentType ct in aii.ApplicationCommentTypes)
                                {
                                    if (db.UpdateQuery(string.Format(@"UPDATE comment_types SET type_updated_ut = {0} WHERE type_type = '{1}' AND application_id = {2};",
                                        updatedRaw, Mysql.Escape(ct.Type), applicationId), true) != 1)
                                    {
                                        db.UpdateQuery(string.Format(@"INSERT INTO comment_types (type_type, application_id, type_updated_ut) VALUES ('{0}', {1}, {2});",
                                            Mysql.Escape(ct.Type), applicationId, updatedRaw), true);
                                    }
                                }
                            }

                            db.UpdateQuery(string.Format(@"DELETE FROM application_slugs WHERE application_id = {0} AND slug_updated_ut <> {1};",
                                applicationId, updatedRaw), true);

                            db.UpdateQuery(string.Format(@"DELETE FROM account_modules WHERE application_id = {0} AND module_updated_ut <> {1};",
                                applicationId, updatedRaw), true);

                            db.UpdateQuery(string.Format(@"DELETE FROM comment_types WHERE application_id = {0} AND type_updated_ut <> {1};",
                                applicationId, updatedRaw), false);
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
            EndResponse();
            
        }
    }
}
