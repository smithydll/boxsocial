/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
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

namespace BoxSocial.FrontEnd
{
    public partial class login : TPage
    {
        public login()
            : base("login.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string redirect = (Request.Form["redirect"] != null) ? Request.Form["redirect"] : Request.QueryString["redirect"];

            if (Request.QueryString["mode"] == "sign-out")
            {
                //FormsAuthentication.SignOut();
                // TODO: make better
                session.SessionEnd(Request.QueryString["sid"], loggedInMember.UserId);
                if (!string.IsNullOrEmpty(redirect))
                {
                    Response.Redirect(redirect, true);
                }
                else
                {
                    Response.Redirect("/", true);
                }
            }
            if (Request.Form["submit"] != null)
            {
                string userName = Request.Form["username"];
                string password = BoxSocial.Internals.User.HashPassword(Request.Form["password"]);

                 DataTable userTable = db.Query(string.Format("SELECT uk.user_name, uk.user_id FROM user_keys uk INNER JOIN user_info ui ON uk.user_id = ui.user_id WHERE uk.user_name = '{0}' AND ui.user_password = '{1}'",
                    userName, password));

                if (userTable.Rows.Count == 1)
                {
                    DataRow userRow = userTable.Rows[0];
                    if (Request.Form["remember"] == "true")
                    {
                        session.SessionBegin((long)userRow["user_id"], false, true);
                    }
                    else
                    {
                        session.SessionBegin((long)userRow["user_id"], false, false);
                    }
                    if (!string.IsNullOrEmpty(redirect))
                    {
                        if (redirect.StartsWith("/account"))
                        {
                            redirect = Linker.AppendSid(Linker.StripSid(redirect), true);
                        }
                        Response.Redirect(redirect, true);
                    }
                    else
                    {
                        Response.Redirect("/", true);
                    }
                    return; /* stop processing the display of this page */
                }
                else
                {
                    template.Parse("ERROR", "Bad log in credentials were given, you could not be logged in. Try again.");
                }
            }

            template.Parse("REDIRECT", redirect);

            EndResponse();
        }
    }
}
