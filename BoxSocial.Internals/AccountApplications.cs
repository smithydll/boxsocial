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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [AccountSubModule(AppPrimitives.Any, "dashboard", "applications")]
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
            AddModeHandler("settings", new ModuleModeHandler(ApplicationSettings));
            AddSaveHandler("settings", new EventHandler(ApplicationSettingsSave));
            AddModeHandler("install", new ModuleModeHandler(ApplicationInstall));
            AddModeHandler("uninstall", new ModuleModeHandler(ApplicationUninstall));
        }

        private void AccountApplications_Show(object sender, EventArgs e)
        {
            // TODO: SetTemplate("account_applications");
            template.SetTemplate("account_applications.html");

            List<ApplicationEntry> applications = BoxSocial.Internals.Application.GetApplications(core, Owner);

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

            if (Owner == LoggedInMember)
            {
                template.Parse("U_APPLICATIONS", core.Uri.AppendCoreSid("/applications"));
            }
            else
            {
                template.Parse("U_APPLICATIONS", core.Uri.AppendCoreSid(string.Format("/applications?type={0}&id={1}",
                    Owner.TypeId, Owner.Id)));
            }
        }

        public void ApplicationSettings(object sender, EventArgs e)
        {
            template.SetTemplate("account_application_settings.html");

            long id = core.Functions.RequestLong("id", 0);

            if (id == 0)
            {
                core.Display.ShowMessage("Error", "Error!");
                return;
            }

            SelectQuery query = new SelectQuery("primitive_apps");
            query.AddFields(ApplicationEntry.GetFieldsPrefixed(typeof(ApplicationEntry)));
            query.AddFields(PrimitiveApplicationInfo.GetFieldsPrefixed(typeof(PrimitiveApplicationInfo)));
            query.AddJoin(JoinTypes.Inner, new DataField("primitive_apps", "application_id"), new DataField("applications", "application_id"));
            query.AddCondition("primitive_apps.application_id", id);
            query.AddCondition("item_id", Owner.Id);
            query.AddCondition("item_type_id", Owner.TypeId);

            DataTable applicationTable = db.Query(query);

            if (applicationTable.Rows.Count == 1)
            {
                ApplicationEntry ae = new ApplicationEntry(core, Owner, applicationTable.Rows[0]);

                List<string> applicationPermissions = new List<string>();
                applicationPermissions.Add("Can Access");

                template.Parse("APPLICATION_NAME", ae.Title);
                //core.Display.ParsePermissionsBox(template, "S_GAPPLICATION_PERMS", ae.Permissions, applicationPermissions);
                template.Parse("S_APPLICATION_ID", ae.ApplicationId.ToString());
            }
            else
            {
                core.Display.ShowMessage("Error", "Error!");
            }
        }

        private void ApplicationSettingsSave(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = core.Functions.FormLong("id", 0);

            if (id == 0)
            {
                DisplayGenericError();
                return;
            }

            UpdateQuery uquery = new UpdateQuery("primitive_apps");
            uquery.AddCondition("item_id", Owner.Id);
            uquery.AddCondition("item_type_id", Owner.TypeId);
            uquery.AddCondition("application_id", id);

            db.Query(uquery);

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Settings updated", "The settings for this application have been successfully updated.");
        }

        public void ApplicationInstall(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            int id;

            try
            {
                id = int.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Error", "Error!");
                return;
            }

            /*try
            {*/
            ApplicationEntry ae = new ApplicationEntry(core, null, id);
            ae.Install(core, core.Session.LoggedInMember, Owner);
            /*}
            catch
            {
            }*/

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Application Installed", "The application has been installed to your profile.");
        }

        public void ApplicationUninstall(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            int id;

            try
            {
                id = int.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Error", "Error!");
                return;
            }

            try
            {
                ApplicationEntry ae = new ApplicationEntry(core, null, id);
                ae.Uninstall(core, Owner);
            }
            catch
            {
            }

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Application Uninstalled", "The application has been uninstalled from your profile.");
        }
        
    }
}
