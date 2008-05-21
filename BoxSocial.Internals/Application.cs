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
using BoxSocial.IO;

namespace BoxSocial.Internals
{

    /* DONE:
     * ALTER TABLE `zinzam0_zinzam`.`applications` ADD COLUMN `application_rating` TINYINT(1) UNSIGNED NOT NULL AFTER `application_comment`;
     * /

    /*
     * An object that describes an application
     */
    public abstract class Application : MarshalByRefObject, IAppInfo
    {
        protected Core core;

        public Application(Core core)
        {
            this.core = core;

            RegisterPages();
        }

        public List<string> GetSlugs()
        {
            List<string> slugs = new List<string>();
            Type type = this.GetType();

            foreach (MemberInfo mi in type.GetMembers(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi))
                {
                    if (attr.GetType() == typeof(ShowAttribute))
                    {
                        slugs.Add(((ShowAttribute)attr).Slug);
                    }
                }
            }

            return slugs;
        }

        public void RegisterPages()
        {
            Type type = this.GetType();

            int i = 0;
            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi))
                {
                    if (attr.GetType() == typeof(ShowAttribute))
                    {
                        if (((ShowAttribute)attr).Order > 0)
                        {
                            core.RegisterApplicationPage(((ShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), ((ShowAttribute)attr).Order);
                        }
                        else
                        {
                            i++;
                            core.RegisterApplicationPage(((ShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), i);
                        }
                    }
                }
            }
        }

        public void InstallPages(ApplicationInstallationInfo aii)
        {
            Type type = this.GetType();

            int i = 0;
            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi))
                {
                    if (attr.GetType() == typeof(ShowAttribute))
                    {
                        i++;
                        //core.RegisterApplicationPage(((ShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), i);
                        if (string.IsNullOrEmpty(((ShowAttribute)attr).Stub))
                        {
                            aii.AddSlug(this.Stub, ((ShowAttribute)attr).Slug, ((ShowAttribute)attr).Primitives);
                        }
                        else
                        {
                            aii.AddSlug(((ShowAttribute)attr).Stub, ((ShowAttribute)attr).Slug, ((ShowAttribute)attr).Primitives);
                        }
                    }
                }
            }
        }

        public static ApplicationEntry Entry
        {
            get
            {

                string assemblyName = Assembly.GetCallingAssembly().GetName().Name;

                if (!Functions.core.ApplicationEntries.ContainsKey(assemblyName))
                {
                    Functions.core.LoadApplicationEntry(assemblyName);
                }

                return Functions.core.ApplicationEntries[assemblyName];
            }
        }

        public virtual void Initialise(Core core)
        {
        }

        public abstract ApplicationInstallationInfo Install();

        public virtual AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.None;
        }

        /// <summary>
        /// Application title
        /// </summary>
        public abstract string Title
        {
            get;
        }

        /// <summary>
        /// Default stub. When no stub is specified, the default one is used.
        /// Can be overridden by specifying an overriding stub in the
        /// appropriate Show attribute overload.
        /// </summary>
        public abstract string Stub
        {
            get;
        }

        /// <summary>
        /// A description of the application
        /// </summary>
        /// <remarks>Uses BBcode</remarks>
        public abstract string Description
        {
            get;
        }

        /// <summary>
        /// A flag indicating whether the application uses the comments module
        /// </summary>
        public abstract bool UsesComments
        {
            get;
        }

        /// <summary>
        /// A flag indicating whether the application uses the ratings module
        /// </summary>
        public abstract bool UsesRatings
        {
            get;
        }

        public abstract Dictionary<string, string> PageSlugs
        {
            get;
        }

        /// <summary>
        /// A 16x16 image file for the application icon.
        /// </summary>
        public abstract System.Drawing.Image Icon
        {
            get;
        }

        public static string InitialiseApplications(Core core, AppPrimitives primitive)
        {
            string debug = "";

            Assembly[] assemblies = core.CoreDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Application)))
                    {
                        Application newApplication = System.Activator.CreateInstance(type, new object[] {core}) as Application;

                        if (newApplication != null)
                        {
                            if ((newApplication.GetAppPrimitiveSupport() & primitive) == primitive
                                || primitive == AppPrimitives.Any)
                            {
                                newApplication.Initialise(core);
                                debug += ", +" + type.ToString();
                            }
                        }
                        else
                        {
                            debug += ", -" + type.ToString();
                        }
                    }
                }
            }
            return debug;
        }

        private static DataTable GetApplicationRows(Core core, Primitive owner)
        {
            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);
            long loggedIdUid = core.LoggedInMemberId;

            DataTable userApplicationsTable = core.db.Query(string.Format(@"SELECT {0}, {1}
                FROM applications ap, primitive_apps pa
                WHERE (pa.item_id = {2} AND pa.item_type = '{5}')
                    AND pa.application_id = ap.application_id
                    AND ap.application_primitives & {6:0}
                    AND (pa.app_access & {3:0} OR (pa.item_id = {4} AND pa.item_type = 'USER'))",
                ApplicationEntry.APPLICATION_FIELDS, ApplicationEntry.USER_APPLICATION_FIELDS, owner.Id, readAccessLevel, loggedIdUid, Mysql.Escape(owner.Type), (byte)owner.AppPrimitive));
            return userApplicationsTable;
        }

        public static List<ApplicationEntry> GetModuleApplications(Core core, Primitive owner)
        {
            List<ApplicationEntry> applicationsList = new List<ApplicationEntry>();
            Dictionary<int, ApplicationEntry> applicationsDictionary = new Dictionary<int, ApplicationEntry>();

            DataTable userApplicationsTable = GetApplicationRows(core, owner);

            if (userApplicationsTable.Rows.Count > 0)
            {
                string applicationIds = "";
                foreach (DataRow applicationRow in userApplicationsTable.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, applicationRow);
                    applicationsList.Add(ae);
                    applicationsDictionary.Add(ae.ApplicationId, ae);

                    if (applicationIds == "")
                    {
                        applicationIds = ae.ApplicationId.ToString();
                    }
                    else
                    {
                        applicationIds = string.Format("{0}, {1}",
                            applicationIds, ae.ApplicationId);
                    }
                }

                DataTable modulesTable = core.db.Query(string.Format(@"SELECT am.module_module, am.application_id
                    FROM account_modules am
                    WHERE am.application_id IN ({0})
                    ORDER BY application_id;",
                    applicationIds));

                foreach (DataRow moduleRow in modulesTable.Rows)
                {
                    applicationsDictionary[(int)moduleRow["application_id"]].AddModule((string)moduleRow["module_module"]);
                }
            }

            return applicationsList;
        }

        public static List<ApplicationEntry> GetApplications(Core core, Primitive owner)
        {
            List<ApplicationEntry> applicationsList = new List<ApplicationEntry>();
            Dictionary<int, ApplicationEntry> applicationsDictionary = new Dictionary<int, ApplicationEntry>();

            DataTable userApplicationsTable = GetApplicationRows(core, owner);

            if (userApplicationsTable.Rows.Count > 0)
            {
                string applicationIds = "";
                foreach (DataRow applicationRow in userApplicationsTable.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, applicationRow);
                    applicationsList.Add(ae);
                    applicationsDictionary.Add(ae.ApplicationId, ae);

                    if (applicationIds == "")
                    {
                        applicationIds = ae.ApplicationId.ToString();
                    }
                    else
                    {
                        applicationIds = string.Format("{0}, {1}",
                            applicationIds, ae.ApplicationId);
                    }
                }

                DataTable applicationSlugsTable = core.db.Query(string.Format(@"SELECT {0}
                    FROM application_slugs al
                    WHERE application_id IN ({1})
                    AND slug_primitives & {2:0}
                    ORDER BY application_id;",
                    ApplicationEntry.APPLICATION_SLUG_FIELDS, applicationIds, (byte)owner.AppPrimitive));

                foreach (DataRow slugRow in applicationSlugsTable.Rows)
                {
                    applicationsDictionary[(int)slugRow["application_id"]].LoadSlugEx((string)slugRow["slug_slug_ex"]);
                }
            }

            return applicationsList;
        }

        public static void LoadApplications(List<ApplicationEntry> applicationsList)
        {
            foreach (ApplicationEntry ae in applicationsList)
            {
                try
                {
                    System.Reflection.Assembly.Load(ae.AssemblyName);
                }
                catch
                {
                }
            }
        }

        public static void LoadApplications(Core core, AppPrimitives primitive, string uri, List<ApplicationEntry> applicationsList)
        {
            foreach (ApplicationEntry ae in applicationsList)
            {
                if (ae.SlugMatch(uri))
                {
                    try
                    {
                        string assemblyPath;
                        if (ae.IsPrimitive)
                        {
                            assemblyPath = HttpContext.Current.Server.MapPath(string.Format("/bin/{0}.dll", ae.AssemblyName));
                        }
                        else
                        {
                            assemblyPath = HttpContext.Current.Server.MapPath(string.Format("/bin/applications/{0}.dll", ae.AssemblyName));
                        }
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);

                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            if (type.IsSubclassOf(typeof(Application)))
                            {
                                Application newApplication = System.Activator.CreateInstance(type, new object[] {core}) as Application;

                                if (newApplication != null)
                                {
                                    if ((newApplication.GetAppPrimitiveSupport() & primitive) == primitive
                                        || primitive == AppPrimitives.Any)
                                    {
                                        newApplication.Initialise(core);
                                        core.template.AddPageAssembly(assembly);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        HttpContext.Current.Response.Write(ex.ToString() + "<hr />");
                    }
                }
            }
        }

        public static Application GetApplication(Core core, AppPrimitives primitive, ApplicationEntry ae)
        {
            try
            {
                string assemblyPath;
                if (ae.IsPrimitive)
                {
                    assemblyPath = HttpContext.Current.Server.MapPath(string.Format("/bin/{0}.dll", ae.AssemblyName));
                }
                else
                {
                    assemblyPath = HttpContext.Current.Server.MapPath(string.Format("/bin/applications/{0}.dll", ae.AssemblyName));
                }
                Assembly assembly = Assembly.LoadFrom(assemblyPath);

                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(typeof(Application)))
                    {
                        Application newApplication = System.Activator.CreateInstance(type, new object[] {core}) as Application;

                        if (newApplication != null)
                        {
                            return newApplication;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HttpContext.Current.Response.Write(ex.ToString());
            }
            return null;
        }

        public static ApplicationEntry GetExecutingApplication(Core core, Primitive installee)
        {
            return new ApplicationEntry(core, installee, Assembly.GetCallingAssembly().GetName().Name);
        }

        /*public static ApplicationEntry GetExecutingApplication(Core core, Primitive installee)
        {
            return GetExecutingApplication(core, installee);
        }*/

        public static void LoadApplication(Core core, AppPrimitives primitive, ApplicationEntry ae)
        {
            Application newApplication = GetApplication(core, primitive, ae);

            if (newApplication != null)
            {
                if ((newApplication.GetAppPrimitiveSupport() & primitive) == primitive
                    || primitive == AppPrimitives.Any)
                {
                    newApplication.Initialise(core);
                }
            }
        }

    }
}
