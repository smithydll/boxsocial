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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
//using BoxSocial.Groups;

namespace BoxSocial
{
    [AccountModule("dashboard")]
    public class AccountDashboard : AccountModule
    {

        public AccountDashboard(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(Overview);
            // TODO: subscription management
            RegisterSubModule += new RegisterSubModuleHandler(Preferences);
            RegisterSubModule += new RegisterSubModuleHandler(Applications);
            RegisterSubModule += new RegisterSubModuleHandler(Password);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }


        public override string Name
        {
            get
            {
                return "Dashboard";
            }
        }

        /*public override string Key
        {
            get
            {
                return "dashboard";
            }
        }*/

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        private void Overview(string submodule)
        {
            subModules.Add("overview", "Overview");
            if (submodule != "overview" && !string.IsNullOrEmpty(submodule)) return;

            template.SetTemplate("account_landing.html");

            /*DataTable friendNotificationsTable = db.Query(string.Format("SELECT notification_time_ut, {0} FROM friend_notifications fn INNER JOIN user_relations ur ON ur.relation_id = fn.relation_id INNER JOIN user_info ui ON ur.relation_me = ui.user_id WHERE ur.relation_you = {1} ORDER BY notification_time_ut DESC",
                Member.USER_INFO_FIELDS, loggedInMember.UserId));*/

            /*DataTable groupInvitesTable = db.Query(string.Format("SELECT gv.invite_date_ut, {0}, {1} FROM group_invites gv INNER JOIN user_info ui ON gv.inviter_id = ui.user_id INNER JOIN group_info gi ON gv.group_id = gi.group_id WHERE gv.user_id = {2} ORDER BY gv.invite_date_ut DESC",
                Member.USER_INFO_FIELDS, UserGroup.GROUP_INFO_FIELDS, loggedInMember.UserId));*/

            /*if (friendNotificationsTable.Rows.Count > 0)
            {
                template.ParseVariables("IS_NOTIFICATIONS", "TRUE");
            }*/

            /*if (groupInvitesTable.Rows.Count > 0)
            {
                template.ParseVariables("IS_INVITATIONS", "TRUE");
            }*/

            /*for (int i = 0; i < friendNotificationsTable.Rows.Count; i++)
            {
                VariableCollection friendNotificationsVariableCollection = template.CreateChild("notifications_list");

                Member friend = new Member(db, friendNotificationsTable.Rows[i], false);

                friendNotificationsVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(tz.MysqlToString(friendNotificationsTable.Rows[i]["notification_time_ut"])));
                friendNotificationsVariableCollection.ParseVariables("ACTION", "New Friend");
                friendNotificationsVariableCollection.ParseVariables("DESCRIPTION", string.Format("<a href=\"{2}\">{0}</a> Added you as a friend... <a href=\"{1}\">Add as friend</a>",
                    friend.UserName, HttpUtility.HtmlEncode(ZzUri.BuildAddFriendUri(friend.UserId)), HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(friend))));
            }/*

            /*for (int i = 0; i < groupInvitesTable.Rows.Count; i++)
            {
                VariableCollection groupInvitationsVariableCollection = template.CreateChild("invitations_list");

                Member friend = new Member(db, groupInvitesTable.Rows[i], false);
                UserGroup thisGroup = new UserGroup(db, groupInvitesTable.Rows[i]);

                groupInvitationsVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(tz.MysqlToString(groupInvitesTable.Rows[i]["invite_date_ut"])));
                groupInvitationsVariableCollection.ParseVariables("ACTION", "Group Invite");
                groupInvitationsVariableCollection.ParseVariables("DESCRIPTION", string.Format("<a href=\"{2}\">{0}</a> Added invited you to '{3}' ... <a href=\"{1}\">Join Group</a>",
                    friend.UserName, HttpUtility.HtmlEncode(thisGroup.JoinUri), HttpUtility.HtmlEncode(ZzUri.BuildProfileUri(friend)), HttpUtility.HtmlEncode(thisGroup.DisplayName)));
            }*/

            List<Notification> notifications = Notification.GetRecentNotifications(core);
            List<long> ids = new List<long>();

            if (notifications.Count > 0)
            {
                template.ParseVariables("IS_NOTIFICATIONS", "TRUE");
            }

            core.LoadUserProfile(core.LoggedInMemberId);
            foreach (Notification notification in notifications)
            {
                VariableCollection notificationVariableCollection = template.CreateChild("notifications_list");

                notificationVariableCollection.ParseVariables("DATE", HttpUtility.HtmlEncode(tz.DateTimeToString(notification.GetTime(tz))));
                notificationVariableCollection.ParseVariables("ACTION", Bbcode.Parse(HttpUtility.HtmlEncode(notification.Title)));
                notificationVariableCollection.ParseVariables("DESCRIPTION", Bbcode.Parse(HttpUtility.HtmlEncode(notification.Body), core.session.LoggedInMember, core.UserProfiles[core.LoggedInMemberId]));

                if (notification.IsSeen)
                {
                    notificationVariableCollection.ParseVariables("SEEN", "TRUE");
                }

                ids.Add(notification.NotificationId);
            }

            if (ids.Count > 0)
            {
                UpdateQuery uQuery = new UpdateQuery("notifications");
                uQuery.AddField("notification_seen", true);
                uQuery.AddCondition("notification_id", ConditionEquality.In, ids);

                db.Query(uQuery);
            }
        }

        public void Applications(string submodule)
        {
            subModules.Add("applications", "Applications");
            if (submodule != "applications") return;

            if (Request["mode"] == "settings")
            {
                ApplicationSettings();
                return;
            }

            if (Request["mode"] == "install")
            {
                ApplicationInstall();
                return;
            }

            if (Request["mode"] == "uninstall")
            {
                ApplicationUninstall();
                return;
            }

            template.SetTemplate("account_applications.html");

            List<ApplicationEntry> applications = BoxSocial.Internals.Application.GetApplications(core, session.LoggedInMember);

            foreach (ApplicationEntry ae in applications)
            {
                VariableCollection applicationsVariableCollection = template.CreateChild("application_list");

                applicationsVariableCollection.ParseVariables("NAME", ae.Title);
                applicationsVariableCollection.ParseVariables("U_SETTINGS", HttpUtility.HtmlEncode(Linker.AppendSid(string.Format("/account/dashboard/applications?mode=settings&id={0}",
                    ae.ApplicationId))));

                if (ae.AssemblyName != "Profile" && ae.AssemblyName != "GuestBook" && !ae.IsPrimitive)
                {
                    applicationsVariableCollection.ParseVariables("U_UNINSTALL", HttpUtility.HtmlEncode(Linker.AppendSid(string.Format("/account/dashboard/applications?mode=uninstall&id={0}",
                    ae.ApplicationId), true)));
                }
            }
        }

        public void ApplicationInstall()
        {
            AuthoriseRequestSid();

            int id;

            try
            {
                id = int.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "Error!");
                return;
            }

            /*try
            {*/
            ApplicationEntry ae = new ApplicationEntry(core, null, id);
                ae.Install(core, loggedInMember);
            /*}
            catch
            {
            }*/

            /*db.UpdateQuery(string.Format(@"INSERT INTO primitive_apps (item_id, item_type, application_id, app_access) VALUES ({0}, '{1}', {2}, {3});",
                loggedInMember.UserId, Mysql.Escape("USER"), id, 0x1111));*/

            template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Linker.AppendSid("/account/dashboard/applications")));
            Display.ShowMessage("Application Installed", "The application has been installed to your profile.");
        }

        public void ApplicationUninstall()
        {
            AuthoriseRequestSid();

            int id;

            try
            {
                id = int.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "Error!");
                return;
            }

            try
            {
                ApplicationEntry ae = new ApplicationEntry(core, null, id);
                ae.Uninstall(core, loggedInMember);
            }
            catch
            {
            }

            /*db.UpdateQuery(string.Format(@"DELETE FROM primitive_apps WHERE item_id = {0} AND item_type = '{1}' AND application_id = {2}",
                loggedInMember.UserId, Mysql.Escape("USER"), id));*/

            template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Linker.AppendSid("/account/dashboard/applications")));
            Display.ShowMessage("Application Uninstalled", "The application has been uninstalled from your profile.");
        }

        public void ApplicationSettings()
        {
            template.SetTemplate("account_application_settings.html");
            int id;

            if (Request.Form["save"] != null)
            {
                ApplicationSettingsSave();
                return;
            }

            try
            {
                id = int.Parse(Request.QueryString["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "Error!");
                return;
            }

            DataTable applicationTable = db.Query(string.Format(@"SELECT {3}, {4}
                FROM primitive_apps pa
                INNER JOIN applications ap ON ap.application_id = pa.application_id
                WHERE pa.application_id = {0}
                    AND pa.item_id = {1}
                    AND pa.item_type = '{2}';",
                id, loggedInMember.UserId, Mysql.Escape("USER"), ApplicationEntry.APPLICATION_FIELDS, ApplicationEntry.USER_APPLICATION_FIELDS));

            if (applicationTable.Rows.Count == 1)
            {
                ApplicationEntry ae = new ApplicationEntry(core, loggedInMember, applicationTable.Rows[0]);

                List<string> applicationPermissions = new List<string>();
                applicationPermissions.Add("Can Access");

                template.ParseVariables("APPLICATION_NAME", HttpUtility.HtmlEncode(ae.Title));
                template.ParseVariables("S_FORM_ACTION", HttpUtility.HtmlEncode(Linker.AppendSid("/account/", true)));
                template.ParseVariables("S_GAPPLICATION_PERMS", Functions.BuildPermissionsBox(ae.Permissions, applicationPermissions));
                template.ParseVariables("S_APPLICATION_ID", HttpUtility.HtmlEncode(ae.ApplicationId.ToString()));
            }
            else
            {
                Display.ShowMessage("Error", "Error!");
            }
        }

        private void ApplicationSettingsSave()
        {
            AuthoriseRequestSid();

            int id;

            try
            {
                id = int.Parse(Request.Form["id"]);
            }
            catch
            {
                Display.ShowMessage("Error", "Error!");
                return;
            }

            db.UpdateQuery(string.Format(@"UPDATE primitive_apps SET app_access = {3} WHERE item_id = {0} AND item_type = '{1}' AND application_id = {2}",
                loggedInMember.UserId, Mysql.Escape("USER"), id, Functions.GetPermission()));

            template.ParseVariables("REDIRECT_URI", HttpUtility.HtmlEncode(Linker.AppendSid("/account/dashboard/applications")));
            Display.ShowMessage("Settings updated", "The settings for this application have been successfully updated.");
        }

        public void Preferences(string submodule)
        {
            subModules.Add("preferences", "Preferences");
            if (submodule != "preferences") return;

            if (Request.Form["save"] != null)
            {
                PreferencesSave();
                return;
            }

            template.SetTemplate("account_preferences.html");

            string radioChecked = " checked=\"checked\"";

            if (loggedInMember.EmailNotifications)
            {
                template.ParseVariables("S_EMAIL_NOTIFICATIONS_YES", radioChecked);
            }
            else
            {
                template.ParseVariables("S_EMAIL_NOTIFICATIONS_NO", radioChecked);
            }

            if (loggedInMember.ShowCustomStyles)
            {
                template.ParseVariables("S_SHOW_STYLES_YES", radioChecked);
            }
            else
            {
                template.ParseVariables("S_SHOW_STYLES_NO", radioChecked);
            }

            if (loggedInMember.BbcodeShowImages)
            {
                template.ParseVariables("S_DISPLAY_IMAGES_YES", radioChecked);
            }
            else
            {
                template.ParseVariables("S_DISPLAY_IMAGES_NO", radioChecked);
            }

            if (loggedInMember.BbcodeShowFlash)
            {
                template.ParseVariables("S_DISPLAY_FLASH_YES", radioChecked);
            }
            else
            {
                template.ParseVariables("S_DISPLAY_FLASH_NO", radioChecked);
            }

            if (loggedInMember.BbcodeShowVideos)
            {
                template.ParseVariables("S_DISPLAY_VIDEOS_YES", radioChecked);
            }
            else
            {
                template.ParseVariables("S_DISPLAY_VIDEOS_NO", radioChecked);
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

            template.ParseVariables("S_HOMEPAGE", Functions.BuildSelectBox("homepage", pages, loggedInMember.ProfileHomepage.ToString()));
            template.ParseVariables("S_TIMEZONE", UnixTime.BuildTimeZoneSelectBox(loggedInMember.TimeZoneCode.ToString()));
        }

        public void PreferencesSave()
        {
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
                string[] paths = homepage.Split('/');
                DataTable pageTable = db.Query(string.Format("SELECT page_id FROM user_pages WHERE page_slug = '{1}' AND page_parent_path = '{2}' AND user_id = {0};",
                    loggedInMember.UserId, Mysql.Escape(homepage.Remove(homepage.Length - paths[paths.GetUpperBound(0)].Length).TrimEnd('/'))));

                if (pageTable.Rows.Count == 0)
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

            db.UpdateQuery(string.Format("UPDATE user_info SET user_show_bbcode = {1}, user_show_custom_styles = {2}, user_email_notifications = {3}, user_home_page = '{4}', user_time_zone = {5} WHERE user_id = {0};",
                loggedInMember.UserId, (byte)showBbcode, showCustomStyles, emailNotifications, Mysql.Escape(homepage), timeZoneCode));

            template.ParseVariables("REDIRECT_URI", "/account/?module=&sub=preferences");
            Display.ShowMessage("Preferences Saved", "Your preferences have been saved in the database.<br /><a href=\"/account/?module=&sub=preferences\">Return</a>");
        }

        public void Password(string submodule)
        {
            subModules.Add("password", "Change Password");
            if (submodule != "password") return;

            template.SetTemplate("account_password.html");

            template.ParseVariables("S_CHANGE_PASSWORD", Linker.AppendSid("/account", true));

            string password = Request.Form["old-password"];

            if (password != null && Request.Form["save"] != null)
            {
                password = Member.HashPassword(password);

                SelectQuery query = new SelectQuery("user_keys uk");
                query.AddFields("uk.user_name, uk.user_id");
                query.AddJoin(JoinTypes.Inner, "user_info ui", "ui.user_id", "uk.user_id");
                query.AddCondition("uk.user_id", core.LoggedInMemberId);
                query.AddCondition("ui.user_password", password);

                DataTable userTable = db.Query(query);
                if (userTable.Rows.Count != 1)
                {
                    SetError("The old password you entered does not match your old password, make sure you have entered your old password correctly.");
                    return;
                }
                else if (Request.Form["new-password"] != Request.Form["confirm-password"])
                {
                    SetError("The passwords you entered do not match, make sure you have entered your desired password correctly.");
                    return;
                }
                else if (((string)Request.Form["new-password"]).Length < 6)
                {
                    SetError("The password you entered is too short. Please choose a strong password of 6 characters or more.");
                    return;
                }
            }
            
            if (Request.Form["save"] != null)
            {
                PasswordSave();
                return;
            }
        }

        public void PasswordSave()
        {
            string password = Member.HashPassword(Request.Form["old-password"]);

            AuthoriseRequestSid();

            UpdateQuery uquery = new UpdateQuery("user_info");
            uquery.AddField("user_password", Member.HashPassword(Request.Form["new-password"]));
            uquery.AddCondition("user_id", core.LoggedInMemberId);

            long rowsChanged = db.Query(uquery);

            if (rowsChanged == 1)
            {
                Display.ShowMessage("Changed Password", "Have successfully changed your password. Keep your password safe and do not share it with anyone.");
            }
            else
            {
                Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }
        }
    }
}
