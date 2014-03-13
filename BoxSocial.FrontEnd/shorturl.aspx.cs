 /* http://boxsocial.net/
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;

namespace BoxSocial.FrontEnd
{
    public partial class shorturl : TPage
    {
        HttpContext httpContext;
        public shorturl()
            : base()
        {
            httpContext = HttpContext.Current;
            this.Load += new EventHandler(Page_Load);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            string key = core.Http["key"];

            try
            {
                ItemInfo info = new ItemInfo(core, key);

                core.Http.StatusCode = 301;
                core.Http.ForceDomain = true;
                core.Http.Redirect(info.Uri);
            }
            catch (InvalidIteminfoException)
            {
                core.Functions.Generate404();
            }
            catch
            {
                core.Functions.Generate404();
            }
        }
    }
}
