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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class error_handler : TPage
    {
        public error_handler()
            : base("error.html")
        {
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            Display.ShowMessage("Error Message", ex.ToString(), ShowMessageOptions.Bbcode);

            try
            {
                Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "An Error occured at ZinZam.com", "URL: " + Request.RawUrl + "\nLOGGED IN:" + (core.LoggedInMemberId > 0).ToString() + "\nEXCEPTION THROWN:\n" + ex.ToString());
            }
            catch
            {
                try
                {
                    Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "An Error occured at ZinZam.com", "EXCEPTION THROWN:\n" + ex.ToString());
                }
                catch
                {
                }
            }

            Server.ClearError();

            EndResponse();
        }
    }
}
