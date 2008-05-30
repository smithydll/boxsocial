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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;

namespace BoxSocial.FrontEnd
{
    public partial class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            AppDomainSetup ads = AppDomain.CurrentDomain.SetupInformation;
            ads.ShadowCopyFiles = bool.TrueString;
            if (ads.ShadowCopyDirectories != null)
            {
                if (ads.ShadowCopyDirectories == "")
                {
                    ads.ShadowCopyDirectories = HttpContext.Current.Server.MapPath("/applications/");
                }
                else
                {
                    ads.ShadowCopyDirectories = string.Format("{0};{1}", ads.ShadowCopyDirectories, HttpContext.Current.Server.MapPath("/applications/"));
                }
            }

            AppDomain.CurrentDomain.SetShadowCopyPath(ads.ShadowCopyDirectories + ";" + Server.MapPath(@"/applications/"));
        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Server.Transfer("error-handler.aspx");
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

            HttpContext httpContext = HttpContext.Current;
            string[] redir = httpContext.Request.RawUrl.Split(';');

            if (!httpContext.Request.RawUrl.Contains("404.aspx"))
            {
                return;
            }
            /*for (int i = 0; i < httpContext.Request.Headers.Count; i++)
            {
                HttpContext.Current.Response.Write(httpContext.Request.Headers[i] + "<br />");
            }*/
            /*HttpContext.Current.Response.Write(httpContext.Request.RawUrl + "<br />");
            HttpContext.Current.Response.End();/**/
#if Local
            if (redir[0] == "/styles/zinzam.css") return;
            if (redir.Length > 1 || !File.Exists(Path.Combine(Server.MapPath("."), redir[0].Replace("/", Path.DirectorySeparatorChar.ToString()))))
            {
                string currentURI;
                Uri cUri;
                if (redir.Length == 1)
                {
                    currentURI = redir[0];
                    cUri = new Uri("http://localhost" + currentURI);
                    currentURI = cUri.AbsolutePath;
                }
                else
                {
                    currentURI = redir[1];
                    cUri = new Uri(currentURI);
                    currentURI = cUri.AbsolutePath;
                }
#else
            if (redir.Length > 1)
            {
                string currentURI = redir[1];
                Uri cUri = new Uri(currentURI);
                currentURI = cUri.AbsolutePath;
#endif
                /*HttpContext.Current.Response.Write(currentURI + "<br />");
                HttpContext.Current.Response.End();*/

                List<string[]> patterns = new List<string[]>();
                patterns.Add(new string[] { @"^/images/corners-(top|bottom|middle)-([0-9a-f\-_]{6})-([0-9\-_]+)-([0-9\-_]+).png$", @"/corners.aspx?location=$1&width=$3&roundness=$4&colour=$2&ext=png" });
                patterns.Add(new string[] { @"^/images/corners-(top|bottom|middle)-([0-9a-f\-_]{6})-([0-9\-_]+)-([0-9\-_]+).gif$", @"/corners.aspx?location=$1&width=$3&roundness=$4&colour=$2&ext=gif" });

                patterns.Add(new string[] { @"^/about(|/)$", @"/about.aspx" });
                patterns.Add(new string[] { @"^/opensource(|/)$", @"/opensource.aspx" });
                patterns.Add(new string[] { @"^/safety(|/)$", @"/safety.aspx" });
                patterns.Add(new string[] { @"^/privacy(|/)$", @"/privacy.aspx" });
                patterns.Add(new string[] { @"^/terms-of-service(|/)$", @"/tos.aspx" });
                patterns.Add(new string[] { @"^/site-map(|/)$", @"/sitemap.aspx" });
                patterns.Add(new string[] { @"^/copyright(|/)$", @"/copyright.aspx" });
                patterns.Add(new string[] { @"^/register(|/)$", @"/register.aspx" });
                patterns.Add(new string[] { @"^/sign-in(|/)$", @"/login.aspx" });
                patterns.Add(new string[] { @"^/login(|/)$", @"/login.aspx" });
                patterns.Add(new string[] { @"^/search(|/)$", @"/search.aspx" });
                patterns.Add(new string[] { @"^/comment(|/)$", @"/comment.aspx" });

                patterns.Add(new string[] { @"^/account/([a-z\-]+)/([a-z\-]+)(|/)$", @"/account.aspx?module=$1&sub=$2" });
                patterns.Add(new string[] { @"^/account/([a-z\-]+)(|/)$", @"/account.aspx?module=$1" });
                patterns.Add(new string[] { @"^/account(|/)$", @"/account.aspx" });

                patterns.Add(new string[] { @"^/styles/([A-Za-z0-9\-_]+).css$", @"/userstyle.aspx?un=$1" });

                patterns.Add(new string[] { @"^/help(|/)$", @"/help.aspx" });
                patterns.Add(new string[] { @"^/help/([a-z\-]+)(|/)$", @"/help.aspx?topic=$1" });

                patterns.Add(new string[] { @"^/applications(|/)$", @"/viewapplications.aspx$1" });

                patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)(|/)$", @"/applicationpage.aspx?an=$1&path=" });

                patterns.Add(new string[] { @"^/groups/create(|/)$", @"/creategroup.aspx" });
                patterns.Add(new string[] { @"^/groups(|/)$", @"/viewgroups.aspx$1" });
                patterns.Add(new string[] { @"^/groups/([A-Za-z0-9\-_]+)(|/)$", @"/viewgroups.aspx?category=$1" });

                patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)(|/)$", @"/grouppage.aspx?gn=$1&path=" });

                //patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/images/([A-Za-z0-9\-_/\.]+)$", @"/viewimage.aspx?gn=$1&path=$2" });

                patterns.Add(new string[] { @"^/networks(|/)$", @"/viewnetworks.aspx" });
                patterns.Add(new string[] { @"^/networks/([A-Za-z0-9\-_\.]+)(|/)$", @"/viewnetworks.aspx?type=$1" });

                patterns.Add(new string[] { @"^/network/([A-Za-z0-9\-_\.]+)(|/)$", @"/networkpage.aspx?nn=$1&path=" });

                //patterns.Add(new string[] { @"^/network/([A-Za-z0-9\-_\.]+)/images/([A-Za-z0-9\-_/\.]+)$", @"/viewimage.aspx?nn=$1&path=$2" });

                patterns.Add(new string[] { @"^/([A-Za-z0-9\-_\.]+)(|/)$", @"/memberpage.aspx?un=$1&path=" });

                //patterns.Add(new string[] { @"^/([A-Za-z0-9\-_]+)/profile(|/)$", @"/viewprofile.aspx?un=$1" });

                patterns.Add(new string[] { @"^/([A-Za-z0-9\-_\.]+)/friends(|/)$", @"/viewfriends.aspx?un=$1" });
                patterns.Add(new string[] { @"^/([A-Za-z0-9\-_\.]+)/friends/([0-9]+)(|/)$", @"/viewfriends.aspx?un=$1&page=$2" });

                //patterns.Add(new string[] { @"^/([A-Za-z0-9\-_]+)/images/([A-Za-z0-9\-_/\.]+)$", @"/viewimage.aspx?un=$1&path=$2" });

                /* Wildcard for application loader */
                patterns.Add(new string[] { @"^/application/([A-Za-z0-9\-_]+)/(.+)(|/)$", @"/applicationpage.aspx?an=$1&path=$2" });
                patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/(.+)(|/)$", @"/grouppage.aspx?gn=$1&path=$2" });
                patterns.Add(new string[] { @"^/network/([A-Za-z0-9\-_\.]+)/(.+)(|/)$", @"/networkpage.aspx?nn=$1&path=$2" });
                patterns.Add(new string[] { @"^/([A-Za-z0-9\-_\.]+)/(.+)(|/)$", @"/memberpage.aspx?un=$1&path=$2" });

                foreach (string[] pattern in patterns)
                {
                    if (Regex.IsMatch(currentURI, pattern[0]))
                    {
                        currentURI = Regex.Replace(currentURI, pattern[0], pattern[1]);
                        if (currentURI.Contains("?"))
                        {
                            httpContext.RewritePath(currentURI + "&" + cUri.Query.TrimStart(new char[] { '?' }));
                            return;
                        }
                        else
                        {
                            httpContext.RewritePath(currentURI + cUri.Query);
                            return;
                        }
                    }
                }
            }
        }
    }
}