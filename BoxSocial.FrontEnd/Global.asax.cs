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
using System.Web.Configuration;
using System.Web.SessionState;
using BoxSocial.Internals;
using BoxSocial.IO;

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
            string host = httpContext.Request.Url.Host.ToLower();

            if (host == "www." + Linker.Domain)
            {
                Response.Redirect(Linker.Uri);
                return;
            }

            string currentURI = null;
            Uri cUri = null;
            if (httpContext.Request.RawUrl.Contains(";http://") || httpContext.Request.RawUrl.Contains("?http://"))
            {
                if (redir.Length > 1)
                // Apache2/IIS
                {
                    currentURI = redir[1];
                    cUri = new Uri(currentURI);
                    currentURI = cUri.AbsolutePath;
                }
                else
                // NGINX
                {
                    int i = httpContext.Request.RawUrl.IndexOf('?');
                    if (httpContext.Request.RawUrl.Length >= i)
                    {
                        currentURI = httpContext.Request.RawUrl.Substring(i + 1);
                    }
                    cUri = new Uri(currentURI);
                    currentURI = cUri.AbsolutePath;
                }
            }
            /*else
            {
                currentURI = httpContext.Request.RawUrl;
                cUri = new Uri(currentURI);
                currentURI = cUri.AbsolutePath;
            }*/

            if (!httpContext.Request.RawUrl.Contains("404.aspx"))
            {
                if (host == Linker.Domain)
                {
                    return;
                }
                else
                {
                    if (httpContext.Request.RawUrl.Contains("default.aspx"))
                    {
                        cUri = httpContext.Request.Url;
                        currentURI = "/";
                    }
                }
            }

            if (currentURI != null)
            {
                List<string[]> patterns = new List<string[]>();
                if (host != Linker.Domain)
                {
                    SelectQuery query = new SelectQuery("dns_records");
                    query.AddFields("dns_domain", "dns_owner_id", "dns_owner_type", "dns_owner_key");
                    query.AddCondition("dns_domain", host);

                    Mysql db = new Mysql(WebConfigurationManager.AppSettings["mysql-user"],
                        WebConfigurationManager.AppSettings["mysql-password"],
                        WebConfigurationManager.AppSettings["mysql-database"],
                        WebConfigurationManager.AppSettings["mysql-host"]);

                    DataTable dnsTable = db.Query(query);

                    if (dnsTable.Rows.Count == 1)
                    {
                        switch ((string)dnsTable.Rows[0]["dns_owner_type"])
                        {
                            case "GROUP":
                                patterns.Add(new string[] { @"^/comment(|/)$", @"/comment.aspx" });

                                patterns.Add(new string[] { string.Format(@"^/styles/group/{0}.css$", (string)dnsTable.Rows[0]["dns_owner_key"]), string.Format(@"/groupstyle.aspx?gn={0}", (string)dnsTable.Rows[0]["dns_owner_key"]) });

                                patterns.Add(new string[] { @"^/account/([a-z\-]+)/([a-z\-]+)(|/)$", string.Format(@"/groupaccount.aspx?gn={0}&module=$1&sub=$2", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                patterns.Add(new string[] { @"^/account/([a-z\-]+)(|/)$", string.Format(@"/groupaccount.aspx?gn={0}&module=$1", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                patterns.Add(new string[] { @"^/account(|/)$", string.Format(@"/groupaccount.aspx?gn={0}", (string)dnsTable.Rows[0]["dns_owner_key"]) });

                                patterns.Add(new string[] { @"^(|/)$", string.Format(@"/grouppage.aspx?gn={0}&path=", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                patterns.Add(new string[] { @"^/(.+)(|/)$", string.Format(@"/grouppage.aspx?gn={0}&path=$1", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                break;
                            case "USER":
                                patterns.Add(new string[] { @"^/comment(|/)$", @"/comment.aspx" });

                                patterns.Add(new string[] { string.Format(@"^/styles/{0}.css$", (string)dnsTable.Rows[0]["dns_owner_key"]), string.Format(@"/userstyle.aspx?un={0}", (string)dnsTable.Rows[0]["dns_owner_key"]) });

                                patterns.Add(new string[] { @"^/friends(|/)$", string.Format(@"/viewfriends.aspx?un={0}", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                patterns.Add(new string[] { @"^/friends/([0-9]+)(|/)$", string.Format(@"/viewfriends.aspx?un={0}&page=$1", (string)dnsTable.Rows[0]["dns_owner_key"]) });

                                patterns.Add(new string[] { @"^(|/)$", string.Format(@"/memberpage.aspx?un={0}&path=", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                patterns.Add(new string[] { @"^/(.+)(|/)$", string.Format(@"/memberpage.aspx?un={0}&path=$1", (string)dnsTable.Rows[0]["dns_owner_key"]) });
                                break;
                        }
                    }
                    else
                    {
                        return;
                    }

                    db.CloseConnection();
                }
                else
                {
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

                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/account/([a-z\-]+)/([a-z\-]+)(|/)$", @"/groupaccount.aspx?gn=$1&module=$2&sub=$3" });
                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/account/([a-z\-]+)(|/)$", @"/groupaccount.aspx?gn=$1&module=$2" });
                    patterns.Add(new string[] { @"^/group/([A-Za-z0-9\-_]+)/account(|/)$", @"/groupaccount.aspx?gn=$1" });

                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/account/([a-z\-]+)/([a-z\-]+)(|/)$", @"/musicaccount.aspx?mn=$1&module=$2&sub=$3" });
                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/account/([a-z\-]+)(|/)$", @"/musicaccount.aspx?mn=$1&module=$2" });
                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)/account(|/)$", @"/musicaccount.aspx?mn=$1" });

                    patterns.Add(new string[] { @"^/styles/([A-Za-z0-9\-_]+).css$", @"/userstyle.aspx?un=$1" });
                    patterns.Add(new string[] { @"^/styles/group/([A-Za-z0-9\-_]+).css$", @"/groupstyle.aspx?gn=$1" });
                    patterns.Add(new string[] { @"^/styles/music/([A-Za-z0-9\-_]+).css$", @"/musicstyle.aspx?gn=$1" });

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

                    patterns.Add(new string[] { @"^/musicians/create(|/)$", @"/createmusician.aspx" });
                    patterns.Add(new string[] { @"^/musicians(|/)$", @"/viewmusicians.aspx$1" });
                    patterns.Add(new string[] { @"^/musicians/([A-Za-z0-9\-_]+)(|/)$", @"/viewmusicians.aspx?genre=$1" });
                    patterns.Add(new string[] { @"^/musicians/([A-Za-z0-9\-_]+)(|/)([A-Za-z0-9\-_]+)(|/)$", @"/viewmusicians.aspx?genre=$1&sub=$2" });

                    patterns.Add(new string[] { @"^/music/([A-Za-z0-9\-_]+)(|/)$", @"/musicpage.aspx?mn=$1&path=" });

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

                }

                // fast cull
                int ioc = currentURI.IndexOf('/', 1);
                if (ioc >= 1)
                {
                    foreach (string[] pattern in patterns)
                    {
                        int iop = pattern[0].IndexOf('/', 2);
                        if (iop >= 2)
                        {
                            if (currentURI.Substring(1, ioc - 1).Equals(pattern[0].Substring(2, iop - 2)))
                            {
                                if (Regex.IsMatch(currentURI, pattern[0]))
                                {
                                    Regex rex = new Regex(pattern[0]);
                                    currentURI = rex.Replace(currentURI, pattern[1]);
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

                // full catch all
                foreach (string[] pattern in patterns)
                {
                    if (Regex.IsMatch(currentURI, pattern[0]))
                    {
                        Regex rex = new Regex(pattern[0]);
                        currentURI = rex.Replace(currentURI, pattern[1]);
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
