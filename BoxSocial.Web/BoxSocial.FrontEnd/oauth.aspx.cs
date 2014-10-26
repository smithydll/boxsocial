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
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class oauth : OPage
    {
        public oauth()
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string method = core.Http["method"];
            //EndResponse();

            switch (method)
            {
                case "request_token":
                    RequestOAuthToken();
                    break;
                case "access_token":
                    break;
            }
        }

        private void RequestOAuthToken()
        {
            OAuthApplication oae = null;
            string nonce = null;

            if (AuthoriseRequest("/oauth/request_token", null, out oae, out nonce))
            {
                try
                {
                    OAuthToken token = OAuthToken.Create(core, oae, nonce);

                    NameValueCollection response = new NameValueCollection();
                    response.Add("oauth_token", token.Token);
                    response.Add("oauth_token_secret", token.TokenSecret);
                    response.Add("oauth_callback_confirmed", "true");

                    core.Http.WriteAndEndResponse(response);
                }
                catch (NonceViolationException)
                {
#if DEBUG
                    HttpContext.Current.Response.Write("Nonce violation");
#endif
                    core.Http.StatusCode = 403;
                    core.Http.End();
                }
            }
            else
            {
#if DEBUG
                HttpContext.Current.Response.Write("Authorization failed");
#endif
                core.Http.StatusCode = 403;
                core.Http.End();
            }
        }

        private bool AuthoriseRequest(string path, OAuthToken token, out OAuthApplication oae , out string nonce)
        {
            string authorisationHeader = ReadAuthorisationHeader();

            if (authorisationHeader != null && authorisationHeader.StartsWith("OAuth "))
            {
                NameValueCollection authorisationHeaders = ParseAuthorisationHeader(authorisationHeader.Substring(6));

                string requestConsumerKey = authorisationHeaders["oauth_consumer_key"];

                try
                {
                    oae = new OAuthApplication(core, requestConsumerKey);
                    nonce = authorisationHeaders["oauth_nonce"];

                    SortedDictionary<string, string> signatureParamaters = new SortedDictionary<string, string>();

                    foreach (string key in authorisationHeaders.Keys)
                    {
                        if (key == null)
                        {
                        }
                        else if (key == "oauth_signature")
                        {
                        }
                        else
                        {
                            signatureParamaters.Add(key, authorisationHeaders[key]);
                        }
                    }

                    foreach (string key in core.Http.Query.Keys)
                    {
                        if (key == null)
                        {
                        }
                        else if (key == "method")
                        {
                        }
                        else if (key == "oauth_signature")
                        {
                        }
                        else
                        {
                            signatureParamaters.Add(key, core.Http.Query[key]);
                        }
                    }

                    string parameters = string.Empty;

                    foreach (string key in signatureParamaters.Keys)
                    {
                        if (parameters != string.Empty)
                        {
                            parameters += "&";
                        }
                        parameters += string.Format("{0}={1}", OAuth.UrlEncode(key), OAuth.UrlEncode(signatureParamaters[key]));
                    }

                    string signature = core.Http.HttpMethod + "&" + OAuth.UrlEncode(core.Http.Host + path) + "&" + OAuth.UrlEncode(parameters);

                    string requestSignature = authorisationHeaders["oauth_signature"];
                    string expectedSignature = OAuth.ComputeSignature(signature, oae.ApiSecret + "&" + (token != null ? token.TokenSecret : string.Empty));

                    if (requestSignature == expectedSignature)
                    {
                        return true;
                    }
                }
                catch (InvalidApplicationException)
                {
                    oae = null;
                    nonce = null;
                    return false;
                }
            }

            oae = null;
            nonce = null;
            return false;
        }

        private string ReadAuthorisationHeader()
        {
            return Request.Headers["Authorization"];
        }

        private NameValueCollection ParseAuthorisationHeader(string header)
        {
            NameValueCollection result = new NameValueCollection();

            bool inDoubleQuote = false;
            bool inValue = false;
            string key = string.Empty;
            string value = string.Empty;
            char previous = ' ';
            for (int i = 0; i < header.Length; i++)
            {
                char c = header[i];

                if (!inDoubleQuote && c == '=')
                {
                    inValue = true;
                }
                else if (c == '"')
                {
                    inDoubleQuote = !inDoubleQuote;
                    if (previous == '"')
                    {
                        if (inValue)
                        {
                            value += c;
                        }
                        else
                        {
                            key += c;
                        }
                    }
                }
                else if (!inDoubleQuote && c == ',')
                {
                    result.Add(HttpUtility.UrlDecode(key), HttpUtility.UrlDecode(value));
                    key = string.Empty;
                    value = string.Empty;
                    inValue = false;
                }
                else if (inValue)
                {
                    value += c;
                }
                else
                {
                    key += c;
                }

                previous = c;
            }

            if (key.Length > 0)
            {
                result.Add(HttpUtility.UrlDecode(key), HttpUtility.UrlDecode(value));
            }

            return result;
        }
    }
}
