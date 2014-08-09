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
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Configuration;

namespace BoxSocial.IO
{
    public class HttpSmsGateway : SmsGateway
    {
        private string smsEndpoint;

        public HttpSmsGateway(string smsEndpoint)
        {
            this.smsEndpoint = smsEndpoint;
        }

        public override void SendSms(string toNumber, string message)
        {
            if (message.Length > 160)
            {
                message = message.Substring(0, 160);
            }

            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(string.Format(smsEndpoint, HttpUtility.UrlEncode(toNumber), HttpUtility.UrlEncode(message)));
            wr.ProtocolVersion = HttpVersion.Version11;
            wr.UserAgent = "HttpCore/1.1";
            wr.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)wr.GetResponse();
        }
    }
}
