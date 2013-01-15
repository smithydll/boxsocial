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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;

namespace BoxSocial.FrontEnd
{
    public partial class statistics : TPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            DataTable userData = db.Query("SELECT COUNT(user_id) as registrations FROM user_keys");
            DataTable inviteData = db.Query("SELECT COUNT(email_key) as invites FROM invite_keys");
            DataTable lastUserData = db.Query("SELECT * FROM user_keys ORDER BY user_id DESC LIMIT 1");

            Response.Clear();
            Response.ContentType = "text/plain";
            if (inviteData.Rows.Count > 0)
            {
                Response.Write("Invites Sent: " + inviteData.Rows[0]["invites"].ToString() + "\n");
            }
            if (userData.Rows.Count > 0)
            {
                Response.Write("Users: " + userData.Rows[0]["registrations"].ToString() + "\n");
            }

            Response.Write("Newest user: " + lastUserData.Rows[0]["user_name"].ToString() + "\n");

            Response.End();
        }
    }
}
