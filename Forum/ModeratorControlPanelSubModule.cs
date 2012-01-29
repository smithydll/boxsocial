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
using System.Reflection;
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

        /// <summary>
        /// Initializes a new instance of the ModeratorControlPanelSubModule class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public ModeratorControlPanelSubModule(Core core)
            : base(core)
        {
        }

        /// <summary>
        /// Creates an isolated template class for the module to render
        /// inside.
        /// </summary>
        private new void CreateTemplate()
        {
            string formSubmitUri = string.Empty;
            template = new Template(core.Http.TemplatePath, "1301.html");
            if (Owner != null)
            {
                formSubmitUri = core.Uri.AppendSid(Owner.UriStub + "forum/mcp", true);
                template.Parse("U_MCP", core.Uri.AppendSid(Owner.UriStub + "forum/mcp/", true));
                template.Parse("S_MCP", core.Uri.AppendSid(Owner.UriStub + "forum/mcp/", true));
            }
            template.AddPageAssembly(Assembly.GetCallingAssembly());
            template.SetProse(core.Prose);

            Form = new Form("control-panel", formSubmitUri);
            Form.SetValues(core.Http.Form);
            if (core.Http.Form["save"] != null)
            {
                Form.IsFormSubmission = true;
            }

            core.Template.Parse("IS_CONTENT", "FALSE");

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "forum", "Forum" });
            breadCrumbParts.Add(new string[] { "mcp", "Moderator Control Panel" });

            Owner.ParseBreadCrumbs(core.Template, "BREADCRUMBS", breadCrumbParts);
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
