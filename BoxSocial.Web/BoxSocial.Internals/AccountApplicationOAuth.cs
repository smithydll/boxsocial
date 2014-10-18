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

namespace BoxSocial.FrontEnd
{
    [AccountSubModule(AppPrimitives.Application, "dashboard", "oauth")]
    public class AccountApplicationOAuth : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("OAUTH_KEYS");
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountOverview class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountApplicationOAuth(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountApplicationOAuth_Load);
            this.Show += new EventHandler(AccountApplicationOAuth_Show);
        }

        void AccountApplicationOAuth_Load(object sender, EventArgs e)
        {
            AddModeHandler("generate-keys", new ModuleModeHandler(AccountApplicationOAuth_GenerateKeys));
        }

        void AccountApplicationOAuth_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_application_oauth.html");

            template.Parse("U_REGENERATE_KEYS", core.Hyperlink.AppendSid(BuildUri("oauth", "generate-keys"), true));

            if (Owner is ApplicationEntry)
            {
                ApplicationEntry ae = (ApplicationEntry)Owner;
                if (ae.ApplicationType == ApplicationType.OAuth)
                {
                    OAuthApplication oa = new OAuthApplication(core, ae);
                    template.Parse("IS_OAUTH", "TRUE");
                    template.Parse("CONSUMER_KEY", oa.ApiKey);
                    template.Parse("CONSUMER_SECRET", oa.ApiSecret);
                }
            }
        }

        void AccountApplicationOAuth_GenerateKeys(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string newKey = OAuth.GeneratePublic();
            string newSecret = OAuth.GenerateSecret();

            if (Owner is ApplicationEntry)
            {
                ApplicationEntry ae = (ApplicationEntry)Owner;
                if (ae.ApplicationType == ApplicationType.OAuth)
                {
                    OAuthApplication oa = new OAuthApplication(core, ae);

                    oa.ApiKey = newKey;
                    oa.ApiSecret = newSecret;

                    oa.Update();
                }
            }

            AccountApplicationOAuth_Show(sender, e);
        }
    }
}
