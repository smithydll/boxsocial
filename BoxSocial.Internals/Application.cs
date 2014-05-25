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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// An object that describes an application
    /// </summary>
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
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ShowAttribute)))
                {
                    slugs.Add(((ShowAttribute)attr).Slug);
                }

                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(StaticShowAttribute)))
                {
                    slugs.Add(((StaticShowAttribute)attr).Slug);
                }
            }

            return slugs;
        }

        public List<ApplicationSlugInfo> GetSlugInformation()
        {
            List<ApplicationSlugInfo> slugs = new List<ApplicationSlugInfo>();
            Type type = this.GetType();

            foreach (MemberInfo mi in type.GetMembers(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                // Show Attribute
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ShowAttribute)))
                {
                    slugs.Add(new ApplicationSlugInfo(((ShowAttribute)attr)));
                }

                // Static Show Attribute
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(StaticShowAttribute)))
                {
                    slugs.Add(new ApplicationSlugInfo(((StaticShowAttribute)attr)));
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
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ShowAttribute)))
                {
                    /*if (((ShowAttribute)attr).Primitives & primitives != primitives)
                    {
                        continue;
                    }*/
                    if (((ShowAttribute)attr).Order > 0)
                    {
                        core.RegisterApplicationPage(((ShowAttribute)attr).Primitives, ((ShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), ((ShowAttribute)attr).Order, false);
                    }
                    else
                    {
                        i++;
                        core.RegisterApplicationPage(((ShowAttribute)attr).Primitives, ((ShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), i, false);
                    }
                }

                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(StaticShowAttribute)))
                {
                    if (((StaticShowAttribute)attr).Order > 0)
                    {
                        core.RegisterApplicationPage(AppPrimitives.None, ((StaticShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), ((StaticShowAttribute)attr).Order, true);
                    }
                    else
                    {
                        i++;
                        core.RegisterApplicationPage(AppPrimitives.None, ((StaticShowAttribute)attr).Slug, (Core.PageHandler)Core.PageHandler.CreateDelegate(typeof(Core.PageHandler), this, mi), i, true);
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
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ShowAttribute)))
                {
                    i++;
                    if (string.IsNullOrEmpty(((ShowAttribute)attr).Stub))
                    {
                        aii.AddSlug(this.Stub, ((ShowAttribute)attr).Slug, ((ShowAttribute)attr).Primitives, false);
                    }
                    else
                    {
                        aii.AddSlug((ShowAttribute)attr);
                    }
                }

                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(StaticShowAttribute)))
                {
                    i++;
                    aii.AddSlug((StaticShowAttribute)attr);
                }
            }
        }
		
		public static void InstallTypes(Core core, Assembly asm, long applicationId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Type[] types = asm.GetTypes();

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(Item)) || type.GetCustomAttributes(typeof(PseudoPrimitiveAttribute), false).Length == 1)
                {
					SelectQuery query = new SelectQuery(Item.GetTable(typeof(ItemType)));
					query.AddCondition("type_namespace", type.FullName);

                    if (core.Db.Query(query).Rows.Count == 0)
                    {
                        InsertQuery iQuery = new InsertQuery(Item.GetTable(typeof(ItemType)));
                        iQuery.AddField("type_namespace", type.FullName);
                        iQuery.AddField("type_application_id", applicationId);
                        iQuery.AddField("type_commentable", typeof(ICommentableItem).IsAssignableFrom(type));
                        iQuery.AddField("type_likeable", typeof(ILikeableItem).IsAssignableFrom(type));
                        iQuery.AddField("type_rateable", typeof(IRateableItem).IsAssignableFrom(type));
                        iQuery.AddField("type_subscribeable", typeof(ISubscribeableItem).IsAssignableFrom(type));
                        iQuery.AddField("type_viewable", typeof(IViewableItem).IsAssignableFrom(type));
                        iQuery.AddField("type_shareable", typeof(IShareableItem).IsAssignableFrom(type));
                        iQuery.AddField("type_primitive", type.IsSubclassOf(typeof(Primitive)));

                        core.Db.Query(iQuery);
                    }
                    else
                    {
                        UpdateQuery uQuery = new UpdateQuery(Item.GetTable(typeof(ItemType)));
                        uQuery.AddCondition("type_namespace", type.FullName);
                        uQuery.AddCondition("type_application_id", applicationId);
                        uQuery.AddField("type_commentable", typeof(ICommentableItem).IsAssignableFrom(type));
                        uQuery.AddField("type_likeable", typeof(ILikeableItem).IsAssignableFrom(type));
                        uQuery.AddField("type_rateable", typeof(IRateableItem).IsAssignableFrom(type));
                        uQuery.AddField("type_subscribeable", typeof(ISubscribeableItem).IsAssignableFrom(type));
                        uQuery.AddField("type_viewable", typeof(IViewableItem).IsAssignableFrom(type));
                        uQuery.AddField("type_shareable", typeof(IShareableItem).IsAssignableFrom(type));
                        uQuery.AddField("type_primitive", type.IsSubclassOf(typeof(Primitive)));

                        core.Db.Query(uQuery);
                    }
				}
			}
		}

        public static void InstallTables(Core core, Assembly asm)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Type[] types = asm.GetTypes();

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(Item)) && type.GetCustomAttributes(typeof(DataTableAttribute), false).Length == 1)
                {
                    string table = Item.GetTable(type);

                    if (!string.IsNullOrEmpty(table))
                    {
                        List<DataFieldInfo> dataFields = Item.GetFields(type);
                        if (core.Db.TableExists(table))
                        {
                            Dictionary<string, DataFieldInfo> columns = core.Db.GetColumns(table);

                            List<DataFieldInfo> newFields = new List<DataFieldInfo>();

                            foreach (DataFieldInfo field in dataFields)
                            {
                                if (!columns.ContainsKey(field.Name))
                                {
                                    newFields.Add(field);
                                }
                                else
                                {
                                    if ((!columns[field.Name].Type.Equals(field.Type)) || ((columns[field.Name].Type.Equals(typeof(string)) || columns[field.Name].Type.Equals(typeof(char[]))) && columns[field.Name].Length != field.Length))
                                    {
                                        //Console.WriteLine(field.Name + ": " + columns[field.Name].Type + " (" + columns[field.Name].Length + "), " + field.Type + " (" + field.Length + ")");
                                        core.Db.ChangeColumn(table, field);
                                    }
                                }
                            }

                            if (newFields.Count > 0)
                            {
                                core.Db.AddColumns(table, newFields);
                            }

                            core.Db.UpdateTableKeys(table, dataFields);
                        }
                        else
                        {
                            core.Db.CreateTable(table, dataFields);
                        }
                    }
                }
            }
        }

        public virtual void Initialise(Core core)
        {
        }

        public virtual void InitialisePrimitive(Primitive owner)
        {
        }

        public abstract ApplicationInstallationInfo Install();

        public ApplicationInstallationInfo GetInstallInfo()
        {
            ApplicationInstallationInfo aii = new ApplicationInstallationInfo();

            Type[] types = this.GetType().Assembly.GetTypes();

            foreach (Type type in types)
            {
                if (type.IsSubclassOf(typeof(AccountModule)))
                {
                    foreach (Attribute attr in type.GetCustomAttributes(typeof(AccountModuleAttribute), false))
                    {
                        aii.AddModule(((AccountModuleAttribute)attr).Name);
                    }
                }
            }

            List<ApplicationSlugInfo> slugs = GetSlugInformation();
            foreach (ApplicationSlugInfo slug in slugs)
            {
                aii.AddSlug(slug.Stub, slug.SlugEx, slug.Primitives, slug.IsStatic);
            }

            return aii;
        }

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

        public abstract Dictionary<string, PageSlugAttribute> PageSlugs
        {
            get;
        }

        public Dictionary<string, PageSlugAttribute> GetPageSlugs(AppPrimitives primitive)
        {
            Dictionary<string, PageSlugAttribute> slugs = null;

            if (PageSlugs != null)
            {
                slugs = PageSlugs;
            }
            else
            {
                slugs = new Dictionary<string, PageSlugAttribute>(StringComparer.Ordinal);
            }


            /* Discover page slugs */
            Type type = this.GetType();

            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ShowAttribute)))
                {
                    if ((((ShowAttribute)attr).Primitives & primitive) == primitive)
                    {
                        foreach (Attribute psAttr in Attribute.GetCustomAttributes(mi, typeof(PageSlugAttribute)))
                        {
                            slugs.Add(((ShowAttribute)attr).CleanSlug, ((PageSlugAttribute)psAttr));
                        }
                    }
                }
            }

            return slugs;
        }

        /// <summary>
        /// A 16x16 image file for the application icon.
        /// </summary>
        public abstract System.Drawing.Image Icon
        {
            get;
        }

        public abstract byte[] SvgIcon
        {
            get;
        }

        /// <summary>
        /// A stylesheet for the application
        /// </summary>
        public abstract string StyleSheet
        {
            get;
        }

        /// <summary>
        /// Clientside javascript for the application
        /// </summary>
        public abstract string JavaScript
        {
            get;
        }

        public static string InitialiseApplications(Core core, AppPrimitives primitive)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            string debug = string.Empty;

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
            //ushort readAccessLevel = owner.GetAccessLevel(core.Session.LoggedInMember);
            //long loggedIdUid = core.LoggedInMemberId;
			
            /*DataTable userApplicationsTable = core.db.Query(string.Format(@"SELECT {0}, {1}
                FROM applications ap, primitive_apps pa
                WHERE (pa.item_id = {2} AND pa.item_type_id = {5})
                    AND pa.application_id = ap.application_id
                    AND ap.application_primitives & {6:0}",
                ApplicationEntry.APPLICATION_FIELDS, ApplicationEntry.USER_APPLICATION_FIELDS, owner.Id, readAccessLevel, loggedIdUid, owner.TypeId, (byte)owner.AppPrimitive, ItemKey.GetTypeId(typeof(User))));*/

            SelectQuery query = Item.GetSelectQueryStub(typeof(PrimitiveApplicationInfo));
            query.AddFields(Item.GetFieldsPrefixed(typeof(ApplicationEntry)));
            query.AddJoin(JoinTypes.Inner, Item.GetTable(typeof(ApplicationEntry)), "application_id", "application_id");
            query.AddCondition(new DataField(typeof(PrimitiveApplicationInfo), "item_id"), owner.ItemKey.Id);
            query.AddCondition(new DataField(typeof(PrimitiveApplicationInfo), "item_type_id"), owner.ItemKey.TypeId);
            query.AddCondition("application_update", false);
            query.AddCondition(new QueryOperation("application_primitives", QueryOperations.BinaryAnd, (byte)owner.AppPrimitive), ConditionEquality.NotEqual, false);

            DataTable userApplicationsTable = core.Db.Query(query);

            return userApplicationsTable;
        }

        private static DataTable GetStaticApplicationRows(Core core)
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(ApplicationEntry));

            DataTable staticApplicationsTable = core.Db.Query(query);

            return staticApplicationsTable;
        }

        public static List<ApplicationEntry> GetModuleApplications(Core core, Primitive owner)
        {
            List<ApplicationEntry> applicationsList = new List<ApplicationEntry>();
            Dictionary<long, ApplicationEntry> applicationsDictionary = new Dictionary<long, ApplicationEntry>();

            DataTable userApplicationsTable = GetApplicationRows(core, owner);

            if (userApplicationsTable.Rows.Count > 0)
            {
                List<long> applicationIds = new List<long>();
                foreach (DataRow applicationRow in userApplicationsTable.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, applicationRow);
                    applicationsList.Add(ae);
                    applicationsDictionary.Add(ae.ApplicationId, ae);

                    applicationIds.Add(ae.Id);
                }

                SelectQuery query = ControlPanelModuleRegister.GetSelectQueryStub(typeof(ControlPanelModuleRegister));
                query.AddCondition("application_id", ConditionEquality.In, applicationIds);
                query.AddSort(SortOrder.Ascending, "application_id");

                DataTable modulesTable = core.Db.Query(query);

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
            Dictionary<long, ApplicationEntry> applicationsDictionary = new Dictionary<long, ApplicationEntry>();

            DataTable userApplicationsTable = GetApplicationRows(core, owner);

            if (userApplicationsTable.Rows.Count > 0)
            {
                List<long> applicationIds = new List<long>();
                foreach (DataRow applicationRow in userApplicationsTable.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, applicationRow);
                    applicationsList.Add(ae);
                    applicationsDictionary.Add(ae.ApplicationId, ae);

                    applicationIds.Add(ae.ApplicationId);
                }

                /*DataTable applicationSlugsTable = core.db.Query(string.Format(@"SELECT {0}
                    FROM application_slugs al
                    WHERE application_id IN ({1})
                    AND slug_primitives & {2:0}
                    ORDER BY application_id;",
                    ApplicationEntry.APPLICATION_SLUG_FIELDS, applicationIds, (byte)owner.AppPrimitive));*/

                SelectQuery query = Item.GetSelectQueryStub(typeof(ApplicationSlug));
                query.AddCondition("application_id", ConditionEquality.In, applicationIds);
                query.AddCondition(new QueryOperation("slug_primitives", QueryOperations.BinaryAnd, (byte)owner.AppPrimitive), ConditionEquality.NotEqual, false);
                query.AddCondition("slug_static", false);
                query.AddSort(SortOrder.Ascending, "application_id");

                DataTable applicationSlugsTable = core.Db.Query(query);

                foreach (DataRow slugRow in applicationSlugsTable.Rows)
                {
                    applicationsDictionary[(long)slugRow["application_id"]].LoadSlugEx((string)slugRow["slug_slug_ex"]);
                }
            }

            return applicationsList;
        }

        public static List<ApplicationEntry> GetStaticApplications(Core core)
        {
            List<ApplicationEntry> applicationsList = new List<ApplicationEntry>();
            Dictionary<long, ApplicationEntry> applicationsDictionary = new Dictionary<long, ApplicationEntry>();

            DataTable userApplicationsTable = GetStaticApplicationRows(core);

            if (userApplicationsTable.Rows.Count > 0)
            {
                List<long> applicationIds = new List<long>();
                foreach (DataRow applicationRow in userApplicationsTable.Rows)
                {
                    ApplicationEntry ae = new ApplicationEntry(core, applicationRow);
                    applicationsList.Add(ae);
                    applicationsDictionary.Add(ae.ApplicationId, ae);

                    applicationIds.Add(ae.ApplicationId);
                }

                /*DataTable applicationSlugsTable = core.db.Query(string.Format(@"SELECT {0}
                    FROM application_slugs al
                    WHERE application_id IN ({1})
                    AND slug_primitives & {2:0}
                    ORDER BY application_id;",
                    ApplicationEntry.APPLICATION_SLUG_FIELDS, applicationIds, (byte)owner.AppPrimitive));*/

                SelectQuery query = Item.GetSelectQueryStub(typeof(ApplicationSlug));
                query.AddCondition("application_id", ConditionEquality.In, applicationIds);
                //query.AddCondition(new QueryOperation("slug_primitives", QueryOperations.BinaryAnd, (byte)AppPrimitives.None), ConditionEquality.NotEqual, false);
                // Zero anyway, could be anything
                query.AddCondition("slug_static", true);
                query.AddSort(SortOrder.Ascending, "application_id");

                DataTable applicationSlugsTable = core.Db.Query(query);

                foreach (DataRow slugRow in applicationSlugsTable.Rows)
                {
                    applicationsDictionary[(long)slugRow["application_id"]].LoadSlugEx((string)slugRow["slug_slug_ex"]);
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
            if (core == null)
            {
                throw new NullCoreException();
            }

            Stopwatch load = new Stopwatch();
            load.Start();
            foreach (ApplicationEntry ae in applicationsList)
            {
                if (!core.LoadedApplication(ae))
                {
                    if (ae.SlugMatch(uri))
                    {
                        try
                        {
                            string assemblyPath;
                            if (ae.IsPrimitive)
                            {
                                assemblyPath = Path.Combine(core.Http.AssemblyPath, string.Format("{0}.dll", ae.AssemblyName));
                            }
                            else
                            {
                                assemblyPath = Path.Combine(core.Http.AssemblyPath, Path.Combine("applications", string.Format("{0}.dll", ae.AssemblyName)));
                            }
                            Assembly assembly = Assembly.LoadFrom(assemblyPath);

                            Type[] types = assembly.GetTypes();
                            foreach (Type type in types)
                            {
                                if (type.IsSubclassOf(typeof(Application)))
                                {
                                    Application newApplication = System.Activator.CreateInstance(type, new object[] { core }) as Application;

                                    if (newApplication != null)
                                    {
                                        if ((newApplication.GetAppPrimitiveSupport() & primitive) == primitive
                                            || primitive == AppPrimitives.Any)
                                        {
                                            newApplication.Initialise(core);
                                            core.Template.AddPageAssembly(assembly);

                                            if (ae.HasStyleSheet)
                                            {
                                                VariableCollection styleSheetVariableCollection = core.Template.CreateChild("style_sheet_list");

                                                styleSheetVariableCollection.Parse("URI", @"/styles/applications/" + ae.Key + @".css");
                                            }

                                            if (ae.HasJavascript)
                                            {
                                                VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");

                                                javaScriptVariableCollection.Parse("URI", @"/scripts/" + ae.Key + @".js");
                                            }

                                            /* Initialise prose class for the application */
                                            core.Prose.AddApplication(ae.Key);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //core.Http.Write(ex.ToString() + "<hr />");
                            // -- DEBUG HERE FOR APPLICATION LOADER --
                        }
                    }
                }
            }
            load.Stop();
        }

        public static Application GetApplication(Core core, AppPrimitives primitive, ApplicationEntry ae)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            try
            {
                string assemblyPath;
                if (ae.IsPrimitive)
                {
					if (core.Http != null)
					{
						assemblyPath = Path.Combine(core.Http.AssemblyPath, string.Format("{0}.dll", ae.AssemblyName));
					}
					else
					{
						//assemblyPath = string.Format("/var/www/bin/{0}.dll", ae.AssemblyName);
                        assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ae.AssemblyName + ".dll");
					}
                }
                else
                {
					if (core.Http != null)
					{
                        assemblyPath = Path.Combine(core.Http.AssemblyPath, Path.Combine("applications", string.Format("{0}.dll", ae.AssemblyName)));
					}
					else
					{
						//assemblyPath = string.Format("/var/www/bin/applications/{0}.dll", ae.AssemblyName);
                        assemblyPath = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "applications"), ae.AssemblyName + ".dll");
					}
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
                // TODO DEBUG HERE
                Console.WriteLine(ex.ToString());
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ApplicationEntry GetExecutingApplication(Core core, Primitive installee)
        {
            return core.GetApplication(Assembly.GetCallingAssembly().GetName().Name);
        }

        public static void LoadApplication(Core core, AppPrimitives primitive, ApplicationEntry ae)
        {
            if (!core.LoadedApplication(ae))
            {
                Application newApplication = GetApplication(core, primitive, ae);

                if (newApplication != null)
                {
                    if ((newApplication.GetAppPrimitiveSupport() & primitive) == primitive
                        || primitive == AppPrimitives.Any)
                    {
                        newApplication.Initialise(core);
                        core.Template.AddPageAssembly(ae.Assembly);

                        if (ae.HasStyleSheet)
                        {
                            VariableCollection styleSheetVariableCollection = core.Template.CreateChild("style_sheet_list");

                            styleSheetVariableCollection.Parse("URI", @"/styles/applications/" + ae.Key + @".css");
                        }

                        if (ae.HasJavascript)
                        {
                            VariableCollection javaScriptVariableCollection = core.Template.CreateChild("javascript_list");

                            javaScriptVariableCollection.Parse("URI", @"/scripts/" + ae.Key + @".js");
                        }

                        /* Initialise prose class for the application */
                        core.Prose.AddApplication(ae.Key);
                    }
                }
            }
        }

    }
}
