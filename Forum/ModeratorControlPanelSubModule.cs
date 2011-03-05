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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Forms;
using BoxSocial.Internals;

namespace BoxSocial.Applications.Forum
{
    public abstract class ModeratorControlPanelSubModule : ControlPanelSubModule
    {
        public abstract override string Title
        {
            get;
        }

        public abstract override int Order
        {
            get;
        }

        public ModeratorControlPanelSubModule(Core core)
            : base(core)
        {
        }

        /// <summary>
        /// Creates an isolated template class for the module to render
        /// inside.
        /// </summary>
        private void CreateTemplate()
        {
            template = new Template(core.Http.TemplatePath, "1301.html");
            if (Owner != null)
            {
                template.Parse("U_MCP", core.Uri.AppendSid(Owner.UriStub + "forum/mcp/", true));
                template.Parse("S_MCP", core.Uri.AppendSid(Owner.UriStub + "forum/mcp/", true));
            }
            template.SetProse(core.Prose);
        }

        protected new string BuildUri(Core core, string sub)
        {
            return core.Uri.AppendSid(string.Format("{0}{1}/{2}",
                Owner.UriStub + "forum/mcp/", ModuleKey, sub));
        }

        public new string BuildUri(Core core)
        {
            return BuildUri(core, Key);
        }
    }
}
