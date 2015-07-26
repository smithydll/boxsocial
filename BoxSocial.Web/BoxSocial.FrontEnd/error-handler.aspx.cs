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
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();

            if (ex.TargetSite.Name == "AssertVirtualPathExists")
            {
                template.SetTemplate("404.html");
                core.Http.StatusCode = 404;
                EndResponse();
            }
            else if (ex.TargetSite.DeclaringType.Name == "HttpForbiddenHandler")
            {
                template.SetTemplate("403.html");
                core.Http.StatusCode = 403;
                EndResponse();
            }
            else
            {

#if DEBUG
                core.Display.ShowMessage("Error Message", "An error occured" + "\n\n" + ex.ToString(), ShowMessageOptions.Bbcode);
#else
                core.Display.ShowMessage("Error Message", "An error occured");
#endif

                try
                {
                    core.Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "An Error occured at " + Hyperlink.Domain, "URL: " + Request.RawUrl + "\nLOGGED IN:" + (core.LoggedInMemberId > 0).ToString() + "\nREFERER: " + core.Http.UrlReferer + "\nHOSTNAME: (" + core.Http.IpAddress + ")\nEXCEPTION THROWN:\n" + ex.ToString());
                }
                catch
                {
                    try
                    {
                        core.Email.SendEmail(WebConfigurationManager.AppSettings["error-email"], "An Error occured at " + Hyperlink.Domain, "\nREFERER: " + core.Http.UrlReferer + "\nHOSTNAME: (" + core.Http.IpAddress + ")\nEXCEPTION THROWN:\n" + ex.ToString());
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
}
