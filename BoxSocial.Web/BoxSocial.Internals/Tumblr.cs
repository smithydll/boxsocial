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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class TumblrPost
    {
        private long id;

        public long Id
        {
            get
            {
                return id;
            }
        }

        public TumblrPost(string response)
        {
            JObject json = JObject.Parse(response);
            id = (long)json["response"]["id"];
        }
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

        public static string UrlEncode(byte[] data)
        {
            Regex reg = new Regex(@"%[a-f0-9]{2}");

            return reg.Replace(HttpUtility.UrlEncode(data), m => m.Value.ToUpperInvariant()).Replace("+", "%20").Replace("*", "%2A");
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

        public TumblrPost StatusesUpdate(TumblrAccessToken token, string hostname, ActionableItemType type, string title, string post, string link, byte[] data, string dataContentType)
        {
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string postType = string.Empty;
            string parameters = string.Empty;
            switch (type)
            {
                case ActionableItemType.Photo:
                    postType = "photo";
                    parameters = "caption=" + UrlEncode(post) + "&format=html&link=" + UrlEncode(link) + "&oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0&type=" + postType;
                    break;
                case ActionableItemType.Audio:
                    postType = "audio";
                    parameters = "caption=" + UrlEncode(post) + "&format=html&oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0&type=" + postType;
                    break;
                case ActionableItemType.Video:
                    postType = "video";
                    break;
                default:
                    postType = "text";
                    parameters = "body=" + UrlEncode(post) + "&format=html&oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0&type=" + postType;
                    break;
            }

            string twitterEndpoint = string.Format("http://api.tumblr.com/v2/blog/{0}/post", hostname);

            string signature = method + "&" + UrlEncode(twitterEndpoint) + "&" + UrlEncode(parameters);

            String oauthSignature = string.Empty;
            try
            {
                oauthSignature = computeSignature(signature, consumerSecret + "&" + token.Secret);
            }
            catch (Exception)
            {
            }

            string authorisationHeader = "OAuth oauth_consumer_key=\"" + consumerKey + "\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"" +
                oAuthTimestamp + "\",oauth_nonce=\"" + oAuthNonce + "\",oauth_version=\"1.0\",oauth_signature=\"" + UrlEncode(oauthSignature) + "\",oauth_token=\"" + UrlEncode(token.Token) + "\"";

            string guid = Guid.NewGuid().ToString();
            string boundary = "----BSFB" + UnixTime.UnixTimeStamp().ToString() + guid.Replace("-", string.Empty);
            StringBuilder body = new StringBuilder();

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(twitterEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;
            if (data == null)
            {
                body.Append("type=" + postType + "&format=html&body=" + UrlEncode(post));

                wr.ContentType = "application/x-www-form-urlencoded";
            }
            else
            {
                string extension = "";
                switch (dataContentType)
                {
                    case "image/jpeg":
                        extension = ".jpg";
                        break;
                    case "image/gif":
                        extension = ".gif";
                        break;
                    case "image/png":
                        extension = ".png";
                        break;
                }

                body.Append("--" + boundary + "\r\n");
                body.Append("Content-Disposition: form-data; name=\"type\"\r\n\r\n");
                body.Append(postType + "\r\n");

                body.Append("--" + boundary + "\r\n");
                body.Append("Content-Disposition: form-data; name=\"format\"\r\n\r\n");
                body.Append("html\r\n");

                body.Append("--" + boundary + "\r\n");
                body.Append("Content-Disposition: form-data; name=\"link\"\r\n\r\n");
                body.Append(link + "\r\n");

                body.Append("--" + boundary + "\r\n");
                body.Append("Content-Disposition: form-data; name=\"caption\"\r\n\r\n");
                body.Append(post + "\r\n");

                body.Append("--" + boundary + "\r\n");
                body.Append("Content-Disposition: form-data; name=\"data[0]\"; filename=\"" + guid + extension + "\"\r\n");
                body.Append("Content-Type: " + dataContentType + "\r\n\r\n");

                wr.ContentType = "multipart/form-data; boundary=" + boundary;
            }

            byte[] bodyBytes = UTF8Encoding.UTF8.GetBytes(body.ToString());
            byte[] boundaryBytes = UTF8Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

            if (data == null)
            {
                wr.ContentLength = bodyBytes.Length;
            }
            else
            {
                wr.ContentLength = bodyBytes.Length + data.Length + boundaryBytes.Length;
            }

            Stream stream = wr.GetRequestStream();
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            if (data != null)
            {
                stream.Write(data, 0, data.Length);
                stream.Write(boundaryBytes, 0, boundaryBytes.Length);
            }
            
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                return new TumblrPost(responseString);
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

        public void DeleteStatus(TumblrAccessToken token, string hostname, long postId)
        {
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "id=" + postId.ToString() + "&oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0";

            string twitterEndpoint = string.Format("http://api.tumblr.com/v2/blog/{0}/post/delete", hostname);

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

            string body = "id=" + postId.ToString();

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
                // ignore response
            }
        }

        public static bool PublishPost(Core core, Job job)
        {
            core.LoadUserProfile(job.UserId);
            User owner = core.PrimitiveCache[job.UserId];
            ItemKey sharedItemKey = new ItemKey(job.ItemId, job.ItemTypeId);
            IActionableItem sharedItem = null;

            core.ItemCache.RequestItem(sharedItemKey);
            try
            {
                sharedItem = (IActionableItem)core.ItemCache[sharedItemKey];
            }
            catch
            {
                try
                {
                    sharedItem = (IActionableItem)NumberedItem.Reflect(core, sharedItemKey);
                    HttpContext.Current.Response.Write("<br />Fallback, had to reflect: " + sharedItemKey.ToString());
                }
                catch
                {
                    return true; // Item is probably deleted, report success to delete from queue
                }
            }

            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", sharedItemKey.Id);
            uQuery.AddCondition("info_item_type_id", sharedItemKey.TypeId);

            try
            {
                if (owner.UserInfo.TumblrAuthenticated) // are we still authenticated
                {
                    string postDescription = job.Body;

                    Tumblr t = new Tumblr(core.Settings.TumblrApiKey, core.Settings.TumblrApiSecret);
                    TumblrPost post = t.StatusesUpdate(new TumblrAccessToken(owner.UserInfo.TumblrToken, owner.UserInfo.TumblrTokenSecret), owner.UserInfo.TumblrHostname, sharedItem.PostType, string.Empty, postDescription, sharedItem.Info.ShareUri, sharedItem.Data, sharedItem.DataContentType);

                    if (post != null)
                    {
                        uQuery.AddField("info_tumblr_post_id", post.Id);
                    }

                    core.Db.Query(uQuery);
                }
            }
            catch (System.Net.WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return true; // This request cannot succeed, so remove it from the queue
                }
                return false; // Failed for other reasons, retry
            }

            return true; // success
        }
    }
}
