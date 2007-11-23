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
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /*
     * An object that describes an application
     */
    public abstract class Application : MarshalByRefObject, IAppInfo
    {

        protected Core core;

        public virtual void Initialise(Core core)
        {
        }

        public virtual AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.None;
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
                        Application newApplication = System.Activator.CreateInstance(type, new object[0]) as Application;

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

        public static List<ApplicationEntry> GetApplications(Core core, Primitive owner)
        {
            List<ApplicationEntry> applicationsList = new List<ApplicationEntry>();

            ushort readAccessLevel = owner.GetAccessLevel(core.session.LoggedInMember);
            long loggedIdUid = Member.GetMemberId(core.session.LoggedInMember);

            DataTable userApplicationsTable = core.db.SelectQuery(string.Format(@"SELECT {0}, {1}
                FROM applications ap, primitive_apps pa
                WHERE (pa.item_id = {2} AND pa.item_type = '{5}')
                    AND pa.application_id = ap.application_id
                    AND (pa.app_access & {3:0} OR (pa.item_id = {4} AND pa.item_type = 'USER'))",
                ApplicationEntry.APPLICATION_FIELDS, ApplicationEntry.USER_APPLICATION_FIELDS, owner.Id, readAccessLevel, loggedIdUid, Mysql.Escape(owner.Type)));

            foreach (DataRow applicationRow in userApplicationsTable.Rows)
            {
                applicationsList.Add(new ApplicationEntry(core.db, applicationRow));
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
                catch { }
            }
        }

    }
}
