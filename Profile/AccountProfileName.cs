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

        /// <summary>
        /// Initializes a new instance of the AccountProfileName class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountProfileName(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountProfileName_Load);
            this.Show += new EventHandler(AccountProfileName_Show);
        }

        void AccountProfileName_Load(object sender, EventArgs e)
        {
        }

        void AccountProfileName_Show(object sender, EventArgs e)
        {
			Save(new EventHandler(AccountProfileName_Save));
			
            SetTemplate("account_name");

            LoggedInMember.LoadProfileInfo();

            template.Parse("DISPLAY_NAME", LoggedInMember.DisplayName);
            template.Parse("FIRST_NAME", LoggedInMember.Profile.FirstName);
            template.Parse("MIDDLE_NAME", LoggedInMember.Profile.MiddleName);
            template.Parse("LAST_NAME", LoggedInMember.Profile.LastName);
            template.Parse("SUFFIX", LoggedInMember.Profile.Suffix);

            string selected = " selected=\"selected\"";
            if (LoggedInMember.Profile.Title != null)
            {
                switch (LoggedInMember.Profile.Title.ToLower().TrimEnd(new char[] { '.' }))
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
            }
            else
            {
                template.Parse("TITLE_NONE", selected);
            }
        }

        void AccountProfileName_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.UserInfo.DisplayName = core.Http.Form["display"];

            LoggedInMember.UserInfo.Update();

            LoggedInMember.Profile.Title = core.Http.Form["title"];
            LoggedInMember.Profile.FirstName = core.Http.Form["firstname"];
            LoggedInMember.Profile.MiddleName = core.Http.Form["middlename"];
            LoggedInMember.Profile.LastName = core.Http.Form["lastname"];
            LoggedInMember.Profile.Suffix = core.Http.Form["suffix"];

            LoggedInMember.Profile.Update();

			SetInformation("Your name has been saved in the database.");
            //SetRedirectUri(BuildUri());
            //Display.ShowMessage("Name Saved", "Your name has been saved in the database.");
        }
    }
}
