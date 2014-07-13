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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Configuration;
using RestSharp;

namespace BoxSocial.IO
{
    public class Mailgun : Email
    {
        private string baseUri;
        private string apiKey;
        private string domain;
        private string resource;
        private string fromEmail;
        private string fromName;

        public Mailgun(string baseUri, string apiKey, string domain, string fromEmail, string fromName)
        {
            this.domain = domain;
            this.baseUri = baseUri;
            this.apiKey = apiKey;
            this.fromEmail = fromEmail;
            this.fromName = fromName;
        }

        override public void SendEmail(string toAddress, string subject, string message)
        {
            RestClient client = new RestClient();
            client.BaseUrl = baseUri;
            client.Authenticator =new HttpBasicAuthenticator("api", apiKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", domain, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", fromName + " <" + fromEmail + ">");
            request.AddParameter("to", toAddress);
            request.AddParameter("subject", subject);
            request.AddParameter("text", message);
            request.Method = Method.POST;
            client.Execute(request);
        }

        override public void SendEmail(string toAddress, string subject, Template message)
        {
            RestClient client = new RestClient();
            client.BaseUrl = baseUri;
            client.Authenticator = new HttpBasicAuthenticator("api", apiKey);
            RestRequest request = new RestRequest();
            request.AddParameter("domain", domain, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", fromName + " <" + fromEmail + ">");
            request.AddParameter("to", toAddress);
            request.AddParameter("subject", subject);
            request.AddParameter("html", message.ToString());
            request.Method = Method.POST;
            client.Execute(request);
        }
    }
}
