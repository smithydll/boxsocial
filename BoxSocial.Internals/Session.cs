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
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Summary description for Session
    /// </summary>
    public class SessionState
    {
        private const int SESSION_EXPIRES = 3600;

        private Member loggedInMember;
        private IPAddress ipAddress;
        private bool isLoggedIn;
        private HttpRequest Request;
        private HttpResponse Response;
        private Mysql db;
        private SessionMethods sessionMethod;
        private SessionCookie sessionData;
        private Core core;

        private string sessionId;

        public string SessionId
        {
            get
            {
                return sessionId;
            }
        }

        public SessionMethods SessionMethod
        {
            get
            {
                return sessionMethod;
            }
        }

        public bool IsLoggedIn
        {
            get
            {
                return isLoggedIn;
            }
        }

        public SessionState(Core core, Mysql db, System.Security.Principal.IPrincipal User, HttpRequest Request, HttpResponse Response)
        {
            this.Request = Request;
            this.Response = Response;
            this.db = db;
            this.core = core;

            isLoggedIn = false;
            ipAddress = IPAddress.Parse(SessionState.ReturnRealIPAddress(Request.ServerVariables));
            SessionPagestart(ipAddress.ToString());
            return;
            
        }

        //
        // The following session algorithm was borrowed from phpBB2.0.22,
        // it is considered secure and widely implemented
        //

        public void SessionBegin(long userId)
        {
            SessionBegin(userId, false, false, false);
        }

        public void SessionBegin(long userId, bool autoCreate)
        {
            SessionBegin(userId, autoCreate, false, false);
        }

        public void SessionBegin(long userId, bool autoCreate, bool enableAutologin)
        {
            SessionBegin(userId, autoCreate, enableAutologin, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="autoCreate"></param>
        /// <param name="enableAutologin"></param>
        /// <param name="admin"></param>
        public void SessionBegin(long userId, bool autoCreate, bool enableAutologin, bool admin)
        {
            string cookieName = "hailToTheChef";
            XmlSerializer xs;
            StringWriter stw;

            sessionData = null;
            sessionId = null;

            if (Request.Cookies[cookieName + "_sid"] != null || Request.Cookies[cookieName + "_data"] != null)
            {
                if (Request.Cookies[cookieName + "_sid"] != null)
                {
                    sessionId = Request.Cookies[cookieName + "_sid"].Value;
                }

                if (Request.Cookies[cookieName + "_data"] != null)
                {
                    xs = new XmlSerializer(typeof(SessionCookie));
                    StringReader sr = new StringReader(HttpUtility.UrlDecode(Request.Cookies[cookieName + "_data"].Value));

                    sessionData = (SessionCookie)xs.Deserialize(sr);
                }
                else
                {
                    sessionData = new SessionCookie();
                }

                sessionMethod = SessionMethods.Cookie;
            }
            else
            {
                sessionData = new SessionCookie();
                if (Request.QueryString["sid"] != null)
                {
                    sessionId = (string)Request.QueryString["sid"];
                }
                sessionMethod = SessionMethods.Get;
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!Regex.IsMatch(sessionId, "^[A-Za-z0-9]*$"))
                {
                    sessionId = "";
                }
            }

	        DateTime lastVisit = new DateTime(1000, 1, 1);

            // 
            // First off attempt to join with the autologin value if we have one
            // If not, just use the user_id value
            //

            loggedInMember = null;

            if (userId != 0)
            {
                //if (isset($sessiondata['autologinid']) && (string) $sessiondata['autologinid'] != '' && $user_id)
                if (sessionData.autoLoginId != null && sessionData.autoLoginId != "" && userId > 0)
                {
                    DataTable userSessionTable = db.SelectQuery(string.Format(
                        @"SELECT {1}
                            FROM user_keys uk
                            INNER JOIN user_info ui ON uk.user_id = ui.user_id
                            INNER JOIN session_keys sk ON sk.user_id = uk.user_id
                            WHERE uk.user_id = {0} AND ui.user_active = 1 AND sk.key_id = '{2}'",
                        userId, Member.USER_INFO_FIELDS, SessionState.SessionMd5(sessionData.autoLoginId)));

                    if (userSessionTable.Rows.Count == 1)
                    {
                        loggedInMember = new Member(db, userSessionTable.Rows[0], false);
                        enableAutologin = isLoggedIn = true;
                    }
                    else
                    {
                        Display.ShowMessage(core, "Error", "Error starting session");
                        Response.Cookies.Clear();
                        //Response.Write("fail 0");
                        //Response.Write(string.Format("SELECT {1} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id INNER JOIN session_keys sk ON sk.user_id = uk.user_id WHERE uk.user_id = {0} AND ui.user_active = 1 AND sk.key_id = '{2}'",
                        //userId, Member.USER_INFO_FIELDS, SessionState.SessionMd5(sessionData.autoLoginId)));
                        if (db != null)
                        {
                            db.CloseConnection();
                        }
                        Response.End();
                    }
                }
                else if (!autoCreate)
                {
                    sessionData.autoLoginId = "";
                    sessionData.userId = userId;

                    DataTable userSessionTable = db.SelectQuery(string.Format("SELECT {1}, {2} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE uk.user_id = {0} AND ui.user_active = 1",
                        userId, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS));

                    if (userSessionTable.Rows.Count == 1)
                    {
                        loggedInMember = new Member(db, userSessionTable.Rows[0], false, true);
                        isLoggedIn = true;
                    }
                    else
                    {
                        // TODO: activation
                        Display.ShowMessage(core, "Inactive account", "You have attempted to use an inactive account. If you have just registered, check for an e-mail with an activation link at the e-mail address you provided.");
                        //Display.ShowMessage(this, "Error", "Error starting session");
                        //Response.Write("fail 1");
                        if (db != null)
                        {
                            db.CloseConnection();
                        }
                        Response.End();
                    }
                }
            }

            //
            // At this point either loggedInMember should be populated or
	        // one of the below is true
	        // * Key didn't match one in the DB
	        // * User does not exist
	        // * User is inactive
	        //
            if (loggedInMember == null)
            {
                if (sessionData == null)
                {
                    sessionData = new SessionCookie();
                }
                sessionData.autoLoginId = "";
                sessionData.userId = userId = 0;
                enableAutologin = isLoggedIn = false;

                DataTable userTable = db.SelectQuery(string.Format("SELECT {1}, {2} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE uk.user_id = {0}",
                    userId, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS));

                if (userTable.Rows.Count == 1)
                {
                    loggedInMember = new Member(db, userTable.Rows[0], false, true);
                }
            }

            // INFO: phpBB2 performs a ban check, we don't have those facilities so let's skip

            //
            // Create or update the session
            //
            long changedRows = db.UpdateQuery(string.Format("UPDATE user_sessions SET session_time_ut = UNIX_TIMESTAMP(), user_id = {0}, session_signed_in = {1} WHERE session_string = '{3}' AND session_ip = '{2}';",
                userId, isLoggedIn, ipAddress.ToString(), sessionId));

            if (changedRows == 0)
            {
                Random rand = new Random();
                sessionId = SessionState.SessionMd5(rand.NextDouble().ToString() + "zzseed").ToLower();

                db.UpdateQuery(string.Format("INSERT INTO user_sessions (session_string, session_time_ut, session_start_ut, session_signed_in, session_ip, user_id) VALUES ('{0}', UNIX_TIMESTAMP(), UNIX_TIMESTAMP(), {1}, '{2}', {3})",
                    sessionId, isLoggedIn, ipAddress.ToString(), userId));
            }

            if (userId != 0)
            {
                TimeSpan ts = DateTime.Now - loggedInMember.LastOnlineTime;

                if (ts.TotalMinutes >= 5)
                {
                    db.UpdateQuery(string.Format("UPDATE user_info SET user_last_visit_ut = UNIX_TIMESTAMP() where user_id = {0}",
                        loggedInMember.UserId));

                    Random rand = new Random();

                    // 1 in 10 chance of deleting stale sessions
                    if (rand.NextDouble() * 10 < 1)
                    {
                        db.UpdateQuery(string.Format("DELETE FROM user_sessions WHERE session_time_ut + {0} < UNIX_TIMESTAMP()",
                            SessionState.SESSION_EXPIRES));
                    }
                }

                if (enableAutologin)
                {
                    Random rand = new Random();
                    string autoLoginKey = SessionState.SessionMd5(rand.NextDouble().ToString() + "zzseed").Substring(4, 16) + SessionState.SessionMd5(rand.NextDouble().ToString() + "zzseed").Substring(4, 16);

                    if (!string.IsNullOrEmpty(sessionData.autoLoginId))
                    {
                        db.UpdateQuery(string.Format("UPDATE session_keys SET key_last_ip = '{0}', key_id = '{1}', key_last_visit_ut = UNIX_TIMESTAMP() WHERE key_id = '{2}'",
                            ipAddress.ToString(), SessionState.SessionMd5(autoLoginKey), SessionState.SessionMd5(sessionData.autoLoginId)));
                    }
                    else
                    {
                        db.UpdateQuery(string.Format("INSERT INTO session_keys (key_id, user_id, key_last_ip, key_last_visit_ut) VALUES ('{0}', {1}, '{2}', UNIX_TIMESTAMP())",
                            SessionState.SessionMd5(autoLoginKey), userId, ipAddress.ToString()));
                    }

                    sessionData.autoLoginId = autoLoginKey;
                    autoLoginKey = "";
                }
                else
                {
                    sessionData.autoLoginId = "";
                }
            }

            ZzUri.Sid = sessionId;

            xs = new XmlSerializer(typeof(SessionCookie));
            stw = new StringWriter();

            HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");
            xs.Serialize(stw, sessionData);
            newSessionDataCookie.Value = stw.ToString();
            newSessionDataCookie.Expires = DateTime.Now.AddYears(1);
            newSessionDataCookie.Secure = false; // TODO: secure cookies
            Response.Cookies.Add(newSessionDataCookie);

            HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
            newSessionSidCookie.Value = sessionId;
            newSessionSidCookie.Expires = DateTime.MinValue;
            newSessionSidCookie.Secure = false; // TODO: secure cookies
            Response.Cookies.Add(newSessionSidCookie);
        }

        public void SessionPagestart(string userIp)
        {
            string cookieName = "hailToTheChef";
            XmlSerializer xs;
            StringWriter stw;

            sessionData = null;
            sessionId = null;

            if (Request.Cookies[cookieName + "_sid"] != null || Request.Cookies[cookieName + "_data"] != null)
            {
                if (Request.Cookies[cookieName + "_sid"] != null)
                {
                    sessionId = Request.Cookies[cookieName + "_sid"].Value;
                }

                if (Request.Cookies[cookieName + "_data"] != null)
                {
                    xs = new XmlSerializer(typeof(SessionCookie));
                    StringReader sr = new StringReader(HttpUtility.UrlDecode(Request.Cookies[cookieName + "_data"].Value));

                    sessionData = (SessionCookie)xs.Deserialize(sr);
                }
                else
                {
                    sessionData = new SessionCookie();
                }

                sessionMethod = SessionMethods.Cookie;
            }
            else
            {
                sessionData = new SessionCookie();
                if (Request.QueryString["sid"] != null)
                {
                    sessionId = (string)Request.QueryString["sid"];
                }
                sessionMethod = SessionMethods.Get;
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!Regex.IsMatch(sessionId, "^[A-Za-z0-9]*$"))
                {
                    sessionId = "";
                }
            }

            DateTime lastVisit = new DateTime(1000, 1, 1);

            if (!string.IsNullOrEmpty(sessionId))
            {
                //
                // session_id exists so go ahead and attempt to grab all
                // data in preparation
                //
                DataTable userSessionTable = db.SelectQuery(string.Format("SELECT {1}, {2}, us.session_string, us.session_ip, us.session_time_ut FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id INNER JOIN user_sessions us ON us.user_id = uk.user_id LEFT JOIN gallery_items gi ON ui.user_icon = gi.gallery_item_id WHERE us.session_string = '{0}';",
                    sessionId, Member.USER_INFO_FIELDS, Member.USER_ICON_FIELDS));

                //
                // Did the session exist in the DB?
                //
                if (userSessionTable.Rows.Count == 1)
                {
                    DataRow userSessionRow = userSessionTable.Rows[0];
                    loggedInMember = new Member(db, userSessionRow, false, true);
                    ZzUri.Sid = sessionId;

                    if (loggedInMember.UserId != 0)
                    {
                        isLoggedIn = true;
                    }

                    //
                    // Do not check IP assuming equivalence, if IPv4 we'll check only first 24
                    // bits ... I've been told (by vHiker) this should alleviate problems with 
                    // load balanced et al proxies while retaining some reliance on IP security.
                    //

                    // we will use complete matches on ZinZam
                    if ((string)userSessionRow["session_ip"] == userIp)
                    {
                        UnixTime tzz = new UnixTime(UnixTime.UTC_CODE); // UTC
                        TimeSpan tss = DateTime.UtcNow - tzz.DateTimeFromMysql(userSessionRow["session_time_ut"]);

                        //
                        // Only update session DB a minute or so after last update
                        //
                        if (tss.TotalMinutes >= 1)
                        {
                            long changedRows = db.UpdateQuery(string.Format("UPDATE user_sessions SET session_time_ut = UNIX_TIMESTAMP() WHERE session_string = '{0}';",
                                sessionId));


                            if (isLoggedIn)
                            {
                                TimeSpan ts = DateTime.Now - loggedInMember.LastOnlineTime;

                                if (ts.TotalMinutes >= 5)
                                {
                                    db.UpdateQuery(string.Format("UPDATE user_info SET user_last_visit_ut = UNIX_TIMESTAMP() where user_id = {0}",
                                        loggedInMember.UserId));

                                    Random rand = new Random();

                                    // 1 in 10 chance of deleting stale sessions
                                    if (rand.NextDouble() * 10 < 1)
                                    {
                                        db.UpdateQuery(string.Format("DELETE FROM user_sessions WHERE session_time_ut + {0} < UNIX_TIMESTAMP()",
                                            SessionState.SESSION_EXPIRES));
                                    }
                                }
                            }

                            SessionClean(sessionId);

                            xs = new XmlSerializer(typeof(SessionCookie));
                            stw = new StringWriter();

                            HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");
                            xs.Serialize(stw, sessionData);
                            newSessionDataCookie.Value = stw.ToString();
                            newSessionDataCookie.Expires = DateTime.Now.AddYears(1);
                            newSessionDataCookie.Secure = false; // TODO: secure cookies
                            Response.Cookies.Add(newSessionDataCookie);

                            HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
                            newSessionSidCookie.Value = sessionId;
                            newSessionSidCookie.Expires = DateTime.MinValue;
                            newSessionSidCookie.Secure = false; // TODO: secure cookies
                            Response.Cookies.Add(newSessionSidCookie);

                        }

                        // Add the session_key to the userdata array if it is set

                        return;
                    }
                }
                else
                {
                    //Display.ShowMessage(this, "Error", "Error starting session");
                    //Response.Write("fail 3");
                    //Response.End();
                }
            }

            //
            // If we reach here then no (valid) session exists. So we'll create a new one,
            // using the cookie user_id if available to pull basic user prefs.
            //

            long userId = (sessionData != null && sessionData.userId > 0) ? sessionData.userId : 0;

            SessionBegin(userId, true);

        }

        public void SessionEnd(string sessionId, long userId)
        {
            string cookieName = "hailToTheChef";
            //XmlSerializer xs;
            //StringWriter stw;

            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!Regex.IsMatch(sessionId, "^[A-Za-z0-9]*$"))
                {
                    return;
                }
            }
            else
            {
                return;
            }

            //
            // Delete existing session
            //
            db.UpdateQuery(string.Format("DELETE FROM user_sessions WHERE session_id = '{0}' AND user_id = {1}",
                sessionId, userId));

            //
            // Remove this auto-login entry (if applicable)
            //

            //
            // We expect that message_die will be called after this function,
            // but just in case it isn't, reset $userdata to the details for a guest
            //

            DataTable userTable = db.SelectQuery(string.Format("SELECT {1} FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id WHERE uk.user_id = {0}",
                    0, Member.USER_INFO_FIELDS));

            if (userTable.Rows.Count == 1)
            {
                loggedInMember = new Member(db, userTable.Rows[0], false);
            }

            HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");
            newSessionDataCookie.Value = "";
            newSessionDataCookie.Expires = DateTime.Now.AddYears(-1);
            newSessionDataCookie.Secure = false; // TODO: secure cookies
            Response.Cookies.Add(newSessionDataCookie);

            HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
            newSessionSidCookie.Value = "";
            newSessionSidCookie.Expires = DateTime.Now.AddYears(-1);
            newSessionSidCookie.Secure = false; // TODO: secure cookies
            Response.Cookies.Add(newSessionSidCookie);

            return;
        }

        private void SessionClean(string sessionId)
        {
            //throw new Exception("The method or operation is not implemented.");
        }

        public IPAddress IPAddress
        {
            get
            {
                return ipAddress;
            }
        }

        public Member LoggedInMember
        {
            get
            {
                return loggedInMember;
            }
        }

        public static string ReturnRealIPAddress(NameValueCollection ServerVariables)
        {
            // List syndicated from http://wikimedia.org/trusted-xff.html
            // TODO: automatically parse the above url with a script into a text file of IP addresses, will be faster
            string[] legitFowardFor = { "61.91.190.242", 
            "61.91.190.246",
            "61.91.190.248",
            "61.91.190.249",
            "61.91.190.250",
            "61.91.190.251",
            "61.91.191.2",
            "61.91.191.4",
            "61.91.191.6",
            "61.91.191.8",
            "61.91.191.9",
            "61.91.191.10",
            "61.91.191.11",
            "203.144.143.2",
            "203.144.143.3",
            "203.144.143.4",
            "203.144.143.5",
            "203.144.143.6",
            "203.144.143.7",
            "203.144.143.8",
            "203.144.143.9",
            "203.144.143.10",
            "203.144.143.11"};
            IPAddress remoteAddress = IPAddress.Parse(ServerVariables["REMOTE_ADDR"]);

            for (int i = 0; i < legitFowardFor.Length; i++)
            {
                if (remoteAddress.Equals(IPAddress.Parse(legitFowardFor[i])))
                {
                    return ServerVariables["HTTP_X_FORWARDED_FOR"];
                }
            }

            return ServerVariables["REMOTE_ADDR"];
        }

        public static string SessionMd5(string input)
        {
            return FormsAuthentication.HashPasswordForStoringInConfigFile(input, "MD5").ToLower();
        }
    }

    [XmlRoot("zinzam-cookie")]
    public class SessionCookie
    {

        [XmlElement("autologinid")]
        public string autoLoginId;

        [XmlElement("userid")]
        public long userId;
    }

    public enum SessionMethods
    {
        /// <summary>
        /// 
        /// </summary>
        Cookie,
        /// <summary>
        /// 
        /// </summary>
        Get
    }
}
