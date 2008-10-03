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
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.FrontEnd
{
    public partial class creategroup : TPage
    {
        public creategroup()
            : base("creategroup.html")
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

            template.Parse("U_CAPTCHA", Linker.AppendSid("/captcha.aspx?secureid=" + confirm.ConfirmId.ToString(), true));
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (session.IsLoggedIn == false)
            {
                template.Parse("REDIRECT_URI", "/sign-in/?redirect=/groups/create");
                Display.ShowMessage("Not Logged In", "You must be logged in to create a group.");
                return;
            }

            string selected = "checked=\"checked\" ";
            short category = 1;
            bool categoryError = false;
            bool typeError = false;
            bool categoryFound = true;
            string slug = Request.Form["slug"];
            string title = Request.Form["title"];

            try
            {
                category = short.Parse(Request["category"]);
            }
            catch
            {
                categoryError = true;
            }

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

            Dictionary<string, string> categories = new Dictionary<string, string>();
            DataTable categoriesTable = db.Query("SELECT category_id, category_title FROM global_categories ORDER BY category_title ASC;");
            foreach (DataRow categoryRow in categoriesTable.Rows)
            {
                categories.Add(((short)categoryRow["category_id"]).ToString(), (string)categoryRow["category_title"]);

                if (category == (short)categoryRow["category_id"])
                {
                    categoryFound = true;
                }
            }

            if (!categoryFound)
            {
                categoryError = true;
            }

            if (Request.Form["submit"] == null)
            {
                prepareNewCaptcha();
                //template.Parse("S_CATEGORIES", Functions.BuildSelectBox("category", categories, category.ToString()));
                Display.ParseSelectBox("S_CATEGORIES", "category", categories, category.ToString());
                template.Parse("S_OPEN_CHECKED", selected);
            }
            else
            {
                // submit the form
                template.Parse("GROUP_TITLE", (string)Request.Form["title"]);
                template.Parse("GROUP_NAME_SLUG", slug);
                template.Parse("GROUP_DESCRIPTION", (string)Request.Form["description"]);
                //template.ParseRaw("S_CATEGORIES", Functions.BuildSelectBox("category", categories, category.ToString()));
                Display.ParseSelectBox("S_CATEGORIES", "category", categories, category.ToString());

                switch ((string)Request.Form["type"])
                {
                    case "open":
                        template.Parse("S_OPEN_CHECKED", selected);
                        break;
                    case "closed":
                        template.Parse("S_CLOSED_CHECKED", selected);
                        break;
                    case "private":
                        template.Parse("S_PRIVATE_CHECKED", selected);
                        break;
                    default:
                        typeError = true;
                        break;
                }

                DataTable confirmTable = db.Query(string.Format("SELECT confirm_code FROM confirm WHERE confirm_type = 2 AND session_id = '{0}' LIMIT 1",
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
                else if (!UserGroup.CheckGroupNameValid(slug))
                {
                    template.Parse("ERROR", "Group slug is invalid, you may only use letters, numbers, period, underscores or a dash (a-z, 0-9, '_', '-', '.').");
                    prepareNewCaptcha();
                }
                else if (!UserGroup.CheckGroupNameUnique(core, slug))
                {
                    template.Parse("ERROR", "Group slug is already taken, please choose another one.");
                    prepareNewCaptcha();
                }
                else if (categoryError)
                {
                    template.Parse("ERROR", "Invalid Category selected, you may have to reload the page.");
                    prepareNewCaptcha();
                }
                else if (typeError)
                {
                    template.Parse("ERROR", "Invalid group type selected, you may have to reload the page.");
                    prepareNewCaptcha();
                }
                else if ((string)Request.Form["agree"] != "true")
                {
                    template.Parse("ERROR", "You must accept the ZinZam Terms of Service to create a group.");
                    prepareNewCaptcha();
                }
                else
                {
                    UserGroup newGroup = UserGroup.Create(core, Request.Form["title"], slug, Request.Form["description"], category, Request.Form["type"]);
                    if (newGroup == null)
                    {
                        template.Parse("ERROR", "Bad registration details");
                        prepareNewCaptcha();
                    }
                    else
                    {
                        // captcha is a use once thing, destroy all for this session
                        db.UpdateQuery(string.Format("DELETE FROM confirm WHERE confirm_type = 2 AND session_id = '{0}'",
                            Mysql.Escape(session.SessionId)));

                        //Response.Redirect("/", true);
                        template.Parse("REDIRECT_URI", newGroup.Uri);
                        Display.ShowMessage("Group Created", "You have have created a new group. You will be redirected to the group home page in a second.");
                        return; /* stop processing the display of this page */
                    }
                }
            }

            EndResponse();
        }
    }
}
