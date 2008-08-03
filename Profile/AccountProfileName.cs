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

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "name")]
    public class AccountProfileName : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "My Name";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountProfileName()
        {
            this.Load += new EventHandler(AccountProfileName_Load);
            this.Show += new EventHandler(AccountProfileName_Show);
        }

        void AccountProfileName_Load(object sender, EventArgs e)
        {
        }

        void AccountProfileName_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_name.html");

            LoggedInMember.LoadProfileInfo();

            template.Parse("DISPLAY_NAME", LoggedInMember.DisplayName);
            template.Parse("FIRST_NAME", LoggedInMember.FirstName);
            template.Parse("MIDDLE_NAME", LoggedInMember.MiddleName);
            template.Parse("LAST_NAME", LoggedInMember.LastName);
            template.Parse("SUFFIX", LoggedInMember.Suffix);

            string selected = " selected=\"selected\"";
            switch (LoggedInMember.Title.ToLower().TrimEnd(new char[] { '.' }))
            {
                default:
                    template.Parse("TITLE_NONE", selected);
                    break;
                case "master":
                    template.Parse("TITLE_MASTER", selected);
                    break;
                case "mr":
                    template.Parse("TITLE_MR", selected);
                    break;
                case "miss":
                    template.Parse("TITLE_MISS", selected);
                    break;
                case "ms":
                    template.Parse("TITLE_MS", selected);
                    break;
                case "mrs":
                    template.Parse("TITLE_MRS", selected);
                    break;
                case "fr":
                    template.Parse("TITLE_FR", selected);
                    break;
                case "sr":
                    template.Parse("TITLE_SR", selected);
                    break;
                case "prof":
                    template.Parse("TITLE_PROF", selected);
                    break;
                case "lord":
                    template.Parse("TITLE_LORD", selected);
                    break;
            }

            Save(new EventHandler(AccountProfileName_Save));
        }

        void AccountProfileName_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.Info.DisplayName = Request.Form["display"];

            LoggedInMember.Info.Update();

            LoggedInMember.Profile.Title = Request.Form["title"];
            LoggedInMember.Profile.FirstName = Request.Form["firstname"];
            LoggedInMember.Profile.MiddleName = Request.Form["middlename"];
            LoggedInMember.Profile.LastName = Request.Form["lastname"];
            LoggedInMember.Profile.Suffix = Request.Form["suffix"];

            LoggedInMember.Profile.Update();

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Name Saved", "Your name has been saved in the database.");
        }
    }
}
