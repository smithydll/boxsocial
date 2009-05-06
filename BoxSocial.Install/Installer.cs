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
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Install
{
    public static class Installer
    {

        private static bool binary;
        private static string root;
        private static string binRoot;
        private static string imagesRoot;
        private static string stylesRoot;
        private static string scriptsRoot;
        private static string domain;
        private static string mysqlRootPassword;
        private static string mysqlWebUser;
        private static string mysqlWebPassword;
        private static string mysqlDatabase;
        private static string adminUsername;
        private static string adminPassword;
        private static string adminEmail;
        private static long adminUid;

        static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            if (argsList.Contains("--source") || argsList.Contains("-S"))
            {
                binary = false;
            }
            else
            {
                binary = true;
            }

            Console.WriteLine("Box Social will only install into the root directory of a domain. Everything in the root directory will be deleted. Do you want to continue? (y/n)");
            if (Console.ReadLine().ToLower().StartsWith("y"))
            {
                Console.WriteLine("If you do not provide the root directory of a domain, Box Social will not install properly.");
                Console.WriteLine("Please enter the root directory of the domain you want to use:");
                root = Console.ReadLine();
                binRoot = Path.Combine(root, "bin");
                imagesRoot = Path.Combine(root, "images");
                stylesRoot = Path.Combine(Path.Combine(root, "styles"), "applications");
                scriptsRoot = Path.Combine(root, "scripts");

                Console.WriteLine("Please enter the domain name of the directory you just entered (e.g. zinzam.com, localhost, 127.0.0.1):");
                domain = Console.ReadLine();

                Console.WriteLine("Please enter the mysql root password:");
                mysqlRootPassword = Console.ReadLine();

                Console.WriteLine("Please enter the mysql database you have created:");
                mysqlDatabase = Console.ReadLine();

                Console.WriteLine("Please enter the mysql low privledge user name (e.g. web@localhost):");
                mysqlWebUser = Console.ReadLine();

                Console.WriteLine("Please enter the mysql low privledge user password:");
                mysqlWebPassword = Console.ReadLine();

                Console.WriteLine("Please enter administrator username:");
                adminUsername = Console.ReadLine();

                Console.WriteLine("Please enter administrator e-mail address:");
                adminEmail = Console.ReadLine();

                Console.WriteLine("Please enter administrator password:");
                adminPassword = Console.ReadLine();

                // install
                PerformInstall();

                Console.WriteLine("Box Social installed successfully.");
                return;
            }
            else
            {
                Console.WriteLine("Installation of Box Social aborted.");
                return;
            }
        }

        static void PerformInstall()
        {
            if (Directory.Exists(binRoot))
            {
                Directory.Delete(binRoot, true);
            }

            Directory.CreateDirectory(binRoot);
            Directory.CreateDirectory(Path.Combine(binRoot, "applications"));

            File.Copy("MySql.Data.dll", Path.Combine(binRoot, "MySql.Data.dll"));

            if (Directory.Exists(imagesRoot))
            {
                Directory.Delete(imagesRoot, true);
            }

            Directory.CreateDirectory(imagesRoot);

            /* ==================== */
            if (!binary)
            {
                DownloadRepository(@"BoxSocial.Forms");
                CompileRepository(@"BoxSocial.Forms");
            }
            InstallRepository(@"BoxSocial.Forms");
            /* ==================== */
            InstallRepository(@"BoxSocial.FrontEnd");
            /* ==================== */
            InstallRepository(@"BoxSocial.IO");
            /* ==================== */
            InstallRepository(@"BoxSocial.Internals");
            InstallApplication(@"BoxSocial.Internals");
            /* ==================== */
            InstallRepository(@"Groups");
            InstallApplication(@"Groups");
            /* ==================== */
            InstallRepository(@"Networks");
            InstallApplication(@"Networks");
            /* ==================== */
            InstallRepository(@"Profile");
            InstallApplication(@"Profile");
            /* ==================== */
            InstallRepository(@"Calendar");
            InstallApplication(@"Calendar");
            /* ==================== */
            InstallRepository(@"Gallery");
            InstallApplication(@"Gallery");
            /* ==================== */
            InstallRepository(@"GuestBook");
            InstallApplication(@"GuestBook");
            /* ==================== */
            InstallRepository(@"Pages");
            InstallApplication(@"Pages");
            /* ==================== */
            InstallRepository(@"Blog");
            InstallApplication(@"Blog");
            /* ==================== */
            InstallRepository(@"Forum");
            InstallApplication(@"Forum");
            /* ==================== */
            InstallRepository(@"Mail");
            InstallApplication(@"Mail");
            /* ==================== */
            InstallRepository(@"News");
            InstallApplication(@"News");
            /* ==================== */

            Mysql db = new Mysql("root", mysqlRootPassword, mysqlDatabase, "localhost");
            Template template = new Template(Path.Combine(root, "templates"), "default.html");
            Core core = new Core(db, template);
            UnixTime tz = new UnixTime(core, 0);

            //User anonymous = User.Register(core, "Anonymous", "anonymous@zinzam.com", "Anonymous", "Anonymous");
            // blank out the anon password to make it impossible to login as
            //db.UpdateQuery("UPDATE user_info SET user_password = '' WHERE user_id = " + anonymous.Id + ";");
			CreateUser(core, "Anonymous", "anonymous@zinzam.com", null);

            //User admin = User.Register(core, adminUsername, adminEmail, adminPassword, adminPassword);
            //adminUid = admin.Id;
			long adminId = CreateUser(core, adminUsername, adminEmail, adminPassword);

            db.UpdateQuery("UPDATE applications SET user_id = " + adminId + ";");
            db.CloseConnection();
        }
		
		public static long CreateUser(Core core, string userName, string email, string password)
		{
            InsertQuery iQuery = new InsertQuery("user_keys");
            iQuery.AddField("user_name", userName);
            iQuery.AddField("user_name_lower", userName.ToLower());
            iQuery.AddField("user_name_first", userName.ToLower()[0]);

            long userId = core.db.Query(iQuery);

			iQuery = new InsertQuery("user_info");
            iQuery.AddField("user_id", userId);
			iQuery.AddField("user_name", userName);
			iQuery.AddField("user_reg_ip", "");
			iQuery.AddField("user_reg_date_ut", UnixTime.UnixTimeStamp().ToString());
			iQuery.AddField("user_last_visit_ut", UnixTime.UnixTimeStamp().ToString());
			if (password != null)
			{
				iQuery.AddField("user_password", User.HashPassword(password));
			}
			else
			{
				iQuery.AddField("user_password", "");
			}
			iQuery.AddField("user_new_password", "");
			iQuery.AddField("user_active", 1);
			iQuery.AddField("user_alternate_email", email);
			iQuery.AddField("user_home_page", "/profile");
			iQuery.AddField("user_show_custom_styles", 1);
			iQuery.AddField("user_email_notifications", 1);
			iQuery.AddField("user_show_bbcode", 0x07);
			iQuery.AddField("user_bytes", 0);
			
			core.db.Query(iQuery);
			
			iQuery = new InsertQuery("user_profile");
			iQuery.AddField("user_id", userId);
			iQuery.AddField("profile_access", 0x3331);
			
			core.db.Query(iQuery);
			
			iQuery = new InsertQuery("user_emails");
			iQuery.AddField("email_user_id", userId);
			iQuery.AddField("email_email", email);
			iQuery.AddField("email_verified", 1);
					
			iQuery.AddField("email_time_ut", UnixTime.UnixTimeStamp().ToString());
			iQuery.AddField("email_access", 0);
						
			core.db.Query(iQuery);
			
			User newUser = new User(core, userId);
			core.session = new SessionState(core, newUser);
			
			// Install a couple of applications
            try
            {
                ApplicationEntry profileAe = new ApplicationEntry(core, newUser, "Profile");
                profileAe.Install(core, newUser);
            }
            catch (Exception ex)
            {
				Console.WriteLine(ex.ToString());
            }

            try
            {
                ApplicationEntry galleryAe = new ApplicationEntry(core, newUser, "Gallery");
                galleryAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry guestbookAe = new ApplicationEntry(core, newUser, "GuestBook");
                guestbookAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry groupsAe = new ApplicationEntry(core, newUser, "Groups");
                groupsAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry networksAe = new ApplicationEntry(core, newUser, "Networks");
                networksAe.Install(core, newUser);
            }
            catch
            {
            }

            try
            {
                ApplicationEntry calendarAe = new ApplicationEntry(core, newUser, "Calendar");
                calendarAe.Install(core, newUser);
            }
            catch
            {
            }

            return userId;
		}

        private static void DownloadRepository(string repo)
        {
        }

        private static void CompileRepository(string repo)
        {
        }

        private static void InstallRepository(string repo)
        {
            switch (repo)
            {
                case "BoxSocial.Forms":
                case "BoxSocial.FrontEnd":
                case "BoxSocial.Internals":
                case "BoxSocial.IO":
                case "Groups":
                case "Networks":
                    File.Copy(repo + ".dll", Path.Combine(binRoot, repo + ".dll"));
                    break;
                default:
                    File.Copy(repo + ".dll", Path.Combine(Path.Combine(binRoot, "applications"), repo + ".dll"));
                    break;
            }
        }

        private static void InstallApplication(string repo)
        {
            Mysql db = new Mysql("root", mysqlRootPassword, mysqlDatabase, "localhost");
            Template template = new Template(Path.Combine(root, "templates"), "default.html");
            Core core = new Core(db, template);
            UnixTime tz = new UnixTime(core, 0);

            string assemblyPath = null;
            bool isPrimitive = false;
            bool isInternals = false;
            switch (repo)
            {
                case "BoxSocial.Internals":
                    assemblyPath = Path.Combine(binRoot, repo + ".dll");
                    isInternals = true;
                    isPrimitive = false;
                    break;
                case "Groups":
                case "Networks":
                    assemblyPath = Path.Combine(binRoot, repo + ".dll");
                    isInternals = false;
                    isPrimitive = true;
                    break;
                default:
                    assemblyPath = Path.Combine(Path.Combine(binRoot, "applications"), repo + ".dll");
                    isInternals = false;
                    isPrimitive = false;
                    break;
            }

            Assembly loadApplication = Assembly.LoadFrom(assemblyPath);

            if (isInternals)
            {
                BoxSocial.Internals.Application.InstallTables(core, loadApplication);
                BoxSocial.Internals.Application.InstallTypes(core, loadApplication, 0);

                Console.WriteLine(repo + " has been installed.");
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

                            DataTable applicationTable = db.Query(string.Format(@"SELECT {0}
                            FROM applications ap
                            WHERE application_assembly_name = '{1}'",
                                ApplicationEntry.APPLICATION_FIELDS, Mysql.Escape(repo)));

                            if (applicationTable.Rows.Count == 1)
                            {
                                ApplicationEntry updateApplication = new ApplicationEntry(core, applicationTable.Rows[0]);
                                applicationId = updateApplication.ApplicationId;

                                //
                                // Save Icon
                                //
                                if (newApplication.Icon != null)
                                {
                                    if (!Directory.Exists(Path.Combine(imagesRoot, updateApplication.Key)))
                                    {
                                        Directory.CreateDirectory(Path.Combine(imagesRoot, updateApplication.Key));
                                    }

                                    newApplication.Icon.Save(Path.Combine(Path.Combine(imagesRoot, updateApplication.Key), "icon.png"), System.Drawing.Imaging.ImageFormat.Png);
                                }

                                //
                                // Save StyleSheet
                                //
                                if (!string.IsNullOrEmpty(newApplication.StyleSheet))
                                {
                                    if (!Directory.Exists(stylesRoot))
                                    {
                                        Directory.CreateDirectory(stylesRoot);
                                    }

                                    SaveTextFile(newApplication.StyleSheet, Path.Combine(stylesRoot, updateApplication.Key + ".css"));
                                }

                                //
                                // Save JavaScript
                                //
                                if (!string.IsNullOrEmpty(newApplication.JavaScript))
                                {
                                    SaveTextFile(newApplication.JavaScript, Path.Combine(scriptsRoot, updateApplication.Key + ".js"));
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
                                query.AddCondition("application_assembly_name", repo);

                                db.BeginTransaction();
                                db.Query(query);

                            }
                            else
                            {
                                applicationId = db.UpdateQuery(string.Format(@"INSERT INTO applications (application_assembly_name, user_id, application_date_ut, application_title, application_description, application_primitive, application_primitives, application_comment, application_rating) VALUES ('{0}', {1}, {2}, '{3}', '{4}', {5}, {6}, {7}, {8});",
                                    Mysql.Escape(repo), 0, tz.GetUnixTimeStamp(tz.Now), Mysql.Escape(newApplication.Title), Mysql.Escape(newApplication.Description), isPrimitive, (byte)newApplication.GetAppPrimitiveSupport(), newApplication.UsesComments, newApplication.UsesRatings));

                                try
                                {
                                    ApplicationEntry profileAe = new ApplicationEntry(core, null, "Profile");
                                    db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type_id, app_access) VALUES ({0}, {1}, '{2}', {3});",
                                        profileAe.ApplicationId, applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry)), 0x1111));
                                }
                                catch
                                {
                                }

                                try
                                {
                                    ApplicationEntry guestbookAe = new ApplicationEntry(core, null, "GuestBook");
                                    db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (application_id, item_id, item_type_id, app_access) VALUES ({0}, {1}, '{2}', {3});",
                                        guestbookAe.ApplicationId, applicationId, ItemKey.GetTypeId(typeof(ApplicationEntry)), 0x1111));
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

                                /*if (aii.ApplicationCommentTypes != null)
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
                                }*/

                                if (aii.ApplicationItemAccessPermissions != null)
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
                                }

                                db.UpdateQuery(string.Format(@"DELETE FROM application_slugs WHERE application_id = {0} AND slug_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                db.UpdateQuery(string.Format(@"DELETE FROM account_modules WHERE application_id = {0} AND module_updated_ut <> {1};",
                                    applicationId, updatedRaw));

                                /*db.UpdateQuery(string.Format(@"DELETE FROM comment_types WHERE application_id = {0} AND type_updated_ut <> {1};",
                                    applicationId, updatedRaw));*/

                                BoxSocial.Internals.Application.InstallTypes(core, loadApplication, applicationId);
                                BoxSocial.Internals.Application.InstallTables(core, loadApplication);

                            }
                            else
                            {
                                Console.WriteLine("error installing" + repo);
                            }
                        }
                    }
                }

                Console.WriteLine(repo + " has been installed.");

            }

            db.CloseConnection();
        }

        /// <summary>
        /// Save a text file
        /// </summary>
        /// <param name="fileToSave"></param>
        /// <param name="fileName"></param>
        private static void SaveTextFile(string fileToSave, string fileName)
        {
            StreamWriter myStreamWriter = File.CreateText(fileName);
            myStreamWriter.Write(fileToSave);
            myStreamWriter.Close();
        }
    }
}
