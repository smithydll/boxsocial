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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Groups;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class register : TPage
    {
        public register()
            : base("register.html")
        {
            this.Load += new EventHandler(Page_Load);
        }

        private void prepareNewCaptcha()
        {
            // prepare the captcha
            string captchaString = Captcha.GenerateCaptchaString();
            //string captchaSecurityToken = captcha.GenerateCaptchaSecurityToken();

            // delete all existing for this session
            // captcha is a use once thing, destroy all for this session
            Confirmation.ClearStale(core, session.SessionId, 1);

            // create a new confimation code
            Confirmation confirm = Confirmation.Create(core, session.SessionId, captchaString, 1);

            template.Parse("U_CAPTCHA", core.Hyperlink.AppendSid("/captcha.aspx?secureid=" + confirm.ConfirmId.ToString(), true));
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (session.IsLoggedIn)
            {
                // redirect to the homepage if we are already logged in
                Response.Redirect("/");
            }

            template.Parse("IS_CONTENT", "FALSE");
            template.Parse("S_POST", core.Hyperlink.AppendSid("/register/", true));
            
            string mode = Request.QueryString["mode"];

            if (mode == "optout")
            {
                string emailKey = Request.QueryString["key"];

                if (emailKey.Length == 32)
                {
                    long rowsChanged = db.UpdateQuery(string.Format("UPDATE invite_keys SET invite_allow = 0 WHERE email_key = '{0}'",
                        Mysql.Escape(emailKey)));

                    if (rowsChanged > 0)
                    {
                        core.Display.ShowMessage("Opt-out of " + core.Settings.SiteTitle + " Mailings", "You have successfully opted-out of further " + core.Settings.SiteTitle + " mailings. If you continue to receive mailings send an e-mail to contact@" + Hyperlink.Domain + " with the subject \"opt-out\".");
                        return;
                    }
                    else
                    {
                        core.Display.ShowMessage("Cannot Opt-out", "The opt-out key you have given is missing or incomplete. To manually opt-out send an e-mail to contact@" + Hyperlink.Domain + " with the subject \"opt-out\".");
                        return;
                    }
                }
                else
                {
                    core.Display.ShowMessage("Cannot Opt-out", "The opt-out key you have given is missing or incomplete. To manually opt-out send an e-mail to contact@" + Hyperlink.Domain + " with the subject \"opt-out\".");
                    return;
                }
            }
            else if (mode == "activate")
            {
                long userId = 0;
                string activateKey = (string)Request.QueryString["key"];

                try
                {
                    userId = long.Parse(Request.QueryString["id"]);
                }
                catch
                {
                    core.Display.ShowMessage("Error", "Error activating user.");
                    return;
                }

                DataTable userTable = db.Query(string.Format("SELECT user_id FROM user_info WHERE user_id = {0} AND user_activate_code = '{1}';",
                    userId, Mysql.Escape(activateKey)));

                if (userTable.Rows.Count == 1)
                {
                    db.UpdateQuery(string.Format("UPDATE user_info SET user_active = 1 WHERE user_id = {0} AND user_activate_code = '{1}';",
                        userId, Mysql.Escape(activateKey)));

                    core.Display.ShowMessage("Success", "You have successfully activated your account. You may now [iurl=\"/sign-in/\"]sign in[/iurl].", ShowMessageOptions.Bbcode);
                    return;
                }
                else
                {
                    core.Display.ShowMessage("Error", "Error activating user.");
                    return;
                }
            }
            else if (mode == "activate-password")
            {
                long userId = 0;
                string activateKey = (string)Request.QueryString["key"];

                try
                {
                    userId = long.Parse(Request.QueryString["id"]);
                }
                catch
                {
                    core.Display.ShowMessage("Error", "Error activating new password.");
                    return;
                }

                DataTable userTable = db.Query(string.Format("SELECT user_id, user_new_password FROM user_info WHERE user_id = {0} AND user_activate_code = '{1}';",
                    userId, Mysql.Escape(activateKey)));

                if (userTable.Rows.Count == 1)
                {
                    db.UpdateQuery(string.Format("UPDATE user_info SET user_password = '{2}', user_new_password = '' WHERE user_id = {0} AND user_activate_code = '{1}';",
                        userId, Mysql.Escape(activateKey), Mysql.Escape(BoxSocial.Internals.User.HashPassword((string)userTable.Rows[0]["user_new_password"]))));

                    core.Display.ShowMessage("Success", "You have successfully activated your new password. You may now [url=\"/sign-in/\"]sign in[/url].");
                    return;
                }
                else
                {
                    core.Display.ShowMessage("Error", "Error activating new password.");
                    return;
                }
            }
            else if (core.Http.Form["submit"] == null)
            {
                long groupId = core.Functions.FormLong("gid", core.Functions.RequestLong("gid", 0));
                string emailKey = core.Http.Query["key"];
                string referralKey = core.Http.Query["refer"];
                bool continueSignup = false;

                Dictionary<string, InviteKey> keys = InviteKey.GetInvites(core, emailKey);
                Dictionary<string, ReferralKey> referrals = ReferralKey.GetReferrals(core, referralKey);

                if (core.Settings.SignupMode == "invite")
                {
                    if (keys.Count == 0 && referrals.Count == 0)
                    {
                        continueSignup = false;
                    }
                    else
                    {
                        continueSignup = true;
                    }
                }
                else
                {
                    continueSignup = true;
                }

                if (continueSignup)
                {
                    template.Parse("GID", groupId.ToString());

                    prepareNewCaptcha();
                    if (!string.IsNullOrEmpty(emailKey))
                    {
                        template.Parse("EMAIL_KEY", emailKey);
                    }

                    if (!string.IsNullOrEmpty(referralKey))
                    {
                        template.Parse("REFERRAL_KEY", referralKey);
                    }

                    if (groupId > 0)
                    {
                        try
                        {
                            UserGroup thisGroup = new UserGroup(core, groupId);
                            if (loggedInMember != null)
                            {
                                if (loggedInMember.UserInfo.ShowCustomStyles)
                                {
                                    template.Parse("USER_STYLE_SHEET", string.Format("group/{0}.css", thisGroup.Key));
                                }
                            }
                            else
                            {
                                template.Parse("USER_STYLE_SHEET", string.Format("group/{0}.css", thisGroup.Key));
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    core.Display.ShowMessage("Invite Only", "Sorry, registration is current on an invite-only basis at the moment. Check back later.");
                }
            }
            else
            {
                long groupId = core.Functions.FormLong("gid", core.Functions.RequestLong("gid", 0));
                string emailKey = core.Http.Form["key"];
                string referralKey = core.Http.Form["refer"];
                bool continueSignup = false;
                List<long> invitedById = new List<long>();

                Dictionary<string, InviteKey> keys = InviteKey.GetInvites(core, emailKey);
                Dictionary<string, ReferralKey> referrals = ReferralKey.GetReferrals(core, referralKey);

                if (core.Settings.SignupMode == "invite")
                {
                    if (keys.Count == 0 && referrals.Count == 0)
                    {
                        continueSignup = false;
                    }
                    else
                    {
                        continueSignup = true;

                        foreach (string key in keys.Keys)
                        {
                            invitedById.Add(keys[key].InviteUserId);
                        }
                    }
                }
                else
                {
                    continueSignup = true;
                }

                if (continueSignup)
                {
                    // submit the form
                    template.Parse("USERNAME", (string)core.Http.Form["username"]);
                    template.Parse("EMAIL", (string)core.Http.Form["email"]);
                    template.Parse("CONFIRM_EMAIL", (string)core.Http.Form["confirm-email"]);
                    template.Parse("GID", groupId.ToString());

                    if (!string.IsNullOrEmpty(emailKey))
                    {
                        template.Parse("EMAIL_KEY", emailKey);
                    }

                    if (!string.IsNullOrEmpty(referralKey))
                    {
                        template.Parse("REFERRAL_KEY", referralKey);
                    }

                    DataTable confirmTable = db.Query(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 1 AND session_id = '{0}' LIMIT 1",
                        Mysql.Escape(session.SessionId)));

                    if (confirmTable.Rows.Count != 1)
                    {
                        template.Parse("ERROR", "Captcha is invalid, please try again.");
                        prepareNewCaptcha();
                    }
                    else if (((string)confirmTable.Rows[0]["confirm_code"]).ToLower() != ((string)core.Http.Form["captcha"]).ToLower())
                    {
                        template.Parse("ERROR", "Captcha is invalid, please try again.");
                        prepareNewCaptcha();
                    }
                    else if (!BoxSocial.Internals.User.CheckUserNameValid(core.Http.Form["username"]))
                    {
                        template.Parse("ERROR", "Username is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                        prepareNewCaptcha();
                    }
                    else if (!BoxSocial.Internals.User.CheckUserNameUnique(db, core.Http.Form["username"]))
                    {
                        template.Parse("ERROR", "Username is already taken, please choose another one.");
                        prepareNewCaptcha();
                    }
                    else if (!BoxSocial.Internals.User.CheckEmailValid(core.Http.Form["email"]))
                    {
                        template.Parse("ERROR", "You have entered an invalid e-mail address, you must use a valid e-mail address to complete registration.");
                        prepareNewCaptcha();
                    }
                    else if (!BoxSocial.Internals.User.CheckEmailUnique(core, core.Http.Form["email"]))
                    {
                        template.Parse("ERROR", "The e-mail address you have entered has already been registered.");
                        prepareNewCaptcha();
                    }
                    else if (core.Http.Form["email"] != core.Http.Form["confirm-email"])
                    {
                        template.Parse("ERROR", "The e-mail addresses you entered do not match, may sure you have entered your e-mail address correctly.");
                        prepareNewCaptcha();
                    }
                    else if (core.Http.Form["password"] != core.Http.Form["confirm-password"])
                    {
                        template.Parse("ERROR", "The passwords you entered do not match, make sure you have entered your desired password correctly.");
                        prepareNewCaptcha();
                    }
                    else if (((string)core.Http.Form["password"]).Length < 6)
                    {
                        template.Parse("ERROR", "The password you entered is too short. Please choose a strong password of 6 characters or more.");
                        prepareNewCaptcha();
                    }
                    else if ((string)core.Http.Form["agree"] != "true")
                    {
                        template.Parse("ERROR", "You must accept the " + core.Settings.SiteTitle + " Terms of Service to register an account.");
                        prepareNewCaptcha();
                    }
                    else
                    {
                        User newUser = BoxSocial.Internals.User.Register(Core, core.Http.Form["username"], core.Http.Form["email"], core.Http.Form["password"], core.Http.Form["confirm-password"]);
                        if (newUser == null)
                        {
                            template.Parse("ERROR", "Bad registration details");
                            prepareNewCaptcha();
                        }
                        else
                        {
                            // captcha is a use once thing, destroy all for this session
                            db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 1 AND session_id = '{0}'",
                                Mysql.Escape(session.SessionId)));

                            // Invite keys are single use
                            if (!string.IsNullOrEmpty(emailKey))
                            {
                                db.UpdateQuery(string.Format("DELETE FROM invite_keys WHERE email_key = '{0}'",
                                    Mysql.Escape(emailKey)));
                            }

                            foreach (long friendId in invitedById)
                            {
                                if (friendId > 0)
                                {
                                    long relationId = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FRIEND');",
                                        newUser.UserId, friendId));

                                    long relationId2 = db.UpdateQuery(string.Format("INSERT INTO user_relations (relation_me, relation_you, relation_time_ut, relation_type) VALUES ({0}, {1}, UNIX_TIMESTAMP(), 'FRIEND');",
                                        friendId, newUser.UserId));

                                    db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends + 1 WHERE ui.user_id = {0};",
                                        friendId));

                                    db.UpdateQuery(string.Format("UPDATE user_info ui SET ui.user_friends = ui.user_friends + 1 WHERE ui.user_id = {0};",
                                        newUser.UserId));
                                }
                            }

                            if (groupId > 0)
                            {
                                try
                                {
                                    UserGroup thisGroup = new UserGroup(core, groupId);

                                    if (loggedInMember != null)
                                    {
                                        if (loggedInMember.UserInfo.ShowCustomStyles)
                                        {
                                            template.Parse("USER_STYLE_SHEET", string.Format("group/{0}.css", thisGroup.Key));
                                        }
                                    }
                                    else
                                    {
                                        template.Parse("USER_STYLE_SHEET", string.Format("group/{0}.css", thisGroup.Key));
                                    }

                                    int activated = 0;

                                    switch (thisGroup.GroupType)
                                    {
                                        case "OPEN":
                                        case "PRIVATE": // assume as you've been invited that it is enough for activation
                                            activated = 1;
                                            break;
                                        case "REQUEST":
                                        case "CLOSED":
                                            activated = 0;
                                            break;
                                    }

                                    bool isInvited = thisGroup.IsGroupInvitee(newUser);

                                    // do not need an invite unless the group is private
                                    // private groups you must be invited to
                                    if (thisGroup.GroupType != "PRIVATE" || (thisGroup.GroupType == "PRIVATE" && isInvited))
                                    {
                                        db.BeginTransaction();
                                        db.UpdateQuery(string.Format("INSERT INTO group_members (group_id, user_id, group_member_approved, group_member_ip, group_member_date_ut) VALUES ({0}, {1}, {2}, '{3}', UNIX_TIMESTAMP());",
                                            thisGroup.GroupId, newUser.Id, activated, Mysql.Escape(session.IPAddress.ToString()), true));

                                        if (activated == 1)
                                        {
                                            db.UpdateQuery(string.Format("UPDATE group_info SET group_members = group_members + 1 WHERE group_id = {0}",
                                                thisGroup.GroupId));
                                        }

                                        // just do it anyway, can be invited to any type of group
                                        db.UpdateQuery(string.Format("DELETE FROM group_invites WHERE group_id = {0} AND user_id = {1}",
                                            thisGroup.GroupId, newUser.Id));
                                    }

                                    core.Template.Parse("REDIRECT_URI", thisGroup.Uri);
                                }
                                catch (InvalidGroupException)
                                {
                                }
                            }

                            //Response.Redirect("/", true);
                            core.Display.ShowMessage("Registered", "You have registered. Before you can use your account you must verify your e-mail address by clicking a verification link sent to it.");
                            return; /* stop processing the display of this page */
                        }
                    }
                }
                else
                {
                    core.Display.ShowMessage("Invite Only", "Sorry, registration is current on an invite-only basis at the moment. Check back later.");
                }
            }

            EndResponse();
        }
    }
}
