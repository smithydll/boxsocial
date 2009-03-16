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

        public AccountPreferences()
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

            string radioChecked = " checked=\"checked\"";

            if (LoggedInMember.EmailNotifications)
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_NO", radioChecked);
            }

            if (LoggedInMember.ShowCustomStyles)
            {
                template.Parse("S_SHOW_STYLES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_SHOW_STYLES_NO", radioChecked);
            }

            if (LoggedInMember.BbcodeShowImages)
            {
                template.Parse("S_DISPLAY_IMAGES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_IMAGES_NO", radioChecked);
            }

            if (LoggedInMember.BbcodeShowFlash)
            {
                template.Parse("S_DISPLAY_FLASH_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_FLASH_NO", radioChecked);
            }

            if (LoggedInMember.BbcodeShowVideos)
            {
                template.Parse("S_DISPLAY_VIDEOS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_VIDEOS_NO", radioChecked);
            }

            DataTable pagesTable = db.Query(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE page_item_id = {0} AND page_item_type = '{1}' ORDER BY page_order ASC;",
                LoggedInMember.UserId, Mysql.Escape(LoggedInMember.Type)));

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

            pagesSelectBox.SelectedKey = LoggedInMember.ProfileHomepage;
            template.Parse("S_HOMEPAGE", pagesSelectBox);
            Display.ParseTimeZoneBox(template, "S_TIMEZONE", LoggedInMember.TimeZoneCode.ToString());
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
            ushort timeZoneCode = 30;

            try
            {
                displayImages = (int.Parse(Request.Form["display-images"]) == 1);
                displayFlash = (int.Parse(Request.Form["display-flash"]) == 1);
                displayVideos = (int.Parse(Request.Form["display-videos"]) == 1);
                // TODO: displayAudio
                showCustomStyles = (int.Parse(Request.Form["show-styles"]) == 1);
                emailNotifications = (int.Parse(Request.Form["email-notifications"]) == 1);
                homepage = Request.Form["homepage"];
                timeZoneCode = ushort.Parse(Request.Form["timezone"]);
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

            LoggedInMember.Info.ShowCustomStyles = showCustomStyles;
            LoggedInMember.Info.EmailNotifications = emailNotifications;
            LoggedInMember.Info.SetUserBbcodeOptions = showBbcode;
            LoggedInMember.Info.ProfileHomepage = homepage;
            LoggedInMember.Info.TimeZoneCode = timeZoneCode;

            LoggedInMember.Info.Update();

            //SetRedirectUri(BuildUri());
            //Display.ShowMessage("Preferences Saved", "Your preferences have been saved in the database.");
			SetInformation("Your preferences have been saved in the database.");
        }
    }
}
