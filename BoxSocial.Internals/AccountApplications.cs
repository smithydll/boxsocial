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
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [AccountSubModule("dashboard", "applications")]
    public class AccountApplications : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Applications";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        public AccountApplications()
        {
            this.Load += new EventHandler(AccountApplications_Load);
            this.Show += new EventHandler(AccountApplications_Show);
        }

        void AccountApplications_Load(object sender, EventArgs e)
        {
            AddModeHandler("settings", new EventHandler(ApplicationSettings));
            AddModeHandler("install", new EventHandler(ApplicationInstall));
            AddModeHandler("uninstall", new EventHandler(ApplicationUninstall));
        }

        private void AccountApplications_Show(object sender, EventArgs e)
        {
            // TODO: SetTemplate("account_applications");
            template.SetTemplate("account_applications.html");

            List<ApplicationEntry> applications = BoxSocial.Internals.Application.GetApplications(core, session.LoggedInMember);

            foreach (ApplicationEntry ae in applications)
            {
                VariableCollection applicationsVariableCollection = template.CreateChild("application_list");

                applicationsVariableCollection.Parse("NAME", ae.Title);
                Dictionary<string, string> args = new Dictionary<string, string>();
                args.Add("mode", "settings");
                args.Add("id", ae.ApplicationId.ToString());
                applicationsVariableCollection.Parse("U_SETTINGS", BuildUri(args));

                if (ae.AssemblyName != "Profile" && ae.AssemblyName != "GuestBook" && !ae.IsPrimitive)
                {
                    args = new Dictionary<string, string>();
                    args.Add("mode", "uninstall");
                    args.Add("id", ae.ApplicationId.ToString());
                    applicationsVariableCollection.Parse("U_UNINSTALL", BuildUri(args));
                }
            }
        }

        public void ApplicationSettings(object sender, EventArgs e)
        {
            template.SetTemplate("account_application_settings.html");
            long id = Functions.RequestLong("id", 0);

            if (id == 0)
            {
                Display.ShowMessage("Error", "Error!");
                return;
            }

            // TODO: fill this in

            Save(new EventHandler(ApplicationSettingsSave));
        }

        private void ApplicationSettingsSave(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = Functions.FormLong("id", 0);

            if (id == 0)
            {
                Display.ShowMessage("Error", "Error!");
                return;
            }

            UpdateQuery uquery = new UpdateQuery("primitive_apps");
            uquery.AddField("app_access", Functions.GetPermission());
            uquery.AddCondition("item_id", core.LoggedInMemberId);
            uquery.AddCondition("item_type", "USER");
            uquery.AddCondition("application_id", id);

            db.Query(uquery);

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Settings updated", "The settings for this application have been successfully updated.");
        }

        public void ApplicationInstall(object sender, EventArgs e)
        {
        }

        public void ApplicationUninstall(object sender, EventArgs e)
        {
        }
        
    }
}
