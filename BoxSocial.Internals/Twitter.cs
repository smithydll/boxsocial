﻿/*
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
    public class Tweet
    {
        private long id;
        private string uri;

        public long Id
        {
            get
            {
                return id;
            }
        }

        public string Uri
        {
            get
            {
                return uri;
            }
        }

        public Tweet(long id, string uri)
        {
            this.id = id;
            this.uri = uri;
        }
    }

    public class TwitterAuthToken
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

        public TwitterAuthToken(string response)
        {
            NameValueCollection r = HttpUtility.ParseQueryString(response);

            token = r["oauth_token"];
            secret = r["oauth_token_secret"];
        }

        public TwitterAuthToken(string token, string secret)
        {
            this.token = token;
            this.secret = secret;
        }
    }

    public class TwitterAccessToken
    {
        private string token;
        private string secret;
        private string screenName;
        private long userId;

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

        public string ScreenName
        {
            get
            {
                return screenName;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public TwitterAccessToken(string response)
        {
            NameValueCollection r = HttpUtility.ParseQueryString(response);

            token = r["oauth_token"];
            secret = r["oauth_token_secret"];
            screenName = r["screen_name"];
            long.TryParse(r["user_id"], out userId);
        }

        public TwitterAccessToken(string token, string secret)
        {
            this.token = token;
            this.secret = secret;
        }
    }

    // Inspired by https://github.com/cyrus7580/twitter_api_examples/blob/master/src/net/adkitech/Twitter.java
    // Re-written in c# and tweaked due to changes in Twitter API
    public class Twitter
    {
        private string consumerKey;
        private string consumerSecret;

        public Twitter(string consumerKey, string consumerSecret)
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

        internal TwitterAuthToken OAuthRequestToken()
        {
            string method = "POST";
            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_version=1.0";

            string twitterEndpoint = "https://api.twitter.com/oauth/request_token";

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

            return new TwitterAuthToken(oauthToken, oauthTokenSecret);
        }

        internal TwitterAccessToken OAuthAccessToken(string verifierOrPin, string oauthToken)
        {
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(oauthToken) + "&oauth_version=1.0";

            string twitterEndpoint = "https://api.twitter.com/oauth/access_token";

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
                oAuthTimestamp + "\",oauth_nonce=\"" + oAuthNonce + "\",oauth_version=\"1.0\",oauth_signature=\"" + UrlEncode(oauthSignature) + "\",oauth_token=\"" + UrlEncode(oauthToken) + "\"";

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

                return new TwitterAccessToken(responseString);
            }

            return null;
        }

        //public Tweet StatusesUpdate(TwitterAccessToken token, string tweet)
        public Tweet StatusesUpdate(TwitterAccessToken token, ActionableItemType type, string tweet, byte[] media, string mediaContentType)
        {
            tweet = Functions.TrimString(tweet, 140);
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = string.Empty;
            string twitterEndpoint = string.Empty;

            if (mediaContentType == "image/gif")
            {
                // No-one uses a gif file unless animated, and twitter doesn't support animated, so we will link to instead
                type = ActionableItemType.Text;
            }

            switch (type)
            {
                case ActionableItemType.Photo:
                    twitterEndpoint = "https://api.twitter.com/1.1/statuses/update_with_media.json";
                    parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0";
                    break;
                case ActionableItemType.Audio:
                case ActionableItemType.Video:
                default:
                    twitterEndpoint = "https://api.twitter.com/1.1/statuses/update.json";
                    parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0&status=" + UrlEncode(tweet);
                    break;
            }

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

            string guid = Guid.NewGuid().ToString();
            string boundary = "----BSFB" + UnixTime.UnixTimeStamp().ToString() + guid.Replace("-", string.Empty);
            StringBuilder body = new StringBuilder();

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(twitterEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;
            if (media == null)
            {
                body.Append("status=" + UrlEncode(tweet));
                wr.ContentType = "application/x-www-form-urlencoded";
            }
            else
            {
                string extension = "";
                switch (mediaContentType)
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
                body.Append("Content-Disposition: form-data; name=\"status\"\r\n\r\n");
                body.Append(tweet + "\r\n");

                body.Append("--" + boundary + "\r\n");
                body.Append("Content-Disposition: form-data; name=\"media[]\"; filename=\"" + guid + extension + "\"\r\n");
                body.Append("Content-Type: " + mediaContentType + "\r\n\r\n");

                wr.ContentType = "multipart/form-data; boundary=" + boundary;
            }

            byte[] bodyBytes = UTF8Encoding.UTF8.GetBytes(body.ToString());
            byte[] boundaryBytes = UTF8Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

            if (media == null)
            {
                wr.ContentLength = bodyBytes.Length;
            }
            else
            {
                wr.ContentLength = bodyBytes.Length + media.Length + boundaryBytes.Length;
            }

            Stream stream = wr.GetRequestStream();
            stream.Write(bodyBytes, 0, bodyBytes.Length);
            if (media != null)
            {
                stream.Write(media, 0, media.Length);
                stream.Write(boundaryBytes, 0, boundaryBytes.Length);
            }

            stream.Close();

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            long tweetId = 0;
            string tweetUri = string.Empty;

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                JsonTextReader reader = new JsonTextReader(new StringReader(responseString));

                string lastToken = string.Empty;

                while (reader.Read())
                {
                    if (reader.Value != null)
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            lastToken = reader.Value.ToString();
                            if (lastToken == "user")
                            {
                                reader.Skip();
                                lastToken = string.Empty;
                            }
                        }
                        if (reader.TokenType == JsonToken.String && lastToken == "id_str")
                        {
                            long.TryParse(reader.Value.ToString(), out tweetId);
                            //tweetId = reader.Value.ToString();
                            lastToken = string.Empty;
                        }
                        if (reader.TokenType == JsonToken.String && lastToken == "url")
                        {
                            tweetUri = reader.Value.ToString();
                            lastToken = string.Empty;
                            break;
                        }
                    }
                }
            }

            return new Tweet(tweetId, tweetUri);
        }

        public void SaveTwitterAccess(Core core, string oAuthToken, string oAuthVerifier)
        {
            if (core.Session.IsLoggedIn)
            {
                if (oAuthToken == core.Session.LoggedInMember.UserInfo.TwitterToken)
                {
                    TwitterAccessToken access = OAuthAccessToken(oAuthVerifier, core.Session.LoggedInMember.UserInfo.TwitterToken);

                    if (access != null)
                    {
                        core.Session.LoggedInMember.UserInfo.TwitterAuthenticated = true;
                        core.Session.LoggedInMember.UserInfo.TwitterSyndicate = true;
                        core.Session.LoggedInMember.UserInfo.TwitterUserName = access.ScreenName;
                        core.Session.LoggedInMember.UserInfo.TwitterToken = access.Token;
                        core.Session.LoggedInMember.UserInfo.TwitterTokenSecret = access.Secret;

                        core.Session.LoggedInMember.UserInfo.Update();

                        core.Http.Redirect(core.Hyperlink.BuildAccountSubModuleUri("dashboard", "preferences"));
                    }
                }
            }
        }

        public void DeleteStatus(TwitterAccessToken token, long tweetId)
        {
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = "oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0";

            string twitterEndpoint = "https://api.twitter.com/1.1/statuses/destroy/" + tweetId.ToString() + ".json";

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
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                // ignore response
            }
        }
    }
}
