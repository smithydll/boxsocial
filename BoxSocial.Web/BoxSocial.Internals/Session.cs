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
using System.Diagnostics;
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
    public enum SessionSignInState : sbyte
    {
        Bot = -1,
        SignedOut = 0,
        SignedIn = 1,
        TwoFactorValidated = 2,
        ElevatedValidation = 3,
    }

    [DataTable("user_sessions", DataTableTypes.Volatile)]
    public class Session : NumberedItem
    {
        [DataField("session_id", DataFieldKeys.Primary)]
        private long sessionId;
        [DataField("user_id", DataFieldKeys.Index)]
        private long userId;
        [DataFieldKey(DataFieldKeys.Index, "i_sid_ip")]
        [DataField("session_string", DataFieldKeys.Index, 32)]
        private string sessionString;
        [DataField("session_root_string", DataFieldKeys.Index, 32)]
        private string sessionRootString;
        [DataField("session_start_ut")]
        private long sessionStartRaw;
        [DataField("session_time_ut")]
        private long sessionTimeRaw;
        [DataField("session_signed_in")]
        private sbyte sessionSignedIn;
        [DataFieldKey(DataFieldKeys.Index, "i_sid_ip")]
        [DataField("session_ip", IP)]
        private string sessionIp;
        [DataField("session_domain", 63)]
        private string sessionDomain;

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

        internal SessionSignInState SignedInState
        {
            get
            {
                return (SessionSignInState)sessionSignedIn;
            }
        }

        internal bool SignedIn
        {
            get
            {
                if (SignedInState == SessionSignInState.SignedIn)
                {
                    if (core.Session.LoggedInMember.UserInfo.TwoFactorAuthVerified)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (SignedInState == SessionSignInState.TwoFactorValidated || SignedInState == SessionSignInState.ElevatedValidation)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal bool TwoFactorValidationRequired
        {
            get
            {
                if (SignedInState == SessionSignInState.SignedIn)
                {
                    if (core.Session.LoggedInMember.UserInfo.TwoFactorAuthVerified)
                    {
                        return true;
                    }
                }
                return false;
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
        [DataField("key_browser_string", 255)]
        private string browserString;

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

        internal string BrowserString
        {
            get
            {
                return browserString;
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

        internal SessionKey(Core core, System.Data.Common.DbDataReader keyRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(SessionKey_ItemLoad);

            loadItemInfo(keyRow);
        }

        protected override void loadItemInfo(DataRow keyRow)
        {
            loadValue(keyRow, "key_id", out keyId);
            loadValue(keyRow, "user_id", out userId);
            loadValue(keyRow, "key_last_ip", out lastIp);
            loadValue(keyRow, "key_last_visit_ut", out lastVisitRaw);

            itemLoaded(keyRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader keyRow)
        {
            loadValue(keyRow, "key_id", out keyId);
            loadValue(keyRow, "user_id", out userId);
            loadValue(keyRow, "key_last_ip", out lastIp);
            loadValue(keyRow, "key_last_visit_ut", out lastVisitRaw);

            itemLoaded(keyRow);
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
        private const int SESSION_EXPIRES = 3600;

        private User loggedInMember;
        private IPAddress ipAddress;
        private bool isLoggedIn;
        private SessionSignInState signInState;
        private HttpRequest Request;
        private HttpResponse Response;
        private Mysql db;
        private SessionMethods sessionMethod;
        private SessionCookie sessionData;
        private Core core;
        private long applicationId;

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

        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
            internal set
            {
                applicationId = value;
            }
        }

        /// <summary>
        /// Sets the signed in state to bot
        /// </summary>
        public void SetBot()
        {
            signInState = SessionSignInState.Bot;
        }

        [Obsolete("IsLoggedIn is deprecated, please use SignedIn or SignedInState")]
        public bool IsLoggedIn
        {
            get
            {
                return SignedIn;
            }
        }

        public bool IsBot
        {
            get
            {
                return signInState == SessionSignInState.Bot;
            }
        }

        internal SessionSignInState SignedInState
        {
            get
            {
                return signInState;
            }
        }

        public bool SignedIn
        {
            get
            {
                if (SignedInState == SessionSignInState.SignedIn)
                {
                    if (CandidateMember.UserInfo.TwoFactorAuthVerified)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (SignedInState == SessionSignInState.TwoFactorValidated || SignedInState == SessionSignInState.ElevatedValidation)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal bool TwoFactorValidationRequired
        {
            get
            {
                if (SignedInState == SessionSignInState.SignedIn)
                {
                    if (LoggedInMember.UserInfo.TwoFactorAuthVerified)
                    {
                        return true;
                    }
                }
                return false;
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
            this.signInState = SessionSignInState.SignedOut;

            ipAddress = IPAddress.Parse(SessionState.ReturnRealIPAddress(Request.ServerVariables));
#if DEBUG
            Stopwatch httpTimer = new Stopwatch();
            httpTimer.Start();
#endif
            SessionPagestart(ipAddress.ToString());
#if DEBUG
            httpTimer.Stop();
            HttpContext.Current.Response.Write(string.Format("<!-- section A.1 in {0} -->\r\n", httpTimer.ElapsedTicks / 10000000.0));
#endif
            return;
            
        }

        public SessionState(Core core, Mysql db, OAuthToken token, HttpRequest Request, HttpResponse Response)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.Request = Request;
            this.Response = Response;
            this.db = db;
            this.core = core;

            applicationId = token.ApplicationId;

            SelectQuery query = new SelectQuery(typeof(PrimitiveApplicationInfo));
            query.AddCondition("application_id", token.ApplicationId);
            query.AddCondition("app_oauth_access_token", token.Token);

            System.Data.Common.DbDataReader appReader = core.Db.ReaderQuery(query);

            if (appReader.HasRows)
            {
                appReader.Read();
                PrimitiveApplicationInfo pai = new PrimitiveApplicationInfo(core, appReader);

                appReader.Close();
                appReader.Dispose();

                if (pai.Owner is User)
                {
                    this.core = core;
                    this.db = core.Db;
                    isLoggedIn = true;
                    this.signInState = SessionSignInState.SignedIn;
                    loggedInMember = (User)pai.Owner;
                    ipAddress = IPAddress.Parse(SessionState.ReturnRealIPAddress(Request.ServerVariables));
                    this.sessionMethod = SessionMethods.OAuth;
                }
            }
            else
            {
                appReader.Close();
                appReader.Dispose();

                this.core = core;
                this.db = core.Db;
                isLoggedIn = false;
                this.signInState = SessionSignInState.SignedOut;
                ipAddress = IPAddress.Parse(SessionState.ReturnRealIPAddress(Request.ServerVariables));
                this.sessionMethod = SessionMethods.OAuth;
            }
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
            /* only used by the installer, and background worker two factor will not be enabled at this point */
            this.signInState = SessionSignInState.SignedIn;
			loggedInMember = user;
			ipAddress = new IPAddress(0);
		}

        private static object botsLock = new object();
        private static Dictionary<string, string> bots = null;

        private string IsBotUserAgent(string ua)
        {
            /* This list of bots from phpBB 3.0.12 */

            lock (botsLock)
            {
                if (bots == null)
                {
                    bots = new Dictionary<string, string>(128, StringComparer.Ordinal);

                    bots.Add("AdsBot-Google", "Google AdsBot");
                    bots.Add("ia_archiver", "Alexa");
                    bots.Add("Scooter/", "Alta Vista");
                    bots.Add("Ask Jeeves", "Ask Jeeves");
                    bots.Add("Baiduspider", "Baidu");
                    bots.Add("bingbot/", "Bing");
                    bots.Add("Exabot", "Exabot");
                    bots.Add("FAST Enterprise Crawler", "FAST Enterprise");
                    bots.Add("FAST-WebCrawler/", "FAST WebCrawler");
                    bots.Add("http://www.neomo.de/", "Francis");
                    bots.Add("Gigabot/", "Gigabot");
                    bots.Add("Mediapartners-Google", "Google Adsense");
                    bots.Add("Google Desktop", "Google Desktop");
                    bots.Add("Feedfetcher-Google", "Google Feedfetcher");
                    bots.Add("Googlebot", "Google");
                    bots.Add("heise-IT-Markt-Crawler", "Heise IT-Markt");
                    bots.Add("heritrix/1.", "Heritrix");
                    bots.Add("ibm.com/cs/crawler", "IBM Research");
                    bots.Add("ICCrawler - ICjobs", "ICCrawler");
                    bots.Add("ichiro/", "ichiro");
                    bots.Add("MJ12bot/", "Majestic-12");
                    bots.Add("MetagerBot/", "Metager");
                    bots.Add("msnbot-NewsBlogs/", "MSN NewsBlogs");
                    bots.Add("msnbot/", "MSN");
                    bots.Add("msnbot-media/", "MSNbot Media");
                    bots.Add("http://lucene.apache.org/nutch/", "Nutch");
                    bots.Add("online link validator", "Online link");
                    bots.Add("psbot/0", "psbot");
                    bots.Add("Sensis Web Crawler", "Sensis");
                    bots.Add("SEO search Crawler/", "SEO");
                    bots.Add("Seoma [SEO Crawler]", "Seoma");
                    bots.Add("SEOsearch/", "SEOSearch");
                    bots.Add("Snappy/1.1 ( http://www.urltrends.com/ )", "Snappy");
                    bots.Add("http://www.tkl.iis.u-tokyo.ac.jp/~crawler/", "Steeler");
                    bots.Add("crawleradmin.t-info@telekom.de", "Telekom");
                    bots.Add("TurnitinBot/", "TurnitinBot");
                    bots.Add("voyager/", "Voyager");
                    bots.Add("W3 SiteSearch Crawler", "W3");
                    bots.Add("W3C-checklink/", "W3C");
                    bots.Add("W3C_Validator", "W3C");
                    bots.Add("yacybot", "YaCy");
                    bots.Add("Yahoo-MMCrawler/", "Yahoo MMCrawler");
                    bots.Add("Yahoo! DE Slurp", "Yahoo Slurp");
                    bots.Add("Yahoo! Slurp", "Yahoo");
                    bots.Add("YahooSeeker/", "YahooSeeker");
                }

                foreach (string key in bots.Keys)
                {
                    if ((!string.IsNullOrEmpty(key)))
                    {
                        if ((key.Contains("*") || key.Contains("#")) && Regex.IsMatch(ua, key.Replace("#", "\\#").Replace("\\*", ".*?"), RegexOptions.IgnoreCase))
                        {
                            return bots[key];
                        }
                        else
                        {
                            if (ua.ToLower().Contains(key.ToLower()))
                            {
                                return bots[key];
                            }
                        }
                    }
                }
            }

            return null;
        }

        public bool IsValidSid(string sid)
        {
            if (sid.Length == 32)
            {
                foreach (char c in sid)
                {
                    if (!char.IsLetterOrDigit(c))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
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

        public string SessionBegin(long userId, bool autoCreate, bool enableAutologin, bool twoFactor)
        {
            return SessionBegin(userId, autoCreate, enableAutologin, twoFactor, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="autoCreate"></param>
        /// <param name="enableAutologin"></param>
        /// <param name="admin"></param>
        public string SessionBegin(long userId, bool autoCreate, bool enableAutologin, bool twoFactor, DnsRecord record)
        {
            string cookieName = "hailToTheChef";
            /*XmlSerializer xs;
            StringWriter stw;*/

            string protocol = "http://";
            if (core.Settings.UseSecureCookies)
            {
                protocol = "https://";
            }

            string rootSessionId = string.Empty;
            if (record != null)
            {
                rootSessionId = core.Session.SessionId;
            }

            sessionData = null;
            sessionId = null;
            string currentDomain = core.Hyperlink.CurrentDomain;
            if (record != null)
            {
                currentDomain = record.Domain;
            }

            if (!String.IsNullOrEmpty(IsBotUserAgent(Request.UserAgent)))
            {
                signInState = SessionSignInState.Bot;
                core.Hyperlink.SidUrls = false;
                return sessionId;
            }

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
                        /*xs = new XmlSerializer(typeof(SessionCookie));
                        StringReader sr = new StringReader(HttpUtility.UrlDecode(Request.Cookies[cookieName + "_data"].Value));*/

                        try
                        {
                            sessionData = new SessionCookie(HttpUtility.UrlDecode(Request.Cookies[cookieName + "_data"].Value)); //(SessionCookie)xs.Deserialize(sr);
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
                if (!IsValidSid(sessionId))
                {
                    sessionId = "";
                }
            }

            if (record != null)
            {
                sessionMethod = SessionMethods.Get;
            }

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
                    SelectQuery query = User.GetSelectQueryStub(core, UserLoadOptions.Info);
                    query.AddJoin(JoinTypes.Inner, "session_keys", "user_id", "user_id");
                    query.AddCondition("user_keys.user_id", userId);
                    query.AddCondition("user_active", true);
                    query.AddCondition("key_id", SessionState.SessionMd5(sessionData.autoLoginId));

                    System.Data.Common.DbDataReader userReader = db.ReaderQuery(query);

                    if (userReader.HasRows)
                    {
                        userReader.Read();

                        loggedInMember = new User(core, userReader, UserLoadOptions.Info);

                        userReader.Close();
                        userReader.Dispose();

                        enableAutologin = isLoggedIn = true;
                        if (loggedInMember.UserInfo.TwoFactorAuthVerified && twoFactor)
                        {
                            signInState = SessionSignInState.TwoFactorValidated;
                        }
                        else
                        {
                            signInState = SessionSignInState.SignedIn;
                        }
                    }
                    else
                    {
                        userReader.Close();
                        userReader.Dispose();

                        core.Template.Parse("REDIRECT_URI", "/");

                        if (record == null)
						{
							Response.Cookies.Clear();
							
                            HttpCookie sessionDataCookie = new HttpCookie(cookieName + "_data");
                            //sessionDataCookie.Domain = core.Hyperlink.CurrentDomain;
                            sessionDataCookie.Path = "/";
                            sessionDataCookie.Value = "";
                            sessionDataCookie.Expires = DateTime.MinValue;
                            sessionDataCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                            sessionDataCookie.HttpOnly = true;
                            Response.Cookies.Add(sessionDataCookie);

                            HttpCookie sessionSidCookie = new HttpCookie(cookieName + "_sid");
                            //sessionSidCookie.Domain = core.Hyperlink.CurrentDomain;
                            sessionSidCookie.Path = "/";
                            sessionSidCookie.Value = "";
                            sessionSidCookie.Expires = DateTime.MinValue;
                            sessionDataCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                            sessionSidCookie.HttpOnly = true;
                            Response.Cookies.Add(sessionSidCookie);

                            if (Request.Cookies[cookieName + "_sid"] == null && signInState != SessionSignInState.Bot)
                            {
                                core.Hyperlink.SidUrls = true;
                            }
                        }

                        //core.Display.ShowMessage("Error", "Error starting session");
                        /*Response.Write("Error starting session");

                        if (db != null)
                        {
                            db.CloseConnection();
                        }
                        Response.End();
                        return null;*/

                        /* Let's try just signing out rather than showing an error message */
                        userId = 0;
                    }
                }
                else if (!autoCreate)
                {
                    sessionData.autoLoginId = "";
                    sessionData.userId = userId;

                    if (userId > 0)
                    {
                        SelectQuery query = User.GetSelectQueryStub(core, UserLoadOptions.Info);
                        query.AddCondition("user_active", true);
                        query.AddCondition("user_keys.user_id", userId);

                        System.Data.Common.DbDataReader userSessionReader = db.ReaderQuery(query);

                        if (userSessionReader.HasRows)
                        {
                            userSessionReader.Read();

                            loggedInMember = new User(core, userSessionReader, UserLoadOptions.Info);

                            userSessionReader.Close();
                            userSessionReader.Dispose();

                            isLoggedIn = true;
                            //signInState = SessionSignInState.SignedIn;
                            if (loggedInMember.UserInfo.TwoFactorAuthVerified && twoFactor)
                            {
                                signInState = SessionSignInState.TwoFactorValidated;
                            }
                            else
                            {
                                signInState = SessionSignInState.SignedIn;
                            }
                        }
                        else
                        {
                            userSessionReader.Close();
                            userSessionReader.Dispose();

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
                signInState = SessionSignInState.SignedOut;

                if (userId > 0)
                {
                    SelectQuery query = User.GetSelectQueryStub(core, UserLoadOptions.Info);
                    query.AddCondition("user_keys.user_id", userId);

                    System.Data.Common.DbDataReader userReader = db.ReaderQuery(query);

                    if (userReader.HasRows)
                    {
                        userReader.Read();

                        loggedInMember = new User(core, userReader, UserLoadOptions.Info);
                    }

                    userReader.Close();
                    userReader.Dispose();
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
                    userId, (byte)signInState, ipAddress.ToString(), sessionId));
            }

            if (changedRows == 0)
            {
                // This should force new sessions on external domains to re-auth rather than logout
                if (core.Hyperlink.CurrentDomain != Hyperlink.Domain)
                {
                    HttpContext.Current.Response.Redirect(protocol + Hyperlink.Domain + string.Format("/session.aspx?domain={0}&path={1}",
                        HttpContext.Current.Request.Url.Host, core.PagePath.TrimStart(new char[] { '/' })));
                    return string.Empty;
                }
                else
                {
                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                    byte[] randomNumber = new byte[16];
                    rng.GetBytes(randomNumber);
                    //Random rand = new Random((int)(DateTime.Now.Ticks & 0xFFFF));
                    //rand.NextDouble().ToString()

                    string rand = HexRNG(randomNumber);
                    sessionId = SessionState.SessionMd5(rand + "bsseed" + DateTime.Now.Ticks.ToString() + ipAddress.ToString()).ToLower();

                    if (record == null)
                    {
                        rootSessionId = sessionId;
                    }
                    db.UpdateQuery(string.Format("INSERT INTO user_sessions (session_string, session_time_ut, session_start_ut, session_signed_in, session_ip, user_id, session_root_string, session_domain) VALUES ('{0}', UNIX_TIMESTAMP(), UNIX_TIMESTAMP(), {1}, '{2}', {3}, '{4}', '{5}')",
                        sessionId, (byte)signInState, ipAddress.ToString(), userId, Mysql.Escape(rootSessionId), Mysql.Escape(currentDomain)));
                }
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
                    long ts = UnixTime.UnixTimeStamp() - loggedInMember.UserInfo.LastVisitDateRaw;

                    if (ts >= 60)
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
                            db.UpdateQuery(string.Format("INSERT INTO session_keys (key_id, user_id, key_last_ip, key_last_visit_ut, key_browser_string) VALUES ('{0}', {1}, '{2}', UNIX_TIMESTAMP(), '{3}')",
                                SessionState.SessionMd5(autoLoginKey), userId, ipAddress.ToString(), Mysql.Escape(Request.UserAgent)));
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

            core.Hyperlink.Sid = sessionId;

            if (record == null)
            {
				Response.Cookies.Clear();
				
                /*xs = new XmlSerializer(typeof(SessionCookie));
                StringBuilder sb = new StringBuilder();
                stw = new StringWriter(sb);

                xs.Serialize(stw, sessionData);
                stw.Flush();
                stw.Close();*/

                HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");

                //newSessionDataCookie.Domain = core.Hyperlink.CurrentDomain; // DO NOT DO THIS, exposes cookie to sub domains
                newSessionDataCookie.Path = "/";
                newSessionDataCookie.Value = sessionData.ToString().Replace("\r", "").Replace("\n", "");
                newSessionDataCookie.Expires = DateTime.Now.AddYears(1);
                newSessionDataCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                newSessionDataCookie.HttpOnly = true;
                Response.Cookies.Add(newSessionDataCookie);

                HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
                //newSessionSidCookie.Domain = core.Hyperlink.CurrentDomain; // DO NOT DO THIS, exposes cookie to sub domains
                newSessionSidCookie.Path = "/";
                newSessionSidCookie.Value = sessionId;
                newSessionSidCookie.Expires = DateTime.MinValue;
                newSessionSidCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                newSessionSidCookie.HttpOnly = true;
                Response.Cookies.Add(newSessionSidCookie);

                if (Request.Cookies[cookieName + "_sid"] == null && signInState != SessionSignInState.Bot)
                {
                    core.Hyperlink.SidUrls = true;
                }
            }

            return sessionId;
        }

        public void SessionPagestart(string userIp)
        {
#if DEBUG
            Stopwatch timeTimer = new Stopwatch();
            timeTimer.Start();
#endif
            long nowUt = UnixTime.UnixTimeStamp();
#if DEBUG
            timeTimer.Stop();
            HttpContext.Current.Response.Write(string.Format("<!-- section A.1.b in {0} -->\r\n", timeTimer.ElapsedTicks / 10000000.0));
#endif

            string cookieName = "hailToTheChef";
            /*XmlSerializer xs;
            StringWriter stw;*/

            string protocol = "http://";
            if (core.Settings.UseSecureCookies)
            {
                protocol = "https://";
            }

            sessionData = null;
            sessionId = null;

#if DEBUG
            Stopwatch botTimer = new Stopwatch();
            botTimer.Start();
#endif
            if (!String.IsNullOrEmpty(IsBotUserAgent(Request.UserAgent)))
            {
                signInState = SessionSignInState.Bot;
                core.Hyperlink.SidUrls = false;
                return;
            }
#if DEBUG
            botTimer.Stop();
            HttpContext.Current.Response.Write(string.Format("<!-- section A.1.c in {0} -->\r\n", botTimer.ElapsedTicks / 10000000.0));
#endif

#if DEBUG
            Stopwatch cookieTimer = new Stopwatch();
            cookieTimer.Start();
#endif
            HttpCookie sidCookie = Request.Cookies[cookieName + "_sid"];
            HttpCookie dataCookie = Request.Cookies[cookieName + "_data"];

            if (sidCookie != null || dataCookie != null)
            {
                if (sidCookie != null)
                {
                    sessionId = sidCookie.Value;
                }

                if (dataCookie != null)
                {
                    /*xs = new XmlSerializer(typeof(SessionCookie));
                    StringReader sr = new StringReader(HttpUtility.UrlDecode(Request.Cookies[cookieName + "_data"].Value));*/

                    try
                    {
                        sessionData = new SessionCookie(HttpUtility.UrlDecode(dataCookie.Value)); //(SessionCookie)xs.Deserialize(sr);
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

                if ((Hyperlink.Domain != core.Hyperlink.CurrentDomain) && (sessionId != (string)Request.QueryString["sid"]) && (!string.IsNullOrEmpty((string)Request.QueryString["sid"])))
                {
                    sessionData = new SessionCookie();
                    sessionId = (string)Request.QueryString["sid"];
                }

                if ((core.Hyperlink.CurrentDomain != Hyperlink.Domain) && string.IsNullOrEmpty(sessionId))
                {
                    HttpContext.Current.Response.Redirect(protocol + Hyperlink.Domain + string.Format("/session.aspx?domain={0}&path={1}",
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

                if ((core.Hyperlink.CurrentDomain != Hyperlink.Domain) && string.IsNullOrEmpty(sessionId))
                {
                    HttpContext.Current.Response.Redirect(protocol + Hyperlink.Domain + string.Format("/session.aspx?domain={0}&path={1}",
                        HttpContext.Current.Request.Url.Host, core.PagePath.TrimStart(new char[] { '/' })));
                    //return;
                }
            }
#if DEBUG
            cookieTimer.Stop();
            HttpContext.Current.Response.Write(string.Format("<!-- section A.1.d in {0} -->\r\n", cookieTimer.ElapsedTicks / 10000000.0));
#endif

#if DEBUG
            Stopwatch sidTimer = new Stopwatch();
            sidTimer.Start();
#endif
            if (!string.IsNullOrEmpty(sessionId))
            {
                if (!IsValidSid(sessionId))
                {
                    sessionId = "";
                }
            }
#if DEBUG
            sidTimer.Stop();
            HttpContext.Current.Response.Write(string.Format("<!-- section A.1.e in {0} -->\r\n", sidTimer.ElapsedTicks / 10000000.0));
#endif

            if (!string.IsNullOrEmpty(sessionId))
            {
                //
                // session_id exists so go ahead and attempt to grab all
                // data in preparation
                //
                SelectQuery query = User.GetSelectQueryStub(core, UserLoadOptions.Info);
                query.AddFields("session_ip", "session_time_ut", "session_signed_in");
                query.AddJoin(JoinTypes.Inner, new DataField(typeof(User), "user_id"), new DataField("user_sessions", "user_id"));
                query.AddCondition("session_string", sessionId);

                System.Data.Common.DbDataReader userSessionReader = db.ReaderQuery(query);

                //
                // Did the session exist in the DB?
                //
                if (userSessionReader.HasRows)
                {
                    userSessionReader.Read();
                    //DataRow userSessionRow = userSessionTable.Rows[0];
                    loggedInMember = new User(core, userSessionReader, UserLoadOptions.Info);

                    sbyte sessionSignedIn = (sbyte)userSessionReader["session_signed_in"];
                    long sessionTimeUt = (long)userSessionReader["session_time_ut"];
                    string sessionIp = (string)userSessionReader["session_ip"];

                    userSessionReader.Close();
                    userSessionReader.Dispose();

                    core.Hyperlink.Sid = sessionId;

                    if (loggedInMember.UserId != 0)
                    {
                        isLoggedIn = true;
                        if (loggedInMember.UserInfo.TwoFactorAuthVerified)
                        {
                            signInState = (SessionSignInState)sessionSignedIn;
                        }
                        else
                        {
                            signInState = SessionSignInState.SignedIn;
                        }
                    }

                    //
                    // Do not check IP assuming equivalence, if IPv4 we'll check only first 24
                    // bits ... I've been told (by vHiker) this should alleviate problems with 
                    // load balanced et al proxies while retaining some reliance on IP security.
                    //

                    // we will use complete matches in BoxSocial
                    if (sessionIp == userIp)
                    {
                        //
                        // Only update session DB a minute or so after last update
                        //
                        if (nowUt - sessionTimeUt >= 60)
                        {
                            long changedRows = db.UpdateQuery(string.Format("UPDATE user_sessions SET session_time_ut = UNIX_TIMESTAMP() WHERE session_string = '{0}';",
                                sessionId));


                            if (SignedIn)
                            {
                                long ts = UnixTime.UnixTimeStamp() - loggedInMember.UserInfo.LastVisitDateRaw;

                                if (ts >= 60)
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

#if DEBUG
                        Stopwatch cookie2Timer = new Stopwatch();
                        cookie2Timer.Start();
#endif
						Response.Cookies.Clear();

                        /*xs = new XmlSerializer(typeof(SessionCookie));
                        StringBuilder sb = new StringBuilder();
                        stw = new StringWriter(sb);

                        xs.Serialize(stw, sessionData);
                        stw.Flush();
                        stw.Close();*/

                        HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");

                        newSessionDataCookie.Value = sessionData.ToString().Replace("\r", "").Replace("\n", "");
                        newSessionDataCookie.Path = "/";
                        newSessionDataCookie.Expires = DateTime.Now.AddYears(1);
                        newSessionDataCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                        newSessionDataCookie.HttpOnly = true;
                        Response.Cookies.Add(newSessionDataCookie);

                        HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
                        newSessionSidCookie.Path = "/";
                        newSessionSidCookie.Value = sessionId;
                        newSessionSidCookie.Expires = DateTime.MinValue;
                        newSessionSidCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                        newSessionSidCookie.HttpOnly = true;
                        Response.Cookies.Add(newSessionSidCookie);

                        // Add the session_key to the userdata array if it is set

                        if (Request.Cookies[cookieName + "_sid"] == null && signInState != SessionSignInState.Bot)
                        {
                            core.Hyperlink.SidUrls = true;
                        }

#if DEBUG
                        cookie2Timer.Stop();
                        HttpContext.Current.Response.Write(string.Format("<!-- section A.1.f in {0} -->\r\n", cookie2Timer.ElapsedTicks / 10000000.0));
#endif

                        return;
                    }
                }
                else
                {
                    userSessionReader.Close();
                    userSessionReader.Dispose();

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
            if ((core.Hyperlink.CurrentDomain != Hyperlink.Domain) && userId > 0 /*&& string.IsNullOrEmpty(sessionId)*/)
            {
                if ((core.Hyperlink.CurrentDomain != Hyperlink.Domain) && string.IsNullOrEmpty(sessionId))
                {
                    HttpContext.Current.Response.Redirect(protocol + Hyperlink.Domain + string.Format("/session.aspx?domain={0}&path={1}",
                        HttpContext.Current.Request.Url.Host, core.PagePath.TrimStart(new char[] { '/' })));
                    //return;
                }
            }
            else
            {
#if DEBUG
                Stopwatch httpTimer = new Stopwatch();
                httpTimer.Start();
#endif
                SessionBegin(userId, true);
#if DEBUG
                httpTimer.Stop();
                HttpContext.Current.Response.Write(string.Format("<!-- section A.1.a in {0} -->\r\n", httpTimer.ElapsedTicks / 10000000.0));
#endif
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
                if (!IsValidSid(sessionId))
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
            if (record == null)
            {
                db.UpdateQuery(string.Format("DELETE FROM user_sessions WHERE (session_string = '{0}' OR session_root_string = '{0}') AND user_id = {1};",
                    sessionId, userId));
            }
            else
            {
                SelectQuery query = new SelectQuery(typeof(Session));
                query.AddCondition("session_string", sessionId);
                query.AddCondition("user_id", userId);
                query.AddCondition("session_domain", record.Domain);

                System.Data.Common.DbDataReader sessionReader = db.ReaderQuery(query);

                List<string> rootSessionIds = new List<string>();
                while (sessionReader.Read())
                {
                    rootSessionIds.Add((string)sessionReader["session_root_string"]);
                }

                sessionReader.Close();
                sessionReader.Dispose();

                if (rootSessionIds.Count > 0)
                {
                    DeleteQuery dQuery = new DeleteQuery(typeof(Session));
                    QueryCondition qc1 = dQuery.AddCondition("session_string", ConditionEquality.In, rootSessionIds);
                    qc1.AddCondition(ConditionRelations.Or, "session_root_string", ConditionEquality.In, rootSessionIds);
                    dQuery.AddCondition("user_id", userId);

                    db.Query(dQuery);
                }
            }

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

                SelectQuery query = User.GetSelectQueryStub(core, UserLoadOptions.Info);
                query.AddCondition("user_keys.user_id", 0);

                DataTable userTable = db.Query(query);
				
				Response.Cookies.Clear();

                if (userTable.Rows.Count == 1)
				{
                    loggedInMember = new User(core, userTable.Rows[0], UserLoadOptions.Info);
                }
                HttpCookie newSessionDataCookie = new HttpCookie(cookieName + "_data");
                newSessionDataCookie.Path = "/";
                newSessionDataCookie.Value = "";
                newSessionDataCookie.Expires = DateTime.Now.AddYears(-1);
                newSessionDataCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                newSessionDataCookie.HttpOnly = true;
                Response.Cookies.Add(newSessionDataCookie);

                HttpCookie newSessionSidCookie = new HttpCookie(cookieName + "_sid");
                newSessionSidCookie.Path = "/";
                newSessionSidCookie.Value = "";
                newSessionSidCookie.Expires = DateTime.Now.AddYears(-1);
                newSessionSidCookie.Secure = core.Settings.UseSecureCookies && core.Hyperlink.CurrentDomain == Hyperlink.Domain;
                newSessionSidCookie.HttpOnly = true;
                Response.Cookies.Add(newSessionSidCookie);

                if (Request.Cookies[cookieName + "_sid"] == null && signInState != SessionSignInState.Bot)
                {
                    core.Hyperlink.SidUrls = true;
                }
            }

            return;
        }

        public void ForceRecentAuthentication()
        {
            
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
                if (SignedIn)
                {
                    return loggedInMember;
                }
                else
                {
                    return null;
                }
            }
        }

        public User CandidateMember
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

        public SessionCookie(string cookie)
        {
            string[] parts = cookie.Split(new char[] { '>', '<' });

            bool autoLoginIdFound = false;
            bool userIdFound = false;

            string last = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                if (last == "autologinid" && !autoLoginIdFound)
                {
                    autoLoginId = parts[i];
                    autoLoginIdFound = true;
                }
                if (last == "userid" && !userIdFound)
                {
                    long.TryParse(parts[i], out userId);
                    userIdFound = true;
                }
                if (autoLoginIdFound && userIdFound)
                {
                    break;
                }
                last = parts[i];
            }
        }

        public override string ToString()
        {
            return string.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?><boxsocial-cookie><autologinid>{0}</autologinid><userid>{1}</userid></boxsocial-cookie>", autoLoginId, userId);
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
        Get,
        /// <summary>
        /// 
        /// </summary>
        OAuth,
    }

    public class InvalidSessionException : Exception
    {
    }
}
