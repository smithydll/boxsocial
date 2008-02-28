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
using System.Web;
using System.Web.Configuration;
using System.Web.Security;
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
        private string pageTitle = "ZinZam &bull; Your point in user space";

        public Template template;
        public Mysql db;
        public Member loggedInMember;
        public SessionState session;
        protected Random rand;
        Stopwatch timer;
        public int page;
        public UnixTime tz;
        protected Core core;
        public PageSignature Signature;
        private bool isAjax;

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
                pageTitle = "ZinZam &bull; " + HttpUtility.HtmlEncode(value);
            }
        }

        public bool IsAjax
        {
            get
            {
                return isAjax;
            }
        }

        public static string StoragePath;

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
            page = Functions.RequestInt("p", 1);

            StoragePath = Server.MapPath(WebConfigurationManager.AppSettings["storage-path"]);
            db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                WebConfigurationManager.AppSettings["mysql-password"],
                WebConfigurationManager.AppSettings["mysql-database"],
                WebConfigurationManager.AppSettings["mysql-host"]);
            template = new Template(Server.MapPath("./templates/"), "");
            core = new Core(db, template);
            core.page = this;
            Bbcode.Initialise(core);

            session = new SessionState(Core, db, User, HttpContext.Current.Request, HttpContext.Current.Response);
            loggedInMember = session.LoggedInMember;
            tz = new UnixTime(UnixTime.UTC_CODE);
            Display.page = this;

            HttpContext httpContext = HttpContext.Current;
            string[] redir = httpContext.Request.RawUrl.Split(';');

            if (redir.Length > 1)
            {
                core.PagePath = redir[1];
                Uri cUri = new Uri(core.PagePath);
                core.PagePath = cUri.AbsolutePath;
            }
            else
            {
                Core.PagePath = httpContext.Request.RawUrl;
            }

            core.session = session;
            core.CoreDomain = AppDomain.CurrentDomain;

            // Ensure that core applications are loaded
            try
            {
                System.Reflection.Assembly.Load("Profile");
                System.Reflection.Assembly.Load("Groups");
                System.Reflection.Assembly.Load("Networks");
                /*System.Reflection.Assembly.Load("Calendar");
                System.Reflection.Assembly.Load("GuestBook");
                System.Reflection.Assembly.Load("Gallery");
                System.Reflection.Assembly.Load("Blog");
                System.Reflection.Assembly.Load("Pages");*/
            }
            catch
            {
            }

            if (loggedInMember != null)
            {
                tz = loggedInMember.GetTimeZone;
            }

            // move it here
            core.tz = tz;

            isAjax = (HttpContext.Current.Request.QueryString["ajax"] == "true");
        }

        public TPage(string templateFile)
            : this()
        {
            template.SetTemplate(templateFile);
            core.template = template;
        }

        public void EndResponse()
        {
            Display.Header(this);
            HttpContext.Current.Response.Write(template.ToString());
            timer.Stop();
            double seconds = (timer.ElapsedTicks) / 10000000.0;
            //Response.Write(string.Format("<p style=\"background-color: white; color: black;\">{0} seconds &bull; {1} queries</p>", seconds, db.GetQueryCount()));

            if (db != null)
            {
                db.CloseConnection();
            }

            HttpContext.Current.Response.End();
        }

        ~TPage()
        {
            // destructor
            if (db != null)
            {
                db.CloseConnection();
            }
        }
    }
}