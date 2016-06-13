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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class Sms2AccessToken
    {
        private string accessToken;
        private long expires;

        public string AccessToken
        {
            get
            {
                return accessToken;
            }
        }

        public long Expires
        {
            get
            {
                return expires;
            }
        }
        public Sms2AccessToken(string accessToken, long expires)
        {
            this.accessToken = accessToken;
            this.expires = expires;
        }
    }

        public class OAuth2SmsGateway : SmsGateway
    {
        private string consumerKey;
        private string consumerSecret;
        private string oauthTokenUri;
        private string oauthTokenParameters;
        private string oauthTokenAuthorization;
        private string oauthSmsUri;
        private string oauthSmsBody;

        public OAuth2SmsGateway(string oauthTokenUri, string oauthSmsUri, string oauthKey, string oauthSecret, string oauthTokenParameters, string oauthTokenAuthorization, string oauthSmsBody)
        {
            this.oauthTokenUri = oauthTokenUri;
            this.oauthSmsUri = oauthSmsUri;
            this.consumerKey = oauthKey;
            this.consumerSecret = oauthSecret;
            this.oauthTokenParameters = oauthTokenParameters;
            this.oauthTokenAuthorization = oauthTokenAuthorization;
            this.oauthSmsBody = oauthSmsBody;
        }

        public override void SendSms(string toNumber, string message)
        {
            Sms2AccessToken token = OAuthAccessToken(consumerKey, consumerSecret);

            string method = "POST";
            string smsEndpoint = oauthSmsUri;
            string authorisationHeader = string.Format(oauthTokenAuthorization, token.AccessToken);

            StringBuilder body = new StringBuilder();

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(smsEndpoint);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.Method = method;
            wr.Headers["Authorization"] = authorisationHeader;

            if (oauthSmsBody.Trim().StartsWith("{"))
            {
                // Escape JSON
                body.Append(string.Format("{" + oauthSmsBody.Trim() + "}", JsonConvert.ToString(toNumber), JsonConvert.ToString(message)));
            }
            else
            {
                body.Append(string.Format(oauthSmsBody, UrlEncode(toNumber), UrlEncode(message)));
            }
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

        internal Sms2AccessToken OAuthAccessToken(string verifierOrPin, string oauthToken)
        {
            string method = "POST";

            string body = string.Format(oauthTokenParameters, UrlEncode(consumerKey), UrlEncode(consumerSecret));

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(oauthTokenUri);
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Method = method;
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

                if (responseString.Trim().StartsWith("{"))
                {
                    // JSON

                    Dictionary<string, string> responseDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                    if (responseDictionary.ContainsKey("access_token"))
                    {
                        long expires = 0;

                        if (responseDictionary.ContainsKey("expires_in"))
                        {
                            long.TryParse(responseDictionary["expires_in"], out expires);
                        }
                        if (responseDictionary.ContainsKey("expires"))
                        {
                            long.TryParse(responseDictionary["expires"], out expires);
                        }

                        Sms2AccessToken token = new Sms2AccessToken(responseDictionary["access_token"], expires);
                        return token;
                    }
                }
                else
                {
                    // Query string

                    NameValueCollection responseDictionary = HttpUtility.ParseQueryString(responseString);

                    if (!string.IsNullOrEmpty(responseDictionary["access_token"]))
                    {
                        long expires = 0;

                        if (!string.IsNullOrEmpty(responseDictionary["expires_in"]))
                        {
                            long.TryParse(responseDictionary["expires_in"], out expires);
                        }
                        if (!string.IsNullOrEmpty(responseDictionary["expires"]))
                        {
                            long.TryParse(responseDictionary["expires"], out expires);
                        }

                        Sms2AccessToken token = new Sms2AccessToken(responseDictionary["access_token"], expires);
                        return token;
                    }
                }
            }

            return null;
        }
    }
}
