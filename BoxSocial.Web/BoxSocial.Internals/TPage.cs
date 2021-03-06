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
using BoxSocial.Forms;

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

        protected Template template;
        public Mysql db;
        public User loggedInMember;
        public SessionState session;
        protected Random rand;
        Stopwatch timer;
        public UnixTime tz;
        protected Core core;
        public PageSignature Signature;
        private bool isAjax;
        private bool isJson;
        private bool isMobile;
        private bool pageEnded;

        public Template Template
        {
            get
            {
                return core.Template;
            }
        }

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

        public bool IsJson
        {
            get
            {
                return IsJson;
            }
        }

        public bool IsMobile
        {
            get
            {
                return isMobile;
            }
        }

        public DisplayMedium Medium
        {
            get
            {
                if (isMobile)
                {
                    return DisplayMedium.Mobile;
                }
                return DisplayMedium.Desktop;
            }
        }

        private static Regex b = new Regex(@"android.+mobile|avantgo|bada\\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\\/|plucker|pocket|psp|symbian|treo|up\\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\\-(n|u)|c55\\/|capi|ccwa|cdm\\-|cell|chtm|cldc|cmd\\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\\-s|devi|dica|dmob|do(c|p)o|ds(12|\\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\\-|_)|g1 u|g560|gene|gf\\-5|g\\-mo|go(\\.w|od)|gr(ad|un)|haie|hcit|hd\\-(m|p|t)|hei\\-|hi(pt|ta)|hp( i|ip)|hs\\-c|ht(c(\\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\\-(20|go|ma)|i230|iac( |\\-|\\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\\/)|klon|kpt |kwc\\-|kyo(c|k)|le(no|xi)|lg( g|\\/(k|l|u)|50|54|e\\-|e\\/|\\-[a-w])|libw|lynx|m1\\-w|m3ga|m50\\/|ma(te|ui|xo)|mc(01|21|ca)|m\\-cr|me(di|rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\\-2|po(ck|rt|se)|prox|psio|pt\\-g|qa\\-a|qc(07|12|21|32|60|\\-[2-7]|i\\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\\-|oo|p\\-)|sdk\\/|se(c(\\-|0|1)|47|mc|nd|ri)|sgh\\-|shar|sie(\\-|m)|sk\\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\\-|v\\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\\-|tdg\\-|tel(i|m)|tim\\-|t\\-mo|to(pl|sh)|ts(70|m\\-|m3|m5)|tx\\-9|up(\\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|xda(\\-|2|g)|yas\\-|your|zeto|zte\\-", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        private long initTime = 0;
        private long loadTime = 0;
        public TPage()
        {
            timer = new Stopwatch();
            timer.Start();
            rand = new Random();

            Stopwatch initTimer = new Stopwatch();
            initTimer.Start();

            db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                WebConfigurationManager.AppSettings["mysql-password"],
                WebConfigurationManager.AppSettings["mysql-database"],
                WebConfigurationManager.AppSettings["mysql-host"]);
            template = new Template(Server.MapPath("./templates/"), "");

            HttpContext.Current.Response.AppendHeader("x-ua-compatible", "IE=edge,chrome=1");

            string u = HttpContext.Current.Request.ServerVariables["HTTP_USER_AGENT"];
            if ((!string.IsNullOrEmpty(u)) && ((b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)))))
            {
                isMobile = true;
                template.Medium = DisplayMedium.Mobile;
                template.Parse("IS_MOBILE", "TRUE");
            }
            else
            {
                isMobile = false;
            }

            ResponseFormats responseFormat = ResponseFormats.Html;
            isAjax = (HttpContext.Current.Request.QueryString["ajax"] == "true");
            if (!isAjax)
            {
                isAjax = (HttpContext.Current.Request.Form != null && HttpContext.Current.Request.Form["ajax"] == "true");
            }
            if (isAjax)
            {
                responseFormat = ResponseFormats.Xml;
            }

            isJson = (HttpContext.Current.Request.QueryString["json"] == "true");
            if (!isJson)
            {
                isJson = (HttpContext.Current.Request.Form != null && HttpContext.Current.Request.Form["json"] == "true");
            }
            if (isJson)
            {
                responseFormat = ResponseFormats.Json;
            }

            core = new Core(this, responseFormat, db, template);

            pageTitle = core.Settings.SiteTitle + (!string.IsNullOrEmpty(core.Settings.SiteSlogan) ? " • " + core.Settings.SiteSlogan : string.Empty);

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

#if DEBUG
            Stopwatch httpTimer = new Stopwatch();
            httpTimer.Start();
#endif
            session = new SessionState(Core, db, User, HttpContext.Current.Request, HttpContext.Current.Response);
            loggedInMember = session.LoggedInMember;
#if DEBUG
            httpTimer.Stop();
            HttpContext.Current.Response.Write(string.Format("<!-- section A in {0} -->\r\n", httpTimer.ElapsedTicks / 10000000.0));
#endif

            long loadStart = initTimer.ElapsedTicks;

            tz = new UnixTime(core, UnixTime.UTC_CODE);

            core.Session = session;
            core.CoreDomain = AppDomain.CurrentDomain;

            if (loggedInMember != null)
            {
                tz = loggedInMember.UserInfo.GetTimeZone;
            }

            // move it here
            core.Tz = tz;

            // As a security measure we use the http object to prevent
            // applications hijacking the response output
            core.Http = new Http();
            Template.Path = core.Http.TemplatePath;

            core.Prose = new Prose();
            core.Prose.Initialise(core, "en");

            //List<string> asmNames = core.GetLoadedAssemblyNames();
            foreach (string asm in BoxSocial.Internals.Application.AssemblyNames.Keys)
            {
                core.Prose.AddApplication(asm);
            }

            //List<Assembly> asms = core.GetLoadedAssemblies();
            foreach (Assembly asm in BoxSocial.Internals.Application.LoadedAssemblies.Values)
            {
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

           if (session != null && session.SignedIn && core.IsMobile && core.ResponseFormat == ResponseFormats.Html)
           {
                List<ApplicationEntry> applications = BoxSocial.Internals.Application.GetApplications(core, core.Session.LoggedInMember);

                foreach (ApplicationEntry ae in applications)
                {
                    BoxSocial.Internals.Application.LoadApplication(core, AppPrimitives.Member, ae);
                }

                core.InvokePostHooks(new HookEventArgs(core, AppPrimitives.Member, core.Session.LoggedInMember));
            }

           // move this here so it can be overwritten
            template.Parse("U_REGISTER", Core.Hyperlink.BuildRegisterUri());

            loadTime = (initTimer.ElapsedTicks - loadStart);
            initTimer.Stop();
            initTime += initTimer.ElapsedTicks;
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
                long pageEnd = timer.ElapsedTicks;

                core.Display.Header(this);
                long templateStart = timer.ElapsedTicks;
                core.Http.Write(template);
                double templateSeconds = (timer.ElapsedTicks - templateStart) / 10000000.0;

                if (db != null)
                {
                    db.CloseConnection();
                }

                core.CloseProse();
                core.CloseSearch();
                //core.Dispose();
                //core = null;

                timer.Stop();
                double seconds = (timer.ElapsedTicks) / 10000000.0;
                double pageEndSeconds = (timer.ElapsedTicks - pageEnd) / 10000000.0;
                if (core != null)
                {
                    //if (core.LoggedInMemberId <= 2 && core.LoggedInMemberId != 0)
                    {
                        HttpContext.Current.Response.Write(string.Format("\r\n<!-- {0} seconds (initilised in {4} seconds assemblies loaded in {6}, ended in {5} seconds) - {1} queries in {2} seconds - template in {3} seconds -->\r\n", seconds, db.GetQueryCount(), db.GetQueryTime(), templateSeconds, initTime / 10000000.0, pageEndSeconds, loadTime / 10000000.0));
#if DEBUG
                        // We will write it out as a comment to preserve html validation
                        HttpContext.Current.Response.Write(string.Format("<!-- {0} seconds, {1} parsed (BBcode) -->\r\n", core.Bbcode.GetBbcodeTime(), core.Bbcode.GetParseCount()));
                        HttpContext.Current.Response.Write(string.Format("<!-- {0} -->", db.QueryListToString()));
#endif
                    }
                }

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

            core.CloseProse();
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
                    core.Settings.SiteTitle, core.Hyperlink.AppendSid(path));

            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i][0] != "")
                {
                    output += string.Format(" <strong>&#8249;</strong> <a href=\"{1}\">{0}</a>",
                        parts[i][1], core.Hyperlink.AppendSid(path + parts[i][0].TrimStart(new char[] { '*' })));
                    if (!parts[i][0].StartsWith("*", StringComparison.Ordinal))
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
