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
            //User loggedInMember = (User)loggedInMember;
            template.SetTemplate("account_preferences.html");

            string radioChecked = " checked=\"checked\"";

            if (loggedInMember.EmailNotifications)
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_EMAIL_NOTIFICATIONS_NO", radioChecked);
            }

            if (loggedInMember.ShowCustomStyles)
            {
                template.Parse("S_SHOW_STYLES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_SHOW_STYLES_NO", radioChecked);
            }

            if (loggedInMember.BbcodeShowImages)
            {
                template.Parse("S_DISPLAY_IMAGES_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_IMAGES_NO", radioChecked);
            }

            if (loggedInMember.BbcodeShowFlash)
            {
                template.Parse("S_DISPLAY_FLASH_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_FLASH_NO", radioChecked);
            }

            if (loggedInMember.BbcodeShowVideos)
            {
                template.Parse("S_DISPLAY_VIDEOS_YES", radioChecked);
            }
            else
            {
                template.Parse("S_DISPLAY_VIDEOS_NO", radioChecked);
            }

            DataTable pagesTable = db.Query(string.Format("SELECT page_id, page_slug, page_parent_path FROM user_pages WHERE user_id = {0} ORDER BY page_order ASC;",
                loggedInMember.UserId));

            Dictionary<string, string> pages = new Dictionary<string, string>();
            List<string> disabledItems = new List<string>();
            pages.Add("/profile", "My Profile");
            pages.Add("/blog", "My Blog");

            foreach (DataRow pageRow in pagesTable.Rows)
            {
                if (string.IsNullOrEmpty((string)pageRow["page_parent_path"]))
                {
                    pages.Add((string)pageRow["page_slug"], (string)pageRow["page_slug"] + "/");
                }
                else
                {
                    pages.Add((string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"], (string)pageRow["page_parent_path"] + "/" + (string)pageRow["page_slug"] + "/");
                }
            }

            Display.ParseSelectBox(template, "S_HOMEPAGE", "homepage", pages, loggedInMember.ProfileHomepage.ToString());
            Display.ParseTimeZoneBox(template, "S_TIMEZONE", loggedInMember.TimeZoneCode.ToString());

            Save(new EventHandler(AccountPreferences_Save));
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
                    Page thisPage = new Page(core, (User)loggedInMember, homepage.TrimStart(new char[] { '/' }));
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

            loggedInMember.Info.ShowCustomStyles = showCustomStyles;
            loggedInMember.Info.EmailNotifications = emailNotifications;
            loggedInMember.Info.SetUserBbcodeOptions = showBbcode;
            loggedInMember.Info.ProfileHomepage = homepage;
            loggedInMember.Info.TimeZoneCode = timeZoneCode;

            loggedInMember.Info.Update();

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Preferences Saved", "Your preferences have been saved in the database.");
        }
    }
}
