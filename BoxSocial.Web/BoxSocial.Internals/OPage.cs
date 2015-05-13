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
    /// <summary>
    /// OPage is used for OAuth API requests, there is no need to generate a Box Social session for these requests
    /// </summary>
    public class OPage : System.Web.UI.Page
    {
        protected Template template;
        public Mysql db;
        protected Random rand;
        Stopwatch timer;
        public UnixTime tz;
        protected Core core;
        private bool pageEnded;
        HttpContext httpContext;

        public OPage()
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

            ResponseFormats responseFormat = ResponseFormats.Json; // we should default to json but give the option for xml output
            bool isAjax = (HttpContext.Current.Request.QueryString["ajax"] == "true");
            if (!isAjax)
            {
                isAjax = (HttpContext.Current.Request.Form["ajax"] == "true");
                if (isAjax)
                {
                    responseFormat = ResponseFormats.Xml;
                }
            }

            bool isJson = (HttpContext.Current.Request.QueryString["json"] == "true");
            if (!isJson)
            {
                isJson = (HttpContext.Current.Request.Form["json"] == "true");
                if (isJson)
                {
                    responseFormat = ResponseFormats.Json;
                }
            }
            core = new Core(this, responseFormat, db);

            httpContext = HttpContext.Current;
        }

        public void StartSession(OAuthToken token)
        {
            core.Session = new SessionState(core, db, token, httpContext.Request, httpContext.Response);

            if (core.Session.LoggedInMember != null)
            {
                tz = core.Session.LoggedInMember.UserInfo.GetTimeZone;
            }

            // move it here
            core.Tz = tz;
        }

        public void EndResponse()
        {
            if (!pageEnded)
            {
                pageEnded = true;
                long pageEnd = timer.ElapsedTicks;

                long templateStart = timer.ElapsedTicks;
                core.Http.Write(template);
                double templateSeconds = (timer.ElapsedTicks - templateStart) / 10000000.0;

                if (db != null)
                {
                    db.CloseConnection();
                }

                core.Prose.Close();
                core.Search.Dispose();
                //core.Dispose();
                //core = null;

                timer.Stop();
                double seconds = (timer.ElapsedTicks) / 10000000.0;
                double pageEndSeconds = (timer.ElapsedTicks - pageEnd) / 10000000.0;
                if (core != null)
                {
                    if (core.LoggedInMemberId <= 2 && core.LoggedInMemberId != 0)
                    {
                        //HttpContext.Current.Response.Write(string.Format("<!-- {0} seconds (initilised in {4} seconds assemblies loaded in {6}, ended in {5} seconds) - {1} queries in {2} seconds - template in {3} seconds -->", seconds, db.GetQueryCount(), db.GetQueryTime(), templateSeconds, initTime / 10000000.0, pageEndSeconds, loadTime / 10000000.0));
                        // We will write it out as a comment to preserve html validation
                        //HttpContext.Current.Response.Write(string.Format("<!-- {0} -->", db.QueryListToString()));
                    }
                }

                core.Http.End();
                //System.Threading.Thread.CurrentThread.Abort();
            }
        }

        ~OPage()
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
    }
}
