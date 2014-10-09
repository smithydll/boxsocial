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
        }

        private bool AuthoriseRequest()
        {
            string authorisationHeader = ReadAuthorisationHeader();

            if (authorisationHeader.StartsWith("OAuth "))
            {
                NameValueCollection authorisationHeaders = HttpUtility.ParseQueryString(authorisationHeader.Substring(6));

                string requestConsumerKey = authorisationHeaders["oauth_consumer_key"];

                OAuthApplication oae = new OAuthApplication(core, requestConsumerKey);

                string requestSignature = authorisationHeaders["oauth_signature"];
                string expectedSignature = "";

                if (requestSignature == expectedSignature)
                {
                    return true;
                }
            }

            return false;
        }

        private string ReadAuthorisationHeader()
        {
            return Request.Headers["Authorization"];
        }
    }
}
