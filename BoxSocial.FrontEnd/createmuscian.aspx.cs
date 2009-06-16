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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Musician;

namespace BoxSocial.FrontEnd
{
    public partial class createmuiscian : TPage
    {
        public createmuiscian()
            : base("createmuiscian.html")
        {
        }

        private void prepareNewCaptcha()
        {
            // prepare the captcha
            string captchaString = Captcha.GenerateCaptchaString();

            // delete all existing for this session
            // captcha is a use once thing, destroy all for this session
            Confirmation.ClearStale(core, session.SessionId, 2);

            // create a new confimation code
            Confirmation confirm = Confirmation.Create(core, session.SessionId, captchaString, 2);

            template.Parse("U_CAPTCHA", core.Uri.AppendSid("/captcha.aspx?secureid=" + confirm.ConfirmId.ToString(), true));
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (session.IsLoggedIn == false)
            {
                template.Parse("REDIRECT_URI", "/sign-in/?redirect=/groups/create");
                core.Display.ShowMessage("Not Logged In", "You must be logged in to register a new muscian.");
                return;
            }

            string slug = Request.Form["slug"];
            string title = Request.Form["title"];

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

            if (Request.Form["submit"] == null)
            {
                prepareNewCaptcha();
            }
            else
            {
                // submit the form
                template.Parse("MUSCIAN_TITLE", (string)Request.Form["title"]);
                template.Parse("MUSCIAN_NAME_SLUG", slug);

                DataTable confirmTable = db.Query(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 3 AND session_id = '{0}' LIMIT 1",
                    Mysql.Escape(session.SessionId)));

                if (confirmTable.Rows.Count != 1)
                {
                    template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha();
                }
                else if (((string)confirmTable.Rows[0]["confirm_code"]).ToLower() != ((string)Request.Form["captcha"]).ToLower())
                {
                    template.Parse("ERROR", "Captcha is invalid, please try again.");
                    prepareNewCaptcha();
                }
                else if (!Musician.Musician.CheckMusicianNameValid(slug))
                {
                    template.Parse("ERROR", "Muscian slug is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                    prepareNewCaptcha();
                }
                else if (!Musician.Musician.CheckMusicianNameUnique(core, slug))
                {
                    template.Parse("ERROR", "Muscian slug is already taken, please choose another one.");
                    prepareNewCaptcha();
                }
                else if ((string)Request.Form["agree"] != "true")
                {
                    template.Parse("ERROR", "You must accept the ZinZam Terms of Service to create register a muscian.");
                    prepareNewCaptcha();
                }
                else
                {
                    Musician.Musician newMusician = Musician.Musician.Create(core, Request.Form["title"], slug);

                    if (newMusician == null)
                    {
                        template.Parse("ERROR", "Bad registration details");
                        prepareNewCaptcha();
                    }
                    else
                    {
                        // captcha is a use once thing, destroy all for this session
                        db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 3 AND session_id = '{0}'",
                            Mysql.Escape(session.SessionId)));

                        //Response.Redirect("/", true);
                        template.Parse("REDIRECT_URI", newMusician.Uri);
                        core.Display.ShowMessage("Musician Registered", "You have have registered a new musician. You will be redirected to the musician home page in a second.");
                        return; /* stop processing the display of this page */
                    }
                }
            }

            EndResponse();
        }
    }
}
