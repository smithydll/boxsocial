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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Groups;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.FrontEnd
{
    public partial class registerapplication : TPage 
    {

        public registerapplication()
            : base("registerapplication.html")
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
            if (core.Session.IsLoggedIn == false)
            {
                template.Parse("REDIRECT_URI", "/sign-in/?redirect=/applications/register");
                core.Display.ShowMessage("Not Logged In", "You must be logged in to register a new application.");
                return;
            }

            template.Parse("S_POST", core.Hyperlink.AppendSid("/applications/register/", true));

            string slug = core.Http.Form["slug"];
            string title = core.Http.Form["title"];

            if (string.IsNullOrEmpty(slug))
            {
                slug = title;
            }

            if (!string.IsNullOrEmpty(title))
            {
                // normalise slug if it has been fiddeled with
                slug = slug.ToLower().Normalize(NormalizationForm.FormD);
                string normalisedSlug = "";

                for (int i = 0; i < slug.Length; i++)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(slug[i]) != UnicodeCategory.NonSpacingMark)
                    {
                        normalisedSlug += slug[i];
                    }
                }
                slug = Regex.Replace(normalisedSlug, @"([\W]+)", "-");
            }

            if (core.Http.Form["submit"] == null)
            {
                prepareNewCaptcha(core);


            }
            else
            {
                // submit the form
                template.Parse("APPLICATION_TITLE", (string)core.Http.Form["title"]);
                template.Parse("APPLICATION_SLUG", slug);
                Template.Parse("APPLICATION_DESCRIPTION", (string)core.Http.Form["description"]);

                DataTable confirmTable = db.Query(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 3 AND session_id = '{0}' LIMIT 1",
                    Mysql.Escape(core.Session.SessionId)));

                if (confirmTable.Rows.Count != 1)
                {
                    template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha(core);
                }
                else if (((string)confirmTable.Rows[0]["confirm_code"]).ToLower() != ((string)core.Http.Form["captcha"]).ToLower())
                {
                    template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha(core);
                }
                else if (!ApplicationEntry.CheckApplicationNameValid(slug))
                {
                    template.Parse("ERROR", "Application slug is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                    prepareNewCaptcha(core);
                }
                else if (!ApplicationEntry.CheckApplicationNameUnique(core, slug))
                {
                    template.Parse("ERROR", "Application slug is already taken, please choose another one.");
                    prepareNewCaptcha(core);
                }
                else if ((string)core.Http.Form["agree"] != "true")
                {
                    template.Parse("ERROR", "You must accept the " + core.Settings.SiteTitle + " Terms of Service to create an application.");
                    prepareNewCaptcha(core);
                }
                else
                {
                    OAuthApplication newApplication = null;
                    try
                    {
                        newApplication = OAuthApplication.Create(core, core.Http.Form["title"], slug, core.Http.Form["description"]);
                    }
                    catch (InvalidOperationException)
                    {
                        /*Response.Write("InvalidOperationException<br />");
                        Response.Write(e.Db.QueryList);
                        Response.End();*/
                    }
                    catch (InvalidApplicationException)
                    {
                        /*Response.Write("InvalidGroupException<br />");
                        Response.Write(e.Db.QueryList);
                        Response.End();*/
                    }

                    if (newApplication == null)
                    {
                        template.Parse("ERROR", "Bad registration details");
                        prepareNewCaptcha(core);
                    }
                    else
                    {
                        // captcha is a use once thing, destroy all for this session
                        db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 3 AND session_id = '{0}'",
                            Mysql.Escape(core.Session.SessionId)));

                        //Response.Redirect("/", true);
                        template.Parse("REDIRECT_URI", newApplication.Uri);
                        core.Display.ShowMessage("Application Created", "You have have created a new application. You will be redirected to the application home page in a second.");
                        return; /* stop processing the display of this page */
                    }
                }
            }

            EndResponse();
        }

        private static void prepareNewCaptcha(Core core)
        {
            // prepare the captcha
            string captchaString = Captcha.GenerateCaptchaString();

            // delete all existing for this session
            // captcha is a use once thing, destroy all for this session
            Confirmation.ClearStale(core, core.Session.SessionId, 3);

            // create a new confimation code
            Confirmation confirm = Confirmation.Create(core, core.Session.SessionId, captchaString, 3);

            core.Template.Parse("U_CAPTCHA", core.Hyperlink.AppendSid("/captcha.aspx?secureid=" + confirm.ConfirmId.ToString(), true));
        }
    }
}
