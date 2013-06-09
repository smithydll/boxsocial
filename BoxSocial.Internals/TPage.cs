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
using System.Collections.Generic;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private string pageTitle;
        private string canonicalUri;

        public Template template;
        public Mysql db;
        public User loggedInMember;
        public SessionState session;
        protected Random rand;
        Stopwatch timer;
        public UnixTime tz;
        protected Core core;
        public PageSignature Signature;
        private bool isAjax;
        private bool isMobile;
        private bool pageEnded;

        //
        // Pagination
        //
        protected int[] page;
        protected long[] offset;

        public int[] PageNumber
        {
            get
            {
                return page;
            }
        }

        public long[] PageOffset
        {
            get
            {
                return offset;
            }
        }

        public int TopLevelPageNumber
        {
            get
            {
                if (page.Length >= 1)
                {
                    return page[0];
                }
                else
                {
                    return 1;
                }
            }
        }

        public long TopLevelPageOffset
        {
            get
            {
                if (offset.Length >= 1)
                {
                    return offset[0];
                }
                else
                {
                    return 0;
                }
            }
        }

        public int CommentPageNumber
        {
            get
            {
                if (page.Length >= 1)
                {
                    return page[page.Length - 1];
                }
                else
                {
                    return 1;
                }
            }
        }

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
                pageTitle = HttpUtility.HtmlEncode(value) + " • " + Core.Settings.SiteTitle;
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
                if (core != null && core.Hyperlink != null)
                {
                    canonicalUri = core.Hyperlink.StripSid(value);
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

        public bool IsMobile
        {
            get
            {
                return isMobile;
            }
        }

        public TPage()
        {
            timer = new Stopwatch();
            timer.Start();
            rand = new Random();

            db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                WebConfigurationManager.AppSettings["mysql-password"],
                WebConfigurationManager.AppSettings["mysql-database"],
                WebConfigurationManager.AppSettings["mysql-host"]);
            template = new Template(Server.MapPath("./templates/"), "");

            string u = HttpContext.Current.Request.ServerVariables["HTTP_USER_AGENT"];
            Regex b = new Regex(@"android.+mobile|avantgo|bada\\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\\/|plucker|pocket|psp|symbian|treo|up\\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\\-(n|u)|c55\\/|capi|ccwa|cdm\\-|cell|chtm|cldc|cmd\\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\\-s|devi|dica|dmob|do(c|p)o|ds(12|\\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\\-|_)|g1 u|g560|gene|gf\\-5|g\\-mo|go(\\.w|od)|gr(ad|un)|haie|hcit|hd\\-(m|p|t)|hei\\-|hi(pt|ta)|hp( i|ip)|hs\\-c|ht(c(\\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\\-(20|go|ma)|i230|iac( |\\-|\\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\\/)|klon|kpt |kwc\\-|kyo(c|k)|le(no|xi)|lg( g|\\/(k|l|u)|50|54|e\\-|e\\/|\\-[a-w])|libw|lynx|m1\\-w|m3ga|m50\\/|ma(te|ui|xo)|mc(01|21|ca)|m\\-cr|me(di|rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\\-2|po(ck|rt|se)|prox|psio|pt\\-g|qa\\-a|qc(07|12|21|32|60|\\-[2-7]|i\\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\\-|oo|p\\-)|sdk\\/|se(c(\\-|0|1)|47|mc|nd|ri)|sgh\\-|shar|sie(\\-|m)|sk\\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\\-|v\\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\\-|tdg\\-|tel(i|m)|tim\\-|t\\-mo|to(pl|sh)|ts(70|m\\-|m3|m5)|tx\\-9|up(\\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|xda(\\-|2|g)|yas\\-|your|zeto|zte\\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            if ((b.IsMatch(u) || v.IsMatch(u.Substring(0, 4))))
            {
                isMobile = true;
                template.Medium = DisplayMedium.Mobile;
                template.Parse("IS_MOBILE", "TRUE");
            }
            else
            {
                isMobile = false;
            }

            core = new Core(db, template);

            core.Settings = new Settings(core);
            core.Search = new Search(core);

            pageTitle = core.Settings.SiteTitle + (!string.IsNullOrEmpty(core.Settings.SiteSlogan) ? " • " + core.Settings.SiteSlogan : string.Empty);

            if (core.Settings.StorageProvider == "amazon_s3")
            {
                core.Storage = new AmazonS3(WebConfigurationManager.AppSettings["amazon-key-id"], WebConfigurationManager.AppSettings["amazon-secret-key"], db);
            }
            else if (core.Settings.StorageProvider == "rackspace")
            {
                core.Storage = new Rackspace(WebConfigurationManager.AppSettings["rackspace-key"], WebConfigurationManager.AppSettings["rackspace-username"], db);
            }
            else if (core.Settings.StorageProvider == "azure")
            {
                // provision: not supported
            }
            else if (core.Settings.StorageProvider == "local")
            {
                core.Storage = new LocalStorage(core.Settings.StorageRootUserFiles, db);
            }
            else
            {
                core.Storage = new LocalStorage(Server.MapPath(WebConfigurationManager.AppSettings["storage-path"]), db);
            }

            core.page = this;
            core.Bbcode = new Bbcode(core);
            core.Functions = new Functions(core);
            core.Display = new Display(core);
            core.Email = new Email(core);
            core.Ajax = new Ajax(core);
            core.Hyperlink = new Hyperlink(core);

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
                if (httpContext.Request.Url.Host.ToLower() == Hyperlink.Domain)
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
                tz = loggedInMember.UserInfo.GetTimeZone;
            }

            // move it here
            core.Tz = tz;

            isAjax = (HttpContext.Current.Request.QueryString["ajax"] == "true");
            if (!isAjax)
            {
                isAjax = (HttpContext.Current.Request.Form["ajax"] == "true");
            }

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

            string pageString = core.Http.Query["p"];
            if (!string.IsNullOrEmpty(pageString))
            {
                string[] pages = pageString.Split(new char[] { ',' });
                page = new int[pages.Length];

                for (int i = 0; i < pages.Length; i++)
                {
                    if (!int.TryParse(pages[i], out page[i]))
                    {
                        page[i] = 1;
                    }
                }
            }
            else
            {
                page = new int[] { 1 };
            }

            string offsetString = core.Http.Query["o"];
            if (!string.IsNullOrEmpty(offsetString))
            {
                string[] offsets = offsetString.Split(new char[] { ',' });
                offset = new long[offsets.Length];

                for (int i = 0; i < offsets.Length; i++)
                {
                    if (!long.TryParse(offsets[i], out offset[i]))
                    {
                        offset[i] = 0;
                    }
                }
            }
            else
            {
                offset = new long[] { 0 };
            }
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
                    HttpContext.Current.Response.Write(string.Format("<!-- {0} seconds - {1} queries in {2} seconds - template in {3} seconds -->", seconds, db.GetQueryCount(), db.GetQueryTime(), templateSeconds));
                    if (core.LoggedInMemberId <= 3 && core.LoggedInMemberId != 0)
                    {
                        // We will write it out as a comment to preserve html validation
                        HttpContext.Current.Response.Write(string.Format("<!-- {0} -->", db.QueryListToString()));
                    }
                }

                if (db != null)
                {
                    db.CloseConnection();
                }

                core.Prose.Close();
                core.Search.Dispose();
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
            core.Search.Dispose();
            //core.Dispose();
            //core = null;
        }

        public void ParseCoreBreadCrumbs(List<string[]> parts)
        {
            ParseCoreBreadCrumbs("BREADCRUMBS", parts);
        }

        public void ParseCoreBreadCrumbs(string templateVar, List<string[]> parts)
        {
            ParseCoreBreadCrumbs(core.Template, templateVar, parts);
        }

        public void ParseCoreBreadCrumbs(Template template, string templateVar, List<string[]> parts)
        {
            string output = string.Empty;
            string path = "/";
            output = string.Format("<a href=\"{1}\">{0}</a>",
                    core.Settings.SiteTitle, path);

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], path + parts[i][0].TrimStart(new char[] { '*' }));
                    if (!parts[i][0].StartsWith("*"))
                    {
                        path += parts[i][0] + "/";
                    }
                }
            }

            template.ParseRaw(templateVar, output);
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
