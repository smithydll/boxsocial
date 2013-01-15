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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [AccountSubModule("dashboard", "preferences")]
    public class AccountPreferences : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Preferences";
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
        /// Initializes a new instance of the AccountPreferences class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountPreferences(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountPreferences_Load);
            this.Show += new EventHandler(AccountPreferences_Show);
        }

        void AccountPreferences_Load(object sender, EventArgs e)
        {
        }

        void AccountPreferences_Show(object sender, EventArgs e)
        {
			Save(new EventHandler(AccountPreferences_Save));
			
            //User loggedInMember = (User)loggedInMember;
            template.SetTemplate("account_preferences.html");

            TextBox analyticsCodeTextBox = new TextBox("analytics-code");
            analyticsCodeTextBox.Value = LoggedInMember.UserInfo.AnalyticsCode;

            string radioChecked = " checked=\"checked\"";

            if (LoggedInMember.UserInfo.EmailNotifications)
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.ShowCustomStyles)
            {
                template.Parse("S_SHOW_STYLES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_SHOW_STYLES_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.BbcodeShowImages)
            {
                template.Parse("S_DISPLAY_IMAGES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_IMAGES_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.BbcodeShowFlash)
            {
                template.Parse("S_DISPLAY_FLASH_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_FLASH_NO", radioChecked);
            }

            if (LoggedInMember.UserInfo.BbcodeShowVideos)
            {
                template.Parse("S_DISPLAY_VIDEOS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_VIDEOS_NO", radioChecked);
            }

            template.Parse("S_ANALYTICS_CODE", analyticsCodeTextBox);

            DataTable pagesTable = db.Query(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE page_item_id = {0} AND page_item_type_id = {1} ORDER BY page_order ASC;",
                LoggedInMember.UserId, ItemKey.GetTypeId(typeof(User))));

            SelectBox pagesSelectBox = new SelectBox("homepage");

            foreach (DataRow pageRow in pagesTable.Rows)
            {
                if (string.IsNullOrEmpty((string)pageRow["page_parent_path"]))
                {
                    pagesSelectBox.Add(new SelectBoxItem("/" + (string)pageRow["page_slug"], "/" + (string)pageRow["page_slug"]));
                }
                else
                {
                    pagesSelectBox.Add(new SelectBoxItem("/" + (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"], "/" + (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"]));
                }
            }

            SelectBox timezoneSelectBox = UnixTime.BuildTimeZoneSelectBox("timezone");
            timezoneSelectBox.SelectedKey = LoggedInMember.UserInfo.TimeZoneCode.ToString();

            pagesSelectBox.SelectedKey = LoggedInMember.UserInfo.ProfileHomepage;
            template.Parse("S_HOMEPAGE", pagesSelectBox);
            template.Parse("S_TIMEZONE", timezoneSelectBox);
            //core.Display.ParseTimeZoneBox(template, "S_TIMEZONE", LoggedInMember.TimeZoneCode.ToString());

        }

        void AccountPreferences_Save(object sender, EventArgs e)
        {
            //User loggedInMember = (User)loggedInMember;
            AuthoriseRequestSid();

            bool displayImages = true;
            bool displayFlash = true;
            bool displayVideos = true;
            bool displayAudio = true;
            bool showCustomStyles = false;
            bool emailNotifications = true;
            BbcodeOptions showBbcode = BbcodeOptions.None;
            string homepage = "/profile";
            string analyticsCode = string.Empty;
            ushort timeZoneCode = 30;

            try
            {
                displayImages = (int.Parse(core.Http.Form["display-images"]) == 1);
                displayFlash = (int.Parse(core.Http.Form["display-flash"]) == 1);
                displayVideos = (int.Parse(core.Http.Form["display-videos"]) == 1);
                // TODO: displayAudio
                showCustomStyles = (int.Parse(core.Http.Form["show-styles"]) == 1);
                emailNotifications = (int.Parse(core.Http.Form["email-notifications"]) == 1);
                homepage = core.Http.Form["homepage"];
                analyticsCode = core.Http.Form["analytics-code"];
                timeZoneCode = ushort.Parse(core.Http.Form["timezone"]);
            }
            catch
            {
            }

            if (homepage != "/profile" && homepage != "/blog")
            {
                try
                {
                    Page thisPage = new Page(core, LoggedInMember, homepage.TrimStart(new char[] { '/' }));
                }
                catch (PageNotFoundException)
                {
                    homepage = "/profile";
                }
            }

            if (displayImages)
            {
                showBbcode |= BbcodeOptions.ShowImages;
            }
            if (displayFlash)
            {
                showBbcode |= BbcodeOptions.ShowFlash;
            }
            if (displayVideos)
            {
                showBbcode |= BbcodeOptions.ShowVideo;
            }
            if (displayAudio)
            {
                showBbcode |= BbcodeOptions.ShowAudio;
            }

            LoggedInMember.UserInfo.ShowCustomStyles = showCustomStyles;
            LoggedInMember.UserInfo.EmailNotifications = emailNotifications;
            LoggedInMember.UserInfo.SetUserBbcodeOptions = showBbcode;
            LoggedInMember.UserInfo.ProfileHomepage = homepage;
            LoggedInMember.UserInfo.TimeZoneCode = timeZoneCode;
            LoggedInMember.UserInfo.AnalyticsCode = analyticsCode;

            LoggedInMember.UserInfo.Update();

            //SetRedirectUri(BuildUri());
            //Display.ShowMessage("Preferences Saved", "Your preferences have been saved in the database.");
			SetInformation("Your preferences have been saved in the database.");
        }
    }
}
