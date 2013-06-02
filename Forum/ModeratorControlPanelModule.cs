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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Forms;
using BoxSocial.Internals;

namespace BoxSocial.Applications.Forum
{
    public abstract class ModeratorControlPanelModule : ControlPanelModule
    {
        public ModeratorControlPanelModule(Account account)
            : base(account)
        {
        }

        protected abstract override void RegisterModule(Core core, EventArgs e);

        public abstract override string Name
        {
            get;
        }

        public abstract override int Order
        {
            get;
        }

        /// <summary>
        /// Creates an isolated template class for the module to render
        /// inside.
        /// </summary>
        public new void CreateTemplate()
        {
            template = new Template(core.Http.TemplatePath, "1301.html");
            template.Parse("U_ACCOUNT", core.Hyperlink.AppendSid(Owner.AccountUriStub, true));
            if (assembly != null)
            {
                template.AddPageAssembly(assembly);
                template.SetProse(core.Prose);
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", "Forum" });
            breadCrumbParts.Add(new string[] { "mcp", "Moderator Control Panel" });

            Owner.ParseBreadCrumbs(core.Template, "BREADCRUMBS", breadCrumbParts);
        }
    }
}
