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
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_sessions", DataTableTypes.Volatile)]
    public class Session : NumberedItem
    {
        [DataField("session_id", DataFieldKeys.Primary)]
        private long sessionId;
        [DataField("user_id")]
        private long userId;
        [DataField("session_string", DataFieldKeys.Unique, 32)]
        private string sessionString;
        [DataField("session_start_ut")]
        private long sessionStartRaw;
        [DataField("session_time_ut")]
        private long sessionTimeRaw;
        [DataField("session_signed_in")]
        private bool sessionSignedIn;
        [DataField("session_ip", IP)]
        private string sessionIp;

        private User user;

        internal long SessionId
        {
            get
            {
                return sessionId;
            }
        }

        internal long UserId
        {
            get
            {
                return userId;
            }
        }

        internal User User
        {
            get
            {
                if (user == null || userId != user.Id)
                {
                    core.LoadUserProfile(userId);
                    user = core.PrimitiveCache[userId];
                    return user;
                }
                else
                {
                    return user;
                }
            }
        }

        internal string SessionString
        {
            get
            {
                return sessionString;
            }
        }

        internal long StartRaw
        {
            get
            {
                return sessionStartRaw;
            }
        }

        internal long TimeRaw
        {
            get
            {
                return sessionTimeRaw;
            }
        }

        internal bool SignedIn
        {
            get
            {
                return sessionSignedIn;
            }
        }

        internal string Ip
        {
            get
            {
                return sessionIp;
            }
        }

        public DateTime GetStart(UnixTime tz)
        {
            return tz.DateTimeFromMysql(sessionStartRaw);
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(sessionTimeRaw);
        }

        internal Session(Core core, DataRow sessionRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Session_ItemLoad);

            loadItemInfo(sessionRow);
        }

        void Session_ItemLoad()
        {
            
        }

        public override long Id
        {
            get
            {
                return sessionId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    [DataTable("session_keys", DataTableTypes.NonVolatile)]
    internal sealed class SessionKey : Item
    {
        [DataField("key_id", DataFieldKeys.Primary, "ternary", 32)]
        private string keyId;
        [DataField("user_id", DataFieldKeys.Primary, "ternary")]
        private long userId;
        [DataField("key_last_ip", IP)]
        private string lastIp;
        [DataField("key_last_visit_ut")]
        private long lastVisitRaw;

        internal string KeyId
        {
            get
            {
                return keyId;
            }
        }

        internal long UserId
        {
            get
            {
                return userId;
            }
        }

        internal string Ip
        {
            get
            {
                return lastIp;
            }
        }

        internal long VisitRaw
        {
            get
            {
                return lastVisitRaw;
            }
        }

        public DateTime GetVisit(UnixTime tz)
        {
            return tz.DateTimeFromMysql(lastVisitRaw);
        }

        internal SessionKey(Core core, DataRow keyRow)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(SessionKey_ItemLoad);

            loadItemInfo(keyRow);
        }

        void SessionKey_ItemLoad()
        {
            
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Summary description for Session
    /// </summary>
    public class SessionState
    {
        private static readonly int SESSION_EXPIRES = 3600;

        private User loggedInMember;
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
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.Request = Request;
            this.Response = Response;
            this.db = db;
            this.core = core;
            this.isLoggedIn = false;

            ipAddress = IPAddress.Parse(SessionState.ReturnRealIPAddress(Request.ServerVariables));
            SessionPagestart(ipAddress.ToString());
            return;
            
        }
		
		public SessionState(Core core, User user)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

			this.core = core;
			this.db = core.Db;
			isLoggedIn = true;
			loggedInMember = user;
			ipAddress = new IPAddress(0);
		}

        //
        // The following session algorithm was borrowed from phpBB2.0.22,
        // it is considered secure and widely implemented
        //

        public string SessionBegin(long userId)
        {
            return SessionBegin(userId, false, false, false);
        }

        public string SessionBegin(long userId, bool autoCreate)
        {
            return SessionBegin(userId, autoCreate, false, false);
        }

        public string SessionBegin(long userId, bool autoCreate, bool enableAutologin)
        {
            return SessionBegin(userId, autoCreate, enableAutologin, false);
        }

        public string SessionBegin(long userId, bool autoCreate, bool enableAutologin, bool admin)
        {
            return SessionBegin(userId, autoCreate, enableAutologin, admin, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="autoCreate"></param>
        /// <param name="enableAutologin"></param>
        /// <param name="admin"></param>
        public string SessionBegin(long userId, bool autoCreate, bool enableAutologin, bool admin, DnsRecord record)
        {
            string cookieName = "hailToTheChef";
            XmlSerializer xs;
            StringWriter stw;

            sessionData = null;
            sessionId = null;

            if (record == null)
            {
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

                        try
                        {
                            sessionData = (SessionCookie)xs.Deserialize(sr);
                        }
                        catch
                        {
                            sessionData = new SessionCookie();
                        }
                    }
                    else
                    {
                        sessionData = new SessionCookie();
                    }

                    if (string.IsNullOrEmpty(sessionId))
                    {
                        sessionId = (string)Request.QueryString["sid"];
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
            }
            else
            {
                sessionData = new SessionCookie();
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!Regex.IsMatch(sessionId, "^[A-Za-z0-9]*$"))
                {
                    sessionId = "";
                }
            }

            if (record != null)
            {
                sessionMethod = SessionMethods.Get;
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
                if (!string.IsNullOrEmpty(sessionData.autoLoginId) && userId > 0)
                {
                    SelectQuery query = User.GetSelectQueryStub(UserLoadOptions.Info);
                    query.AddJoin(JoinTypes.Inner, "session_keys", "user_id", "user_id");
                    query.AddCondition("user_keys.user_id", userId);
                    query.AddCondition("user_active", true);
                    query.AddCondition("key_id", SessionState.SessionMd5(sessionData.autoLoginId));

                    DataTable userSessionTable = db.Query(query);

                    if (userSessionTable.Rows.Count == 1)
                    {
                        loggedInMember = new User(core, userSessionTable.Rows[0], UserLoadOptions.Info);
                        enableAutologin = isLoggedIn = true;
                    }
                    else
                    {
                        core.Template.Parse("REDIRECT_URI", "/");

                        if (record == null)
						{
							Response.Cookies.Clear();
							
                            HttpCookie sessionDataCookie = new HttpCookie(cookieName + "_data");
                            sessionDataCookie.Value = "";
                            sessionDataCookie.Expires = DateTime.MinValue;
                            sessionDataCookie.Secure = false; // TODO: secure cookies
                            Response.Cookies.Add(sessionDataCookie);

                            HttpCookie sessionSidCookie = new HttpCookie(cookieName + "_sid");
                            sessionSidCookie.Value = "";
                            sessionSidCookie.Expires = DateTime.MinValue;
                            sessionSidCookie.Secure = false; // TODO: secure cookies
                            Response.Cookies.Add(sessionSidCookie);
                        }

                        //core.Display.ShowMessage("Error", "Error starting session");
                        Response.Write("Error starting session");

                        if (db != null)
                        {
                            db.CloseConnection();
                        }
                        Response.End();
                        return null;
                    }
                }
                else if (!autoCreate)
                {
                    sessionData.autoLoginId = "";
                    sessionData.userId = userId;

                    SelectQuery query = User.GetSelectQueryStub(UserLoadOptions.Info | UserLoadOptions.Icon);
                    query.AddCondition("user_active", true);
                    query.AddCondition("user_keys.user_id", userId);

                    DataTable userSessionTable = db.Query(query);

                    if (userSessionTable.Rows.Count == 1)
                    {
                        loggedInMember = new User(core, userSessionTable.Rows[0], UserLoadOptions.Info | UserLoadOptions.Icon);
                        isLoggedIn = true;
                    }
                    else
                    {
                        // TODO: activation
                        //core.Display.ShowMessage("Inactive account", "You have attempted to use an inactive account. If you have just registered, check for an e-mail with an activation link at the e-mail address you provided.");
                        Response.Write("You have attempted to use an inactive account. If you have just registered, check for an e-mail with an activation link at the e-mail address you provided.");
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

                SelectQuery query = User.GetSelectQueryStub(UserLoadOptions.Info | UserLoadOptions.Icon);
                query.AddCondition("user_keys.user_id", userId);

                DataTable userTable = db.Query(query);

                if (userTable.Rows.Count == 1)
                {
                    loggedInMember = new User(core, userTable.Rows[0], UserLoadOptions.Info | UserLoadOptions.Icon);
                }
            }

            // INFO: phpBB2 performs a ban check, we don't have those facilities so let's skip

            //
            // Create or update the session
            //
            long changedRows = 0;

            if (record == null)
            {
                changedRows = db.UpdateQuery(string.Format("UPDATE user_sessions SET session_time_ut = UNIX_TIMESTAMP(), user_id = {0}, session_signed_in = {1} WHERE session_string = '{3}' AND session_ip = '{2}';",
                    userId, isLoggedIn, ipAddress.ToString(), sessionId));
            }

            if (changedRows == 0)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] randomNumber = new byte[16];
                rng.GetBytes(randomNumber);
                //Random rand = new Random((int)(DateTime.Now.Ticks & 0xFFFF));
                //rand.NextDouble().ToString()

                string rand = HexRNG(randomNumber);
                sessionId = SessionState.SessionMd5(rand + "bsseed" + DateTime.Now.Ticks.ToString() + ipAddress.ToString()).ToLower();

                db.UpdateQuery(string.Format("INSERT INTO user_sessions (session_string, session_time_ut, session_start_ut, session_signed_in, session_ip, user_id) VALUES ('{0}', UNIX_TIMESTAMP(), UNIX_TIMESTAMP(), {1}, '{2}', {3})",
                    sessionId, isLoggedIn, ipAddress.ToString(), userId));
            }

            if (record == null)
            {
                // 1 in 100 chance of deleting stale sessions
                // Move delete stale session code outside to allow guest sessions to clear stale sessions on low use websites
                Random rand = new Random();
                if (rand.NextDouble() * 100 < 1)
                {
                    db.UpdateQuery(string.Format("DELETE FROM user_sessions WHERE session_time_ut + {0} < UNIX_TIMESTAMP()",
                        SessionState.SESSION_EXPIRES));
                }

                if (userId != 0)
                {
                    TimeSpan ts = DateTime.Now - loggedInMember.UserInfo.LastOnlineTime;

                    if (ts.TotalMinutes >= 5)
                    {
                        db.UpdateQuery(string.Format("UPDATE user_info SET user_last_visit_ut = UNIX_TIMESTAMP() where user_id = {0}",
                            loggedInMember.UserId));
                    }

                    if (enableAutologin)
                    {
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
            }

            core.Uri.Sid = sessionId;

            if (record == null)
            {
				Response.Cookies.Clear();
				
                xs = new XmlSerializer(typeof(SessionCookie));
                StringBuilder sb = new StringBuilder();
                stw = new StringWriter(sb);

                HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");
                xs.Serialize(stw, sessionData);
                stw.Flush();
                stw.Close();
				
                newSessionDataCookie.Value = sb.ToString().Replace("\r", "").Replace("\n", "");
                newSessionDataCookie.Expires = DateTime.Now.AddYears(1);
                newSessionDataCookie.Secure = false; // TODO: secure cookies
                Response.Cookies.Add(newSessionDataCookie);

                HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
                newSessionSidCookie.Value = sessionId;
                newSessionSidCookie.Expires = DateTime.MinValue;
                newSessionSidCookie.Secure = false; // TODO: secure cookies
                Response.Cookies.Add(newSessionSidCookie);
            }

            return sessionId;
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

                    try
                    {
                        sessionData = (SessionCookie)xs.Deserialize(sr);
                    }
                    catch
                    {
                        sessionData = new SessionCookie();
                    }
                }
                else
                {
                    sessionData = new SessionCookie();
                }

                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = (string)Request.QueryString["sid"];
                }

                if ((Linker.Domain != core.Uri.CurrentDomain) && (sessionId != (string)Request.QueryString["sid"]) && (!string.IsNullOrEmpty((string)Request.QueryString["sid"])))
                {
                    sessionData = new SessionCookie();
                    sessionId = (string)Request.QueryString["sid"];
                }

                if ((core.Uri.CurrentDomain != Linker.Domain) && string.IsNullOrEmpty(sessionId))
                {
                    HttpContext.Current.Response.Redirect(Linker.Uri + string.Format("session.aspx?domain={0}&path={1}",
                        HttpContext.Current.Request.Url.Host, core.PagePath.TrimStart(new char[] { '/' })));
                    //return;
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
                SelectQuery query = User.GetSelectQueryStub(UserLoadOptions.Info | UserLoadOptions.Icon);
                query.AddFields("session_ip", "session_time_ut");
                query.AddJoin(JoinTypes.Inner, "user_sessions", "user_id", "user_id");
                query.AddCondition("session_string", sessionId);

                DataTable userSessionTable = db.Query(query);

                //
                // Did the session exist in the DB?
                //
                if (userSessionTable.Rows.Count == 1)
                {
                    DataRow userSessionRow = userSessionTable.Rows[0];
                    loggedInMember = new User(core, userSessionRow, UserLoadOptions.Info | UserLoadOptions.Icon);
                    core.Uri.Sid = sessionId;

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
                        UnixTime tzz = new UnixTime(core, UnixTime.UTC_CODE); // UTC
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
                                TimeSpan ts = DateTime.Now - loggedInMember.UserInfo.LastOnlineTime;

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
                        }
						
						Response.Cookies.Clear();

                        xs = new XmlSerializer(typeof(SessionCookie));
                        StringBuilder sb = new StringBuilder();
                        stw = new StringWriter(sb);

                        HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");
                        xs.Serialize(stw, sessionData);
                        stw.Flush();
                        stw.Close();
                        newSessionDataCookie.Value = sb.ToString().Replace("\r", "").Replace("\n", "");
                        newSessionDataCookie.Expires = DateTime.Now.AddYears(1);
                        newSessionDataCookie.Secure = false; // TODO: secure cookies
                        Response.Cookies.Add(newSessionDataCookie);

                        HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
                        newSessionSidCookie.Value = sessionId;
                        newSessionSidCookie.Expires = DateTime.MinValue;
                        newSessionSidCookie.Secure = false; // TODO: secure cookies
                        Response.Cookies.Add(newSessionSidCookie);

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

			// If the current domain is not the root domain, and the session is empty
            if ((core.Uri.CurrentDomain != Linker.Domain) && string.IsNullOrEmpty(sessionId))
            {
            }
            else
            {
                SessionBegin(userId, true);
            }
        }

        public void SessionEnd(string sessionId, long userId)
        {
            SessionEnd(sessionId, userId, null);
        }

        public void SessionEnd(string sessionId, long userId, DnsRecord record)
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

            if (record == null)
            {
				Response.Cookies.Clear();
				
                SelectQuery query = User.GetSelectQueryStub(UserLoadOptions.Info);
                query.AddCondition("user_keys.user_id", 0);

                DataTable userTable = db.Query(query);
				
				Response.Cookies.Clear();

                if (userTable.Rows.Count == 1)
				{
                    loggedInMember = new User(core, userTable.Rows[0], UserLoadOptions.Info);
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
            }

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

        public User LoggedInMember
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
            return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(input, "MD5").ToLower();
        }
		
		public static byte[] phpBBMd5(byte[] input)
		{
			System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            return md5.ComputeHash(input);
		}
		
		/*public static string phpBBMd5(string input)
		{
			System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            return ASCIIEncoding.ASCII.GetString(md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(input)));
		}*/
		
		public static string phpBB3Hash(string password, string setting, ref string itoa64)
		{
		     string output = "*";

            // Check for correct hash
            if (setting.Substring(0, 3) != "$H$")
            {
                return output;
            }

            int count_log2 = itoa64.IndexOf(setting[3]);
			
			if (count_log2 <= 0)
			{
				count_log2 = 0;
			}

            if (count_log2 < 7 || count_log2 > 30)
            {
                return output;
            }

            int count = 1 << count_log2;
            string salt = setting.Substring(4, 8);

            if (salt.Length != 8)
            {
                return output;
            }

            byte[] hash = SessionState.phpBBMd5(ASCIIEncoding.ASCII.GetBytes(salt + password));
			
			//Console.WriteLine(hash.Length);

            do
            {
                hash = SessionState.phpBBMd5(CombineByte(hash, ASCIIEncoding.ASCII.GetBytes(password)));
            }
            while ((--count) > 0);

            output = setting.Substring(0, 12);

            output += SessionState.phpBB3Encode64(hash, 16, ref itoa64);

            return output;
		}
		
		private static byte[] CombineByte(byte[] one, byte[] two)
		{
			byte[] ret = new byte[one.Length + two.Length];
			
			Array.Copy(one, 0, ret, 0, one.Length);
			Array.Copy(two, 0, ret, one.Length, two.Length);
			
			return ret;
		}
		
		private static string phpBB3Encode64(byte[] input, int count, ref string itoa64)
        {
            string output = "";
            int i = 0;

            do
            {
				/*Console.WriteLine(UTF7Encoding.UTF7.GetBytes(input[i].ToString()).Length);
				Console.WriteLine(Convert.ToChar(Convert.ToByte(input[i])));
				Console.WriteLine(input[i].ToString());*/
                int val = Convert.ToByte(input[i++]);
                output += itoa64[val & 0x3f];

                if (i < count)
                {
                    val |= Convert.ToByte(input[i]) << 8;
                }

                output += itoa64[(val >> 6) & 0x3f];

                if (i++ >= count)
                {
                    break;
                }

                if (i < count)
                {
                    val |= Convert.ToByte(input[i]) << 16;
                }

                output += itoa64[(val >> 12) & 0x3f];

                if (i++ >= count)
                {
                    break;
                }

                output += itoa64[(val >> 18) & 0x3f];
            }
            while (i < count);

            return output;
        }

        public static string HexRNG(byte[] input)
        {
            string output = string.Empty;

            for (int i = 0; i < input.Length; i++)
            {
                output += string.Format("{0:X2}", input[i]);
            }

            return output;
        }

        public static double GetDoubleRNG(RNGCryptoServiceProvider rng)
        {
            byte[] randomNumber = new byte[4];
            rng.GetBytes(randomNumber);

            double rand = (((((((uint)randomNumber[0] << 8) + (uint)randomNumber[1]) << 8) + (uint)randomNumber[2]) << 8) + (uint)randomNumber[3]) / (double)UInt32.MaxValue;

            return rand;
        }

        public static void RedirectAuthenticate()
        {
            HttpContext.Current.Response.Redirect(string.Format("/sign-in/?redirect={0}", HttpUtility.UrlEncode(HttpContext.Current.Request.RawUrl)), true);
        }
    }

    [XmlRoot("boxsocial-cookie")]
    public class SessionCookie
    {
        [XmlElement("autologinid")]
        public string autoLoginId;

        [XmlElement("userid")]
        public long userId;

        public SessionCookie()
        {
            autoLoginId = "";
            userId = 0;
        }
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

    public class InvalidSessionException : Exception
    {
    }
}
