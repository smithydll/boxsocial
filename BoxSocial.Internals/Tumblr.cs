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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace BoxSocial.Internals
{
    public class TumblrPost
    {
    }

    public class TumblrAuthToken
    {
        private string token;
        private string secret;

        public string Token
        {
            get
            {
                return token;
            }
        }

        public string Secret
        {
            get
            {
                return secret;
            }
        }

        public TumblrAuthToken(string response)
        {
            NameValueCollection r = HttpUtility.ParseQueryString(response);

            token = r["oauth_token"];
            secret = r["oauth_token_secret"];
        }

        public TumblrAuthToken(string token, string secret)
        {
            this.token = token;
            this.secret = secret;
        }
    }

    public class TumblrAccessToken
    {
        private string token;
        private string secret;

        public string Token
        {
            get
            {
                return token;
            }
        }

        public string Secret
        {
            get
            {
                return secret;
            }
        }

        public TumblrAccessToken(string response)
        {
            NameValueCollection r = HttpUtility.ParseQueryString(response);

            token = r["oauth_token"];
            secret = r["oauth_token_secret"];
        }

        public TumblrAccessToken(string token, string secret)
        {
            this.token = token;
            this.secret = secret;
        }
    }

    public class TumblrUserInfo
    {
        private string userName;
        private List<Dictionary<string, string>> blogs;

        public string UserName
        {
            get
            {
                return userName;
            }
        }

        public List<Dictionary<string, string>> Blogs
        {
            get
            {
                return blogs;
            }
        }

        public TumblrUserInfo(string response)
        {
            blogs = new List<Dictionary<string, string>>();

            JObject json = JObject.Parse(response);
            userName = (string)json["response"]["user"]["name"];

            foreach (JObject blog in json["response"]["user"]["blogs"])
            {
                Dictionary<string, string> newBlog = new Dictionary<string, string>();

                newBlog.Add("name", (string)blog["name"]);
                newBlog.Add("title", (string)blog["title"]);
                newBlog.Add("url", (string)blog["url"]);
                newBlog.Add("primary", (bool)blog["primary"] ? "true" : "false");

                blogs.Add(newBlog);
            }
        }
    }

    public class Tumblr
    {
        private string consumerKey;
        private string consumerSecret;

        public Tumblr(string consumerKey, string consumerSecret)
        {
            this.consumerKey = consumerKey;
            this.consumerSecret = consumerSecret;
        }

        public static string UrlEncode(string value)
        {
            Regex reg = new Regex(@"%[a-f0-9]{2}");

            return reg.Replace(HttpUtility.UrlEncode(value), m => m.Value.ToUpperInvariant()).Replace("+", "%20").Replace("*", "%2A");
        }

        private static string computeSignature(string baseString, string keyString)
        {
            byte[] keyBytes = UTF8Encoding.UTF8.GetBytes(keyString);

            HMACSHA1 sha1 = new HMACSHA1(keyBytes);
            sha1.Initialize();

            byte[] baseBytes = UTF8Encoding.UTF8.GetBytes(baseString);

            byte[] text = sha1.ComputeHash(baseBytes);

            string signature = Convert.ToBase64String(text).Trim();

            return signature;
        }

        internal TumblrAuthToken OAuthRequestToken()
        {
            string method = "POST";
            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_version=1.0";

            string twitterEndpoint = "http://www.tumblr.com/oauth/request_token";

            string signature = method + "&" + UrlEncode(twitterEndpoint) + "&" + UrlEncode(parameters);

            String oauthSignature = string.Empty;
            try
            {
                oauthSignature = computeSignature(signature, consumerSecret + "&");
            }
            catch (Exception)
            {
            }

            string authorisationHeader = "OAuth oauth_consumer_key=\"" + consumerKey + "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" +
                oAuthTimestamp + "\",oauth_nonce=\"" + oAuthNonce + "\",oauth_version=\"1.0\",oauth_signature=\"" + UrlEncode(oauthSignature) + "\"";

            string oauthToken = "";
            string oauthTokenSecret = "";

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(twitterEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                NameValueCollection r = HttpUtility.ParseQueryString(responseString);

                oauthToken = r["oauth_token"];
                oauthTokenSecret = r["oauth_token_secret"];
            }

            return new TumblrAuthToken(oauthToken, oauthTokenSecret);
        }

        internal TumblrAccessToken OAuthAccessToken(string verifierOrPin, string oauthToken, string oauthSecret)
        {
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(oauthToken) + "&oauth_verifier=" + UrlEncode(verifierOrPin) + "&oauth_version=1.0";

            string twitterEndpoint = "http://www.tumblr.com/oauth/access_token";

            string signature = method + "&" + UrlEncode(twitterEndpoint) + "&" + UrlEncode(parameters);

            String oauthSignature = string.Empty;
            try
            {
                oauthSignature = computeSignature(signature, consumerSecret + "&" + oauthSecret);
            }
            catch (Exception)
            {
            }

            string authorisationHeader = "OAuth oauth_consumer_key=\"" + consumerKey + "\",oauth_nonce=\"" + oAuthNonce + "\",oauth_signature=\"" + UrlEncode(oauthSignature) + "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" +
                oAuthTimestamp + "\",oauth_token=\"" + UrlEncode(oauthToken) + "\",oauth_version=\"1.0\"";

            string body = "oauth_verifier=" + UrlEncode(verifierOrPin);

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(twitterEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;
            wr.ContentLength = body.Length;

            byte[] bodyBytes = UTF8Encoding.UTF8.GetBytes(body);

            Stream stream = wr.GetRequestStream();
            stream.Write(bodyBytes, 0, bodyBytes.Length);

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                return new TumblrAccessToken(responseString);
            }

            return null;
        }

        public TumblrUserInfo GetUserInfo(TumblrAccessToken token)
        {
            string method = "GET";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0";

            string twitterEndpoint = "http://api.tumblr.com/v2/user/info";

            string signature = method + "&" + UrlEncode(twitterEndpoint) + "&" + UrlEncode(parameters);

            String oauthSignature = string.Empty;
            try
            {
                oauthSignature = computeSignature(signature, consumerSecret + "&" + token.Secret);
            }
            catch (Exception ex)
            {
            }

            string authorisationHeader = "OAuth oauth_consumer_key=\"" + consumerKey + "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" +
                oAuthTimestamp + "\",oauth_nonce=\"" + oAuthNonce + "\",oauth_version=\"1.0\",oauth_signature=\"" + UrlEncode(oauthSignature) + "\",oauth_token=\"" + UrlEncode(token.Token) + "\"";

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(twitterEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                return new TumblrUserInfo(responseString);
            }

            return null;
        }

        public void SaveTumblrAccess(Core core, string oAuthToken, string oAuthVerifier)
        {
            if (core.Session.IsLoggedIn)
            {
                if (oAuthToken == core.Session.LoggedInMember.UserInfo.TumblrToken)
                {
                    //HttpContext.Current.Response.Write(oAuthToken + "\r\n<br />\r\n" + oAuthVerifier + "\r\n<br />\r\n" + core.Session.LoggedInMember.UserInfo.TumblrToken + "\r\n<br />\r\n");

                    TumblrAccessToken access = OAuthAccessToken(oAuthVerifier, core.Session.LoggedInMember.UserInfo.TumblrToken, core.Session.LoggedInMember.UserInfo.TumblrTokenSecret);

                    if (access != null)
                    {
                        //HttpContext.Current.Response.Write(access.Token + "\r\n<br />\r\n" + access.Secret);
                        //HttpContext.Current.Response.End();

                        TumblrUserInfo info = GetUserInfo(access);

                        if (info != null)
                        {
                            core.Session.LoggedInMember.UserInfo.TumblrAuthenticated = true;
                            core.Session.LoggedInMember.UserInfo.TumblrSyndicate = false;
                            core.Session.LoggedInMember.UserInfo.TumblrToken = access.Token;
                            core.Session.LoggedInMember.UserInfo.TumblrTokenSecret = access.Secret;

                            core.Session.LoggedInMember.UserInfo.TumblrUserName = info.UserName;
                            for (int i = 0; i < info.Blogs.Count; i++ )
                            {
                                if (info.Blogs[i]["primary"] == "true")
                                {
                                    core.Session.LoggedInMember.UserInfo.TumblrSyndicate = true;
                                    core.Session.LoggedInMember.UserInfo.TumblrHostname = new Uri(info.Blogs[i]["url"]).Host;
                                }
                            }

                            core.Session.LoggedInMember.UserInfo.Update();
                        }

                        core.Http.Redirect(core.Hyperlink.BuildAccountSubModuleUri("dashboard", "preferences"));
                    }
                }
            }
        }
    }
}
