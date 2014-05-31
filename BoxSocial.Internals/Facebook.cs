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
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    public class FacebookPost
    {
        private string postId;

        public string PostId
        {
            get
            {
                return postId;
            }
        }

        public FacebookPost(string postId)
        {
            this.postId = postId;
        }
    }

    public class FacebookAccessToken
    {
        private string accessToken;
        private string code;
        private string userId;
        private long expires;

        public string AccessToken
        {
            get
            {
                return accessToken;
            }
        }

        public string Code
        {
            get
            {
                return code;
            }
        }

        public string UserId
        {
            get
            {
                return userId;
            }
        }

        public long Expires
        {
            get
            {
                return expires;
            }
        }

        public FacebookAccessToken(string accessToken, string userId)
        {
            this.accessToken = accessToken;
            this.userId = userId;
            this.expires = 0;
        }

        public FacebookAccessToken(string code, string accessToken, long expires)
        {
            this.accessToken = accessToken;
            this.code = code;
            this.expires = UnixTime.UnixTimeStamp() + expires;
        }

        public FacebookAccessToken(string code, string accessToken, string userId, long expires)
        {
            this.accessToken = accessToken;
            this.code = code;
            this.userId = userId;
            this.expires = UnixTime.UnixTimeStamp() + expires;
        }
    }

    public class Facebook
    {
        private string appId;
        private string appSecret;

        public Facebook(string appId, string appSecret)
        {
            this.appId = appId;
            this.appSecret = appSecret;
        }

        public string UrlEncode(string value)
        {
            Regex reg = new Regex(@"%[a-f0-9]{2}");

            return reg.Replace(HttpUtility.UrlEncode(value), m => m.Value.ToUpperInvariant());
        }

        internal FacebookAccessToken OAuthAppAccessToken(Core core, string userId)
        {
            string facebookEndpoint = "https://graph.facebook.com/oauth/access_token";

            string tokenArgs = string.Format("client_id={0}&client_secret={1}&grant_type=client_credentials",
                appId, appSecret);

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(facebookEndpoint + "?" + tokenArgs);

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                NameValueCollection responseDictionary = HttpUtility.ParseQueryString(responseString);

                if (!string.IsNullOrEmpty(responseDictionary["access_token"]))
                {
                    FacebookAccessToken token = new FacebookAccessToken(responseDictionary["access_token"], userId);
                    return token;
                }
            }

            return null;
        }

        internal FacebookAccessToken OAuthAccessToken(Core core, string code)
        {
            string facebookEndpoint = "https://graph.facebook.com/oauth/access_token";
            string redirectTo = (core.Settings.UseSecureCookies ? "https://" : "http://") + Hyperlink.Domain + "/api/facebook/callback";

            string tokenArgs = string.Format("client_id={0}&redirect_uri={1}&client_secret={2}&code={3}",
                appId, UrlEncode(redirectTo), appSecret, code);

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(facebookEndpoint + "?" + tokenArgs);

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                NameValueCollection responseDictionary = HttpUtility.ParseQueryString(responseString);

                if (!string.IsNullOrEmpty(responseDictionary["access_token"]))
                {
                    long expires = 0;
                    long.TryParse(responseDictionary["expires"], out expires);

                    FacebookAccessToken token = new FacebookAccessToken(code, responseDictionary["access_token"], expires);
                    return RefreshOAuthAccessToken(core, token);
                }
            }

            return null;
        }

        internal FacebookAccessToken RefreshOAuthAccessToken(Core core, FacebookAccessToken token)
        {
            string facebookEndpoint = "https://graph.facebook.com/oauth/access_token";
            string redirectTo = (core.Settings.UseSecureCookies ? "https://" : "http://") + Hyperlink.Domain + "/api/facebook/callback";

            string tokenArgs = string.Format("grant_type=fb_exchange_token&client_id={0}&client_secret={1}&fb_exchange_token={2}",
                appId, appSecret, token.AccessToken);

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(facebookEndpoint + "?" + tokenArgs);

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                NameValueCollection responseDictionary = HttpUtility.ParseQueryString(responseString);

                if (!string.IsNullOrEmpty(responseDictionary["access_token"]))
                {
                    long expires = 0;
                    long.TryParse(responseDictionary["expires"], out expires);

                    return new FacebookAccessToken(string.Empty, responseDictionary["access_token"], expires);
                }
            }

            return null;
        }

        private Dictionary<string, string> GetUserInfo(FacebookAccessToken token)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();

            string facebookEndpoint = "https://graph.facebook.com/me";

            string tokenArgs = string.Format("access_token={0}",
                UrlEncode(token.AccessToken));

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(facebookEndpoint + "?" + tokenArgs);

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                info = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }

            return info;
        }

        public FacebookPost StatusesUpdate(FacebookAccessToken token, string message, string link)
        {
            string method = "POST";
            string facebookEndpoint = string.Format("https://graph.facebook.com/{0}/feed", token.UserId);

            string body = "message=" + UrlEncode(message);

            if (!string.IsNullOrEmpty(link))
            {
                body += "&link=" + UrlEncode(link);
            }
            body += "&privacy=" + UrlEncode("{'value':'EVERYONE'}");
            body += "&access_token=" + UrlEncode(token.AccessToken);

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(facebookEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Method = method;
            wr.ContentLength = body.Length;

            byte[] bodyBytes = UTF8Encoding.UTF8.GetBytes(body);

            Stream stream = wr.GetRequestStream();
            stream.Write(bodyBytes, 0, bodyBytes.Length);

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            string postId = string.Empty;

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                Dictionary<string, string> info = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                postId = info["id"];
            }

            return new FacebookPost(postId);
        }

        public void SaveFacebookAccess(Core core, string code)
        {
            if (core.Session.IsLoggedIn)
            {
                FacebookAccessToken access = OAuthAccessToken(core, code);

                    if (access != null)
                    {
                        core.Session.LoggedInMember.UserInfo.FacebookAuthenticated = true;
                        core.Session.LoggedInMember.UserInfo.FacebookSyndicate = true;
                        core.Session.LoggedInMember.UserInfo.FacebookCode = access.Code;
                        core.Session.LoggedInMember.UserInfo.FacebookAccessToken = access.AccessToken;
                        core.Session.LoggedInMember.UserInfo.FacebookExpires = access.Expires;

                        Dictionary<string, string> info = GetUserInfo(access);

                        core.Session.LoggedInMember.UserInfo.FacebookUserId = info["id"];

                        core.Session.LoggedInMember.UserInfo.Update();

                        core.Http.Redirect(core.Hyperlink.BuildAccountSubModuleUri("dashboard", "preferences"));
                    }
            }
        }

        public void DeleteStatus(FacebookAccessToken token, string postId)
        {
            string method = "DELETE";
            string facebookEndpoint = string.Format("https://graph.facebook.com/{0}", postId);

            string tokenArgs = string.Format("access_token={0}",
                token.AccessToken);

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(facebookEndpoint + "?" + tokenArgs);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Method = method;

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();


            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();
            }
        }
    }
}
