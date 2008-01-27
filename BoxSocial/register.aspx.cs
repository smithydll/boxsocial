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
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial
{
    public partial class register : TPage
    {
        public register()
            : base("register.html")
        {
        }

        private void prepareNewCaptcha()
        {
            // prepare the captcha
            string captchaString = captcha.GenerateCaptchaString();
            //string captchaSecurityToken = captcha.GenerateCaptchaSecurityToken();

            // delete all existing for this session
            // captcha is a use once thing, destroy all for this session
            db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 1 AND session_id = '{0}'",
                Mysql.Escape(session.SessionId)));

            // create a new confimation code
            long confirmId = db.UpdateQuery(string.Format("INSERT INTO confirm (session_id, confirm_code, confirm_type) VALUES ('{0}', '{1}', '{2}')",
                Mysql.Escape(session.SessionId), Mysql.Escape(captchaString), 1));

            template.ParseVariables("U_CAPTCHA", HttpUtility.HtmlEncode(Linker.AppendSid("/captcha.aspx?secureid=" + confirmId.ToString(), true)));
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (Request.QueryString["mode"] == "optout")
            {
                string emailKey = Request.QueryString["key"];

                if (emailKey.Length == 32)
                {
                    long rowsChanged = db.UpdateQuery(string.Format("UPDATE invite_keys SET invite_allow = 0 WHERE email_key = '{0}'",
                        Mysql.Escape(emailKey)));

                    if (rowsChanged > 0)
                    {
                        Display.ShowMessage(Core, "Opt-out of ZinZam Mailings", "You have successfully opted-out of further ZinZam mailings. If you continue to receive mailings send an e-mail to contact@zinzam.com with the subject \"opt-out\".");
                        return;
                    }
                    else
                    {
                        Display.ShowMessage(Core, "Cannot Opt-out", "The opt-out key you have given is missing or incomplete. To manually opt-out send an e-mail to contact@zinzam.com with the subject \"opt-out\".");
                        return;
                    }
                }
                else
                {
                    Display.ShowMessage(Core, "Cannot Opt-out", "The opt-out key you have given is missing or incomplete. To manually opt-out send an e-mail to contact@zinzam.com with the subject \"opt-out\".");
                    return;
                }
            }
            else if (Request.QueryString["mode"] == "activate")
            {
                long userId = 0;
                string activateKey = (string)Request.QueryString["key"];

                try
                {
                    userId = long.Parse(Request.QueryString["id"]);
                }
                catch
                {
                    Display.ShowMessage(Core, "Error", "Error activating user.");
                    return;
                }

                DataTable userTable = db.SelectQuery(string.Format("SELECT user_id FROM user_info WHERE user_id = {0} AND user_activate_code = '{1}';",
                    userId, Mysql.Escape(activateKey)));

                if (userTable.Rows.Count == 1)
                {
                    db.UpdateQuery(string.Format("UPDATE user_info SET user_active = 1 WHERE user_id = {0} AND user_activate_code = '{1}';",
                        userId, Mysql.Escape(activateKey)));

                    Display.ShowMessage(Core, "Success", "You have successfully activated your account. You may now <a href=\"/sign-in/\">sign in</a>.");
                    return;
                }
                else
                {
                    Display.ShowMessage(Core, "Error", "Error activating user.");
                    return;
                }
            }
            else if (Request.Form["submit"] == null)
            {
                prepareNewCaptcha();
            }
            else
            {
                // submit the form
                template.ParseVariables("USERNAME", HttpUtility.HtmlEncode((string)Request.Form["username"]));
                template.ParseVariables("EMAIL", HttpUtility.HtmlEncode((string)Request.Form["email"]));
                template.ParseVariables("CONFIRM_EMAIL", HttpUtility.HtmlEncode((string)Request.Form["confirm-email"]));

                DataTable confirmTable = db.SelectQuery(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 1 AND session_id = '{0}' LIMIT 1",
                    Mysql.Escape(session.SessionId)));

                if (confirmTable.Rows.Count != 1)
                {
                    template.ParseVariables("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha();
                }
                else if (((string)confirmTable.Rows[0]["confirm_code"]).ToLower() != ((string)Request.Form["captcha"]).ToLower())
                {
                    template.ParseVariables("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha();
                }
                else if (!Member.CheckUserNameValid(Request.Form["username"]))
                {
                    template.ParseVariables("ERROR", "Username is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                    prepareNewCaptcha();
                }
                else if (!Member.CheckUserNameUnique(db, Request.Form["username"]))
                {
                    template.ParseVariables("ERROR", "Username is already taken, please choose another one.");
                    prepareNewCaptcha();
                }
                else if (!Member.CheckEmailValid(Request.Form["email"]))
                {
                    template.ParseVariables("ERROR", "You have entered an invalid e-mail address, you must use a valid e-mail address to complete registration.");
                    prepareNewCaptcha();
                }
                else if (!Member.CheckEmailUnique(db, Request.Form["email"]))
                {
                    template.ParseVariables("ERROR", "The e-mail address you have entered has already been registered.");
                    prepareNewCaptcha();
                }
                else if (Request.Form["email"] != Request.Form["confirm-email"])
                {
                    template.ParseVariables("ERROR", "The e-mail addresses you entered do not match, may sure you have entered your e-mail address correctly.");
                    prepareNewCaptcha();
                }
                else if (Request.Form["password"] != Request.Form["confirm-password"])
                {
                    template.ParseVariables("ERROR", "The passwords you entered do not match, may sure you have entered your desired password correctly.");
                    prepareNewCaptcha();
                }
                else if (((string)Request.Form["password"]).Length < 6)
                {
                    template.ParseVariables("ERROR", "The password you entered is too short. Please choose a strong password of 6 characters or more.");
                    prepareNewCaptcha();
                }
                else if ((string)Request.Form["agree"] != "true")
                {
                    template.ParseVariables("ERROR", "You must accept the ZinZam Terms of Service to register an account.");
                    prepareNewCaptcha();
                }
                else
                {
                    if (Member.Register(Core, Request.Form["username"], Request.Form["email"], Request.Form["password"], Request.Form["confirm-password"]) == null)
                    {
                        template.ParseVariables("ERROR", "Bad registration details");
                        prepareNewCaptcha();
                    }
                    else
                    {
                        // captcha is a use once thing, destroy all for this session
                        db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 1 AND session_id = '{0}'",
                            Mysql.Escape(session.SessionId)));

                        //Response.Redirect("/", true);
                        Display.ShowMessage(Core, "Registered", "You have registered. Before you can use your account you must verify your e-mail address by clicking a link sent to it.");
                        return; /* stop processing the display of this page */
                    }
                }
            }

            EndResponse();
        }
    }
}
