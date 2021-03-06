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
using System.Data;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Configuration;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class SmsAccessToken
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

        public SmsAccessToken(string response)
        {
            NameValueCollection r = HttpUtility.ParseQueryString(response);

            token = r["oauth_token"];
            secret = r["oauth_token_secret"];
        }

        public SmsAccessToken(string token, string secret)
        {
            this.token = token;
            this.secret = secret;
        }
    }

    public class OAuthSmsGateway : SmsGateway
    {
        private string consumerKey;
        private string consumerSecret;
        private string oauthTokenUri;
        private string oauthTokenParameters;
        private string oauthSmsUri;

        public OAuthSmsGateway(string oauthTokenUri, string oauthSmsUri, string oauthKey, string oauthSecret)
        {
            this.oauthTokenUri = oauthTokenUri;
            this.oauthSmsUri = oauthSmsUri;
            this.consumerKey = oauthKey;
            this.consumerSecret = oauthSecret;
        }

        public override void SendSms(string toNumber, string message)
        {
            SmsAccessToken token = new SmsAccessToken(consumerKey, consumerSecret);

            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = string.Empty;
            string smsEndpoint = oauthSmsUri;

            parameters = "body=" + UrlEncode(message) + "&oauth_consumer_key=" + consumerKey + "&oauth_nonce=" + oAuthNonce + "&oauth_signature_method=" + oAuthSignatureMethod + "&oauth_timestamp=" + oAuthTimestamp + "&oauth_token=" + UrlEncode(token.Token) + "&oauth_version=1.0&to=" + UrlEncode(toNumber);

            string signature = method + "&" + UrlEncode(smsEndpoint) + "&" + UrlEncode(parameters);

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

            StringBuilder body = new StringBuilder();

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(smsEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;

            body.Append("body=" + UrlEncode(message) + "&to=" + UrlEncode(toNumber));
            wr.ContentType = "application/x-www-form-urlencoded";

            byte[] bodyBytes = UTF8Encoding.UTF8.GetBytes(body.ToString());

            wr.ContentLength = bodyBytes.Length;

            Stream stream = wr.GetRequestStream();
            stream.Write(bodyBytes, 0, bodyBytes.Length);

            stream.Close();

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
            }
            else
            {
                Encoding encode = Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(response.GetResponseStream(), encode);

                string responseString = sr.ReadToEnd();

                // we could decode the error, but why?
            }
        }

        public static string UrlEncode(string value)
        {
            Regex reg = new Regex(@"%[a-f0-9]{2}");

            return reg.Replace(HttpUtility.UrlEncode(value), m => m.Value.ToUpperInvariant()).Replace("+", "%20").Replace("*", "%2A").Replace("!", "%21").Replace("(", "%28").Replace(")", "%29");
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

        public static string BuildParameters(string parametric, Dictionary<string, string> parameters)
        {
            string output = string.Empty;

            SortedDictionary<string, string> sortedParameters = new SortedDictionary<string, string>(parameters);

            string[] p = parametric.Split(new char[] { '&' });

            foreach (string pp in p)
            {
                string[] parts = parametric.Split(new char[] { '=' });

                if (parts.Length == 2)
                {
                    sortedParameters.Add(parts[0], parts[1]);
                }
            }

            foreach (string key in sortedParameters.Keys)
            {
                if (output != string.Empty)
                {
                    output += "&";
                }

                output += UrlEncode(sortedParameters[key]);
            }

            return output;
        }

        internal SmsAccessToken OAuthAccessToken(string verifierOrPin, string oauthToken)
        {
            string method = "POST";

            string oAuthSignatureMethod = "HMAC-SHA1";

            string oAuthNonce = Guid.NewGuid().ToString().Replace("-", string.Empty);
            string oAuthTimestamp = UnixTime.UnixTimeStamp().ToString();

            string parameters = BuildParameters(oauthTokenParameters, new Dictionary<string, string>()
            {
                { "oauth_consumer_key", consumerKey },
                { "oauth_nonce", oAuthNonce },
                { "oauth_signature_method", oAuthSignatureMethod },
                { "oauth_timestamp", oAuthTimestamp },
                { "oauth_token", oauthToken },
                { "oauth_version", "1.0" }
            });

            string signature = method + "&" + UrlEncode(oauthTokenUri) + "&" + UrlEncode(parameters);

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

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(oauthSmsUri);
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

                return new SmsAccessToken(responseString);
            }

            return null;
        }

    }
}
