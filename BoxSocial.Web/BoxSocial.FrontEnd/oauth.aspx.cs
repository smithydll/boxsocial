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
            string method = core.Http["global_method"];
            //EndResponse();

            switch (method)
            {
                case "request_token":
                    RequestOAuthRequestToken();
                    break;
                case "access_token":
                    RequestOAuthAccessToken();
                    break;
                case "call":
                    InitiateApplicationMethod();
                    break;
            }
        }

        private void InitiateApplicationMethod()
        {
            string applicationName = core.Http.Query["global_an"];
            string callName = core.Http.Query["global_call"];

            OAuthApplication oae = null;
            string nonce = null;

            if (AuthoriseRequest("/oauth/" + applicationName + "/" + callName, null, out oae, out nonce))
            {
                if (applicationName == "Internals")
                {
                    core.InvokeApplicationCall(null, callName);
                }
                else
                {
                    try
                    {
                        ApplicationEntry ae = new ApplicationEntry(core, applicationName);

                        core.InvokeApplicationCall(ae, callName);
                    }
                    catch (InvalidApplicationException)
                    {
                    }
                }
            }
            else
            {
                core.Http.StatusCode = 401;

                NameValueCollection response = new NameValueCollection();
                response.Add("error", "unauthorised, access token rejected");

                core.Http.WriteAndEndResponse(response);
                return;
            }
        }

        private void RequestOAuthRequestToken()
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

                    db.CommitTransaction();

                    core.Http.WriteAndEndResponse(response);
                }
                catch (NonceViolationException)
                {
                    core.Http.StatusCode = 401;

                    NameValueCollection response = new NameValueCollection();
                    response.Add("error", "unauthorised, nonce violation");

                    core.Http.WriteAndEndResponse(response);
                    return;
                }
            }
            else
            {
                core.Http.StatusCode = 401;

                NameValueCollection response = new NameValueCollection();
                response.Add("error", "unauthorised, access token rejected");

                core.Http.WriteAndEndResponse(response);
                return;
            }
        }

        private void RequestOAuthAccessToken()
        {
            // Final step in oauth handshake

            OAuthApplication oae = null;
            string nonce = null;

            string verifier = core.Http.Form["oauth_verifier"];

            OAuthVerifier oAuthVerifier = null;
            OAuthToken oauthToken = null;

            try
            {
                oAuthVerifier = new OAuthVerifier(core, verifier);
            }
            catch (InvalidOAuthVerifierException)
            {
                core.Http.StatusCode = 401;

                NameValueCollection response = new NameValueCollection();
                response.Add("error", "unauthorised, invalid verifier");

                core.Http.WriteAndEndResponse(response);
                return;
            }

            if (oAuthVerifier.Expired)
            {
                core.Http.StatusCode = 401;

                NameValueCollection response = new NameValueCollection();
                response.Add("error", "unauthorised, verifier expired");

                core.Http.WriteAndEndResponse(response);
                return;
            }

            try
            {
                oauthToken = new OAuthToken(core, oAuthVerifier.TokenId);
            }
            catch (InvalidOAuthTokenException)
            {
                core.Http.StatusCode = 401;

                NameValueCollection response = new NameValueCollection();
                response.Add("error", "unauthorised, invalid token");

                core.Http.WriteAndEndResponse(response);
                return;
            }


            if (AuthoriseRequest("/oauth/access_token", oauthToken, out oae, out nonce))
            {
                oAuthVerifier.UseVerifier();

                // TODO: check application is not already installed
                SelectQuery query = new SelectQuery(typeof(PrimitiveApplicationInfo));
                query.AddCondition("application_id", oauthToken.ApplicationId);
                query.AddCondition("item_id", oAuthVerifier.UserId);
                query.AddCondition("item_type_id", ItemKey.GetTypeId(core, typeof(User)));

                System.Data.Common.DbDataReader appReader = db.ReaderQuery(query);

                if (!appReader.HasRows)
                {
                    appReader.Close();
                    appReader.Dispose();

                    OAuthToken oauthAuthToken = OAuthToken.Create(core, oae, nonce);

                    InsertQuery iQuery = new InsertQuery("primitive_apps");
                    iQuery.AddField("application_id", oauthToken.ApplicationId);
                    iQuery.AddField("item_id", oAuthVerifier.UserId);
                    iQuery.AddField("item_type_id", ItemKey.GetTypeId(core, typeof(User)));
                    iQuery.AddField("app_email_notifications", true);
                    iQuery.AddField("app_oauth_access_token", oauthAuthToken.Token);
                    iQuery.AddField("app_oauth_access_token_secret", oauthAuthToken.TokenSecret);

                    if (core.Db.Query(iQuery) > 0)
                    {
                        // successfull
                    }

                    db.CommitTransaction();

                    NameValueCollection response = new NameValueCollection();
                    response.Add("oauth_token", oauthAuthToken.Token);
                    response.Add("oauth_token_secret", oauthAuthToken.TokenSecret);

                    core.Http.WriteAndEndResponse(response);
                }
                else
                {
                    appReader.Read();

                    PrimitiveApplicationInfo pai = new PrimitiveApplicationInfo(core, appReader);

                    appReader.Close();
                    appReader.Dispose();

                    NameValueCollection response = new NameValueCollection();
                    response.Add("oauth_token", pai.OAuthAccessToken);
                    response.Add("oauth_token_secret", pai.OAuthAccessTokenSecret);

                    core.Http.WriteAndEndResponse(response);
                }
            }
            else
            {
                // FAIL
                core.Http.StatusCode = 401;

                NameValueCollection response = new NameValueCollection();
                response.Add("error", "unauthorised, access token rejected");

                core.Http.WriteAndEndResponse(response);
                core.Http.End();
                return;
            }
        }

        private bool AuthoriseRequest(string path, OAuthToken token, out OAuthApplication oae, out string nonce)
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
                        if (string.IsNullOrEmpty(key))
                        {
                        }
                        else if (key.StartsWith("global_"))
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

                    foreach (string key in core.Http.Form.Keys)
                    {
                        //if (key == "oauth_verifier")
                        if (string.IsNullOrEmpty(key))
                        {
                        }
                        else
                        {
                            signatureParamaters.Add(key, core.Http.Form[key]);
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

                    if (token == null)
                    {
                        string oauthToken = authorisationHeaders["oauth_token"];

                        if (!string.IsNullOrEmpty(oauthToken))
                        {
                            try
                            {
                                token = new OAuthToken(core, oauthToken);

                                // start session
                                StartSession(token);
                            }
                            catch (InvalidOAuthTokenException)
                            {
                                oae = null;
                                nonce = null;
                                return false;
                            }
                        }
                    }

                    string requestSignature = authorisationHeaders["oauth_signature"];
                    string expectedSignature = OAuth.ComputeSignature(signature, oae.ApiSecret + "&" + (token != null ? token.TokenSecret : string.Empty));

                    if (requestSignature == expectedSignature)
                    {
                        return true;
                    }
#if DEBUG
                    else
                    {
                        HttpContext.Current.Response.Write("Request signature: " + requestSignature + "\r\n");
                        HttpContext.Current.Response.Write("Expected signature: " + expectedSignature + "\r\n");
                        HttpContext.Current.Response.Write("signature: " + signature + "\r\n");
                        if (token != null)
                        {
                            HttpContext.Current.Response.Write("token: " + token.Token + "\r\n");
                            HttpContext.Current.Response.Write("secret: " + token.TokenSecret + "\r\n");
                        }
                    }
#endif
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
