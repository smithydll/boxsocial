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
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    public class AppInfo : Application
    {

        public AppInfo(Core core)
            : base(core)
        {
        }

        public override string Title
        {
            get
            {
                return "Enterprise Resource Planning";
            }
        }

        public override string Stub
        {
            get
            {
                return "erp";
            }
        }

        public override string Description
        {
            get
            {
                return "";
            }
        }

        public override bool UsesComments
        {
            get
            {
                return false;
            }
        }

        public override bool UsesRatings
        {
            get
            {
                return false;
            }
        }

        public override System.Drawing.Image Icon
        {
            get
            {
                //return Properties.Resources.forum;
                return null;
            }
        }

        public override byte[] SvgIcon
        {
            get
            {
                return Properties.Resources.svgIcon;
            }
        }

        public override string StyleSheet
        {
            get
            {
                return Properties.Resources.style;
            }
        }

        public override string JavaScript
        {
            get
            {
                return Properties.Resources.script;
            }
        }

        public override void Initialise(Core core)
        {
            this.core = core;

            core.PageHooks += new Core.HookHandler(core_PageHooks);
            core.LoadApplication += new Core.LoadHandler(core_LoadApplication);
        }

        public override ApplicationInstallationInfo Install()
        {
            ApplicationInstallationInfo aii = base.GetInstallInfo();

            return aii;
        }

        public override Dictionary<string, string> PageSlugs
        {
            get
            {
                Dictionary<string, string> slugs = new Dictionary<string, string>();
                slugs.Add("erp", "Enterprise Resource Planning");
                return slugs;
            }
        }

        void core_LoadApplication(Core core, object sender)
        {
            this.core = core;

        }

        public override AppPrimitives GetAppPrimitiveSupport()
        {
            return AppPrimitives.Group;
        }

        void core_PageHooks(HookEventArgs e)
        {
        }

        [Show(@"document/([a-zA-Z0-9\-\_\.\# ]+)", AppPrimitives.Group)]
        private void showDocument(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Document.Show(sender, new ShowPPageEventArgs(page, core.PagePathParts[1].Value));
            }
        }

        [Show(@"document/([a-zA-Z0-9\-\_\.\# ]+)/([a-zA-Z0-9\-\_\.\# ]+)", AppPrimitives.Group)]
        private void showDocumentAtRevision(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Document.Show(sender, new ShowPPageEventArgs(page, core.PagePathParts[1].Value + "/" + core.PagePathParts[2].Value));
            }
        }

        [Show(@"vendor/(\d+)", AppPrimitives.Group)]
        private void showVendor(Core core, object sender)
        {
            if (sender is PPage)
            {
                PPage page = (PPage)sender;
                Vendor.Show(sender, new ShowPPageEventArgs(page, long.Parse(core.PagePathParts[1].Value)));
            }
        }
    }
}