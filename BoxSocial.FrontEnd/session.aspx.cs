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
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.FrontEnd
{
    public partial class session : TPage
    {
        public session()
            : base("session.html")
        {
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string domain = Request.QueryString["domain"];
            string path = Request.QueryString["path"];
            //string sessionId = Request.QueryString["sid"];

            try
            {
                DnsRecord record = new DnsRecord(core, domain);

                /*if (!string.IsNullOrEmpty(sessionId))
                {
                    core.session.SessionEnd(sessionId, 0, record);
                }*/

                string sessionId = core.Session.SessionBegin(core.LoggedInMemberId, false, false, false, record);

                Response.Redirect(core.Hyperlink.AppendSid("http://" + record.Domain + "/" + path, true));
            }
            catch (InvalidDnsRecordException)
            {
                core.Display.ShowMessage("Error", "Error starting remote session");
            }
        }
    }
}
