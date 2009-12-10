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
using BoxSocial.IO;

namespace BoxSocial.Documentation.FrontEnd
{
    public partial class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
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

            if (host == "www." + Domain)
            {
                Response.Redirect(Uri);
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

                    if (currentURI.EndsWith("index.php"))
                    {
                        currentURI = currentURI.Substring(0, currentURI.Length - 9);
                        Response.Redirect(currentURI, true);
                        Response.End();
                        return;
                    }

                    if (currentURI.EndsWith(".php"))
                    {
                        currentURI = currentURI.Substring(0, currentURI.Length - 4);
                        Response.Redirect(currentURI, true);
                        Response.End();
                        return;
                    }
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

            if (!httpContext.Request.RawUrl.Contains("404.aspx"))
            {
                if (host == Domain)
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
                patterns.Add(new string[] { @"^/about(/|)$", @"/about.aspx" });


                // full catch all
                foreach (string[] pattern in patterns)
                {
                    if (Regex.IsMatch(currentURI, pattern[0]))
                    {
                        Regex rex = new Regex(pattern[0]);
                        currentURI = rex.Replace(currentURI, pattern[1]);
                        if (currentURI.Contains("?"))
                        {
                            httpContext.RewritePath(currentURI.TrimEnd(new char[] { '/' }) + "&" + cUri.Query.TrimStart(new char[] { '?' }));
                            return;
                        }
                        else
                        {
                            httpContext.RewritePath(currentURI.TrimEnd(new char[] { '/' }) + cUri.Query);
                            return;
                        }
                    }
                }
            }
        }

        public static string Domain
        {
            get
            {
                if (WebConfigurationManager.AppSettings != null && WebConfigurationManager.AppSettings.HasKeys())
                {
                    return WebConfigurationManager.AppSettings["boxsocial-host"].ToLower();
                }
                else
                {
                    return "zinzam.com";
                }
            }
        }

        public static string Uri
        {
            get
            {
                return string.Format("http://{0}/", Domain);
            }
        }
    }
}
