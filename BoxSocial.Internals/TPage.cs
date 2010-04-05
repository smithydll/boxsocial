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
using System.Collections;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Configuration;
using System.Drawing;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public enum StorageFileType
    {
        Original,
        Display,
        Thumbnail,
        Icon,
        Tile,
        Tiny,
    }

    public enum PageSignature
    {
        viewgroup,
        viewgroupphoto,
        viewnetwork,
        viewnetworkphoto,
        viewprofile,
        viewblog,
        viewpage,
        today,
        viewapplication,
    }

    /// <summary>
    /// Template Page
    /// </summary>
    public abstract class TPage : System.Web.UI.Page
    {
        private string pageTitle = "ZinZam • A Global Community";
        private string canonicalUri;

        public Template template;
        public Mysql db;
        public User loggedInMember;
        public SessionState session;
        protected Random rand;
        Stopwatch timer;
        public int page;
        public UnixTime tz;
        protected Core core;
        public PageSignature Signature;
        private bool isAjax;
        private bool pageEnded;

        public Core Core
        {
            get
            {
                return core;
            }
        }

        public string PageTitle
        {
            get
            {
                return pageTitle;
            }
            set
            {
                pageTitle = "ZinZam • " + HttpUtility.HtmlEncode(value);
            }
        }

        public string CanonicalUri
        {
            get
            {
                return canonicalUri;
            }
            set
            {
                if (core != null && core.Uri != null)
                {
                    canonicalUri = core.Uri.StripSid(value);
                }
                else
                {
                    canonicalUri = value;
                }
            }
        }

        public bool IsAjax
        {
            get
            {
                return isAjax;
            }
        }

        public static Object StoragePathLock = new object();
        public static string StoragePath = null;

        public static string GetStorageFilePath(string fileName)
        {
            return GetStorageFilePath(fileName, StorageFileType.Original);
        }

        public static string GetStorageFilePath(string fileName, StorageFileType fileType)
        {
            string topLevelDirectory = fileName.Substring(0, 1).ToLower();
            string secondLevelDirectory = fileName.Substring(0, 2).ToLower();
            switch (fileType)
            {
                case StorageFileType.Display:
                    return Path.Combine(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_display"), fileName);
                case StorageFileType.Thumbnail:
                    return Path.Combine(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_thumb"), fileName);
                case StorageFileType.Icon:
                    return Path.Combine(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_icon"), fileName);
                case StorageFileType.Tile:
                    return Path.Combine(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_tile"), fileName);
                case StorageFileType.Tiny:
                    return Path.Combine(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_tiny"), fileName);
                default:
                    return Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), fileName);
            }
        }

        public static void EnsureStoragePathExists(string fileName)
        {
            EnsureStoragePathExists(fileName, StorageFileType.Original);
        }

        public static void EnsureStoragePathExists(string fileName, StorageFileType fileType)
        {
            string topLevelDirectory = fileName.Substring(0, 1).ToLower();
            string secondLevelDirectory = fileName.Substring(0, 2).ToLower();

            if (!Directory.Exists(Path.Combine(StoragePath, topLevelDirectory)))
            {
                Directory.CreateDirectory(Path.Combine(StoragePath, topLevelDirectory));
            }

            if (!Directory.Exists(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory)))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory));
            }

            switch (fileType)
            {
                case StorageFileType.Display:
                    if (!Directory.Exists(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_display")))
                    {
                        Directory.CreateDirectory(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_display"));
                    }
                    break;
                case StorageFileType.Thumbnail:
                    if (!Directory.Exists(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_thumb")))
                    {
                        Directory.CreateDirectory(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_thumb"));
                    }
                    break;
                case StorageFileType.Icon:
                    if (!Directory.Exists(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_icon")))
                    {
                        Directory.CreateDirectory(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_icon"));
                    }
                    break;
                case StorageFileType.Tile:
                    if (!Directory.Exists(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_tile")))
                    {
                        Directory.CreateDirectory(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_tile"));
                    }
                    break;
                case StorageFileType.Tiny:
                    if (!Directory.Exists(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_tiny")))
                    {
                        Directory.CreateDirectory(Path.Combine(Path.Combine(Path.Combine(StoragePath, topLevelDirectory), secondLevelDirectory), "_tiny"));
                    }
                    break;
            }
        }

        public TPage()
        {

            timer = new Stopwatch();
            timer.Start();
            rand = new Random();

            lock (StoragePathLock)
            {
                if (StoragePath == null)
                {
                    StoragePath = Server.MapPath(WebConfigurationManager.AppSettings["storage-path"]);
                }
            }
            db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                WebConfigurationManager.AppSettings["mysql-password"],
                WebConfigurationManager.AppSettings["mysql-database"],
                WebConfigurationManager.AppSettings["mysql-host"]);
            template = new Template(Server.MapPath("./templates/"), "");
            core = new Core(db, template);
            //Core.DB = db;
            core.page = this;
            //Functions.Core = core;
            //Display.Core = core;
            //Email.Core = core;
            //Ajax.Core = core;
            //Linker.Core = core;
            core.Bbcode = new Bbcode(core);
            core.Functions = new Functions(core);
            core.Display = new Display(core);
            core.Email = new Email(core);
            core.Ajax = new Ajax(core);
            core.Uri = new Linker(core);

            HttpContext httpContext = HttpContext.Current;
            string[] redir = httpContext.Request.RawUrl.Split(';');

            if (redir.Length > 1)
            {
                core.PagePath = redir[1];
                Uri cUri = new Uri(core.PagePath);
                core.PagePath = cUri.AbsolutePath.TrimEnd(new char[] { '/' });
            }
            else
            {
                if (httpContext.Request.Url.Host.ToLower() == Linker.Domain)
                {
                    Core.PagePath = httpContext.Request.RawUrl.TrimEnd(new char[] { '/' });
                }
                else
                {
                    Core.PagePath = "/";
                }
            }

            session = new SessionState(Core, db, User, HttpContext.Current.Request, HttpContext.Current.Response);
            loggedInMember = session.LoggedInMember;
            tz = new UnixTime(core, UnixTime.UTC_CODE);
            core.Display.page = this;

            core.Session = session;
            core.CoreDomain = AppDomain.CurrentDomain;

            // Ensure that core applications are loaded
            Stopwatch load = new Stopwatch();
            load.Start();
            try
            {
                System.Reflection.Assembly.Load("Profile");
                System.Reflection.Assembly.Load("Groups");
                System.Reflection.Assembly.Load("Networks");
            }
            catch
            {
            }
            load.Stop();

            if (loggedInMember != null)
            {
                tz = loggedInMember.GetTimeZone;
            }

            // move it here
            core.Tz = tz;

            isAjax = (HttpContext.Current.Request.QueryString["ajax"] == "true");
            
            // As a security measure we use the http object to prevent
            // applications hijacking the response output
            core.Http = new Http();
            Template.Path = core.Http.TemplatePath;
            core.Prose = new Prose();
            core.Prose.Initialise(core, "en");
            
            AssemblyName[] assemblies = Assembly.Load(new AssemblyName("BoxSocial.FrontEnd")).GetReferencedAssemblies();

            foreach (AssemblyName an in assemblies)
            {
                core.Prose.AddApplication(an.Name);
                Assembly asm = Assembly.Load(an);
                template.AddPageAssembly(asm);
            }

            template.SetProse(core.Prose);
            
            page = core.Functions.RequestInt("p", 1);
        }

        public TPage(string templateFile)
            : this()
        {
            template.SetTemplate(templateFile);
            core.Template = template;
        }

        protected void BeginStaticPage()
        {
            List<ApplicationEntry> applications = BoxSocial.Internals.Application.GetStaticApplications(core);
            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.None, core.PagePath, applications);
        }

        public void EndResponse()
        {
            if (!pageEnded)
            {
                pageEnded = true;

                core.Display.Header(this);
                long templateStart = timer.ElapsedTicks;
                core.Http.Write(template);
                double templateSeconds = (timer.ElapsedTicks - templateStart) / 10000000.0;
                timer.Stop();
                double seconds = (timer.ElapsedTicks) / 10000000.0;
                if (core != null)
                {
                    if (core.LoggedInMemberId <= 3 && core.LoggedInMemberId != 0)
                    {
                        //HttpContext.Current.Response.Write(string.Format("<p style=\"background-color: white; color: black;\">{0} seconds &bull; {1} queries in {2} seconds &bull; template in {3} seconds</p>", seconds, db.GetQueryCount(), db.GetQueryTime(), templateSeconds));
                    }
                    //HttpContext.Current.Response.Write(db.QueryList.ToString());
                }

                if (db != null)
                {
                    db.CloseConnection();
                }

                core.Prose.Close();
                //core.Dispose();
                //core = null;

                core.Http.End();
                //System.Threading.Thread.CurrentThread.Abort();
            }
        }

        ~TPage()
        {
            // destructor
            if (db != null)
            {
                db.CloseConnection();
            }

            core.Prose.Close();
            //core.Dispose();
            //core = null;
        }
    }

    public class ShowPageEventArgs : EventArgs
    {
        protected Core core;
        protected TPage page;

        public Core Core
        {
            get
            {
                return core;
            }
        }

        public Mysql Db
        {
            get
            {
                return core.Db;
            }
        }

        public TPage Page
        {
            get
            {
                return page;
            }
        }

        public Template Template
        {
            get
            {
                return core.Template;
            }
        }

        public ShowPageEventArgs(TPage page)
        {
            this.page = page;
            this.core = page.Core;
        }
    }
}
