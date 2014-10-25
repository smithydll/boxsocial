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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [AccountSubModule(AppPrimitives.Application, "applications", "settings")]
    public class AccountApplicationSettings : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("SETTINGS");
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountApplicationSettings(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountApplicationSettings_Load);
            this.Show += new EventHandler(AccountApplicationSettings_Show);
        }

        void AccountApplicationSettings_Load(object sender, EventArgs e)
        {

        }

        void AccountApplicationSettings_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_application_settings.html");

            Save(new EventHandler(AccountApplicationSettings_Save));

            if (Owner is ApplicationEntry)
            {
                ApplicationEntry ae = (ApplicationEntry)Owner;
                template.Parse("APPLICATION_TITLE", ae.Title);
                template.Parse("APPLICATION_DESCRIPTION", ae.Description);

                if (ae.ApplicationType == ApplicationType.OAuth)
                {
                    OAuthApplication oa = new OAuthApplication(core, ae);

                    template.Parse("IS_OAUTH", "TRUE");
                    template.Parse("OAUTH_CALLBACK", oa.CallbackUrl);
                }
            }
        }

        void AccountApplicationSettings_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string title = core.Http.Form["title"];
            string description = core.Http.Form["description"];
            string callbackUrl = core.Http.Form["callback"];

            if (Owner is ApplicationEntry)
            {
                ApplicationEntry ae = (ApplicationEntry)Owner;
                if (ae.ApplicationType == ApplicationType.OAuth)
                {
                    OAuthApplication oa = new OAuthApplication(core, ae);

                    oa.CallbackUrl = callbackUrl;

                    oa.Update();
                }

                ae.Title = title;
                ae.Description = description;

                ae.Update();

                SetInformation("Application settings saved.");
            }
        }
    }
}
