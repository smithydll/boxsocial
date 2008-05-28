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
using BoxSocial.IO;
using BoxSocial.Internals;
using BoxSocial.Applications.Gallery; // This is why Gallery is an uninstallable application

namespace BoxSocial
{
    [AccountModule("profile")]
    public class AccountProfile : AccountModule
    {

        public AccountProfile(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(Info);
            RegisterSubModule += new RegisterSubModuleHandler(MyName);
            RegisterSubModule += new RegisterSubModuleHandler(DisplayPicture);
            RegisterSubModule += new RegisterSubModuleHandler(Contact);
            // TODO: personality
            RegisterSubModule += new RegisterSubModuleHandler(Lifestyle);
            RegisterSubModule += new RegisterSubModuleHandler(Style);
            RegisterSubModule += new RegisterSubModuleHandler(Permissions);
            RegisterSubModule += new RegisterSubModuleHandler(SaveStatus);
        }

        protected override void RegisterModule(Core core, EventArgs e)
        {
        }

        public override string Name
        {
            get
            {
                return "Profile";
            }
        }

        /*public override string Key
        {
            get
            {
                return "profile";
            }
        }*/

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        private void Lifestyle(string submodule)
        {
            subModules.Add("lifestyle", "Lifestyle");
            if (submodule != "lifestyle") return;

            if (Request.Form["save"] != null)
            {
                LifestyleSave();
                return;
            }

            //loggedInMember.LoadProfileInfo();

            template.SetTemplate("Profile", "account_lifestyle");

            Dictionary<string, string> maritialStatuses = new Dictionary<string, string>();
            maritialStatuses.Add("UNDEF", "No Answer");
            maritialStatuses.Add("SINGLE", "Single");
            maritialStatuses.Add("RELATIONSHIP", "In a Relationship");
            maritialStatuses.Add("MARRIED", "Married");
            maritialStatuses.Add("SWINGER", "Swinger");
            maritialStatuses.Add("DIVORCED", "Divorced");
            maritialStatuses.Add("WIDOWED", "Widowed");

            Dictionary<string, string> religions = new Dictionary<string, string>();
            religions.Add("0", "No Answer");

            DataTable religionsTable = db.Query("SELECT * FROM religions ORDER BY religion_title ASC");

            foreach (DataRow religionRow in religionsTable.Rows)
            {
                religions.Add(((short)religionRow["religion_id"]).ToString(), (string)religionRow["religion_title"]);
            }

            Dictionary<string, string> sexualities = new Dictionary<string, string>();
            sexualities.Add("UNDEF", "No Answer");
            sexualities.Add("UNSURE", "Unsure");
            sexualities.Add("STRAIGHT", "Straight");
            sexualities.Add("HOMOSEXUAL", "Homosexual");
            sexualities.Add("BISEXUAL", "Bisexual");
            sexualities.Add("TRANSEXUAL", "Transexual");

            template.ParseVariables("S_MARITIAL_STATUS", Functions.BuildSelectBox("maritial-status", maritialStatuses, loggedInMember.MaritialStatusRaw));
            template.ParseVariables("S_RELIGION", Functions.BuildSelectBox("religion", religions, loggedInMember.ReligionRaw.ToString()));
            template.ParseVariables("S_SEXUALITY", Functions.BuildSelectBox("sexuality", sexualities, loggedInMember.SexualityRaw));
        }

        public void LifestyleSave()
        {
            db.UpdateQuery(string.Format("UPDATE user_profile SET profile_religion = {1}, profile_sexuality = '{2}', profile_maritial_status = '{3}' WHERE user_id = {0};",
                loggedInMember.UserId, int.Parse(Request.Form["religion"]), Mysql.Escape(Request.Form["sexuality"]), Mysql.Escape(Request.Form["maritial-status"])));

            Display.ShowMessage("Lifestyle Saved", "Your lifestyle has been saved in the database.<br /><a href=\"/account/?module=profile&sub=lifestyle\">Return</a>");
        }

        public void Info(string submodule)
        {
            subModules.Add("info", "My Information");
            if (submodule != "info" && !string.IsNullOrEmpty(submodule)) return;

            if (Request.Form["save"] != null)
            {
                InfoSave();
                return;
            }

            //loggedInMember.LoadProfileInfo();

            string selected = " checked=\"checked\"";
            switch (loggedInMember.GenderRaw)
            {
                case "UNDEF":
                    template.ParseVariables("S_GENDER_UNDEF", selected);
                    break;
                case "MALE":
                    template.ParseVariables("S_GENDER_MALE", selected);
                    break;
                case "FEMALE":
                    template.ParseVariables("S_GENDER_FEMALE", selected);
                    break;
            }

            Dictionary<string, string> dobYears = new Dictionary<string, string>();
            for (int i = DateTime.Now.AddYears(-110).Year; i < DateTime.Now.AddYears(-13).Year; i++)
            {
                dobYears.Add(i.ToString(), i.ToString());
            }

            Dictionary<string, string> dobMonths = new Dictionary<string, string>();
            for (int i = 1; i < 13; i++)
            {
                dobMonths.Add(i.ToString(), Functions.IntToMonth(i));
            }

            Dictionary<string, string> dobDays = new Dictionary<string, string>();
            for (int i = 1; i < 32; i++)
            {
                dobDays.Add(i.ToString(), i.ToString());
            }

            Dictionary<string, string> countries = new Dictionary<string, string>();

            DataTable countriesTable = db.Query("SELECT * FROM countries ORDER BY country_name ASC");

            countries.Add("", "Unspecified");
            foreach (DataRow countryRow in countriesTable.Rows)
            {
                countries.Add((string)countryRow["country_iso"], (string)countryRow["country_name"]);
            }

            template.ParseVariables("S_DOB_YEAR", Functions.BuildSelectBox("dob-year", dobYears, loggedInMember.DateOfBirth.Year.ToString()));
            template.ParseVariables("S_DOB_MONTH", Functions.BuildSelectBox("dob-month", dobMonths, loggedInMember.DateOfBirth.Month.ToString()));
            template.ParseVariables("S_DOB_DAY", Functions.BuildSelectBox("dob-day", dobDays, loggedInMember.DateOfBirth.Day.ToString()));
            template.ParseVariables("S_COUNTRY", Functions.BuildSelectBox("country", countries, loggedInMember.CountryIso));
            template.ParseVariables("S_AUTO_BIOGRAPHY", HttpUtility.HtmlEncode(loggedInMember.Autobiography));

            template.SetTemplate("account_profile.html");
        }

        public void InfoSave()
        {
            string dob = string.Format("{0:0000}-{1:00}-{2:00}",
                Request.Form["dob-year"], Request.Form["dob-month"], Request.Form["dob-day"]);

            db.UpdateQuery(string.Format("UPDATE user_profile SET profile_date_of_birth = '{1}', profile_gender = '{2}', profile_country = '{3}', profile_autobiography = '{4}' WHERE user_id = {0};",
                loggedInMember.UserId, Mysql.Escape(dob), Mysql.Escape(Request.Form["gender"]), Mysql.Escape(Request.Form["country"]), Mysql.Escape(Request.Form["auto-biography"])));

            SetRedirectUri(AccountModule.BuildModuleUri("profile", "info"));
            Display.ShowMessage("Information Saved", "Your information has been saved in the database.<br /><a href=\"/account/?module=profile&sub=info\">Return</a>");
        }

        public void Style(string submodule)
        {
            subModules.Add("style", "Profile Style");
            if (submodule != "style") return;

            if (Request.Form["save"] != null)
            {
                StyleSave();
                return;
            }

            string mode = Request["mode"];

            template.SetTemplate("Profile", "account_style");

            CascadingStyleSheet css = new CascadingStyleSheet();
            css.Parse(loggedInMember.GetUserStyle());

            StyleGenerator editor = css.Generator;

            if (string.IsNullOrEmpty(loggedInMember.GetUserStyle()))
            {
                editor = StyleGenerator.Theme;
            }

            if (!string.IsNullOrEmpty(mode))
            {
                switch (mode.ToLower())
                {
                    case "theme":
                        editor = StyleGenerator.Theme;
                        break;
                    case "standard":
                        editor = StyleGenerator.Standard;
                        break;
                    case "advanced":
                        editor = StyleGenerator.Advanced;
                        break;
                }
            }

            string backgroundRepeat = "no-repeat";
            string backgroundPosition = "left top";
            bool backgroundAttachment = false; // False == scroll, true == fixed

            if (editor == StyleGenerator.Theme)
            {
                template.ParseVariables("THEME_EDITOR", "TRUE");

                if (css.Hue == -1)
                {
                    template.ParseVariables("DEFAULT_SELECTED", " checked=\"checked\"");
                }

                for (int i = 0; i < 8; i++)
                {
                    VariableCollection themeVariableCollection = template.CreateChild("theme");
                    double baseHue = (50.0 + i * (360 / 8.0)) % 360.0;

                    System.Drawing.Color one = Display.HlsToRgb(baseHue, 0.5F, 0.5F); // background colour
                    System.Drawing.Color two = Display.HlsToRgb(baseHue, 0.6F, 0.2F); // link colour
                    System.Drawing.Color three = Display.HlsToRgb(baseHue, 0.4F, 0.85F); // box title colour
                    System.Drawing.Color four = Display.HlsToRgb(baseHue - 11F - 180F, 0.7F, 0.2F); // box border
                    System.Drawing.Color five = Display.HlsToRgb(baseHue - 11F - 180F, 0.4F, 0.85F); // box background colour

                    themeVariableCollection.ParseVariables("C_ONE", string.Format("{0:x2}{1:x2}{2:x2}", one.R, one.G, one.B));
                    themeVariableCollection.ParseVariables("C_TWO", string.Format("{0:x2}{1:x2}{2:x2}", two.R, two.G, two.B));
                    themeVariableCollection.ParseVariables("C_THREE", string.Format("{0:x2}{1:x2}{2:x2}", three.R, three.G, three.B));
                    themeVariableCollection.ParseVariables("C_FOUR", string.Format("{0:x2}{1:x2}{2:x2}", four.R, four.G, four.B));
                    themeVariableCollection.ParseVariables("C_FIVE", string.Format("{0:x2}{1:x2}{2:x2}", five.R, five.G, five.B));
                    themeVariableCollection.ParseVariables("HUE", ((int)baseHue).ToString());

                    if ((int)baseHue == css.Hue)
                    {
                        themeVariableCollection.ParseVariables("SELECTED", " checked=\"checked\"");
                    }
                }
            }

            if (editor == StyleGenerator.Standard)
            {
                template.ParseVariables("STANDARD_EDITOR", "TRUE");

                if (css.HasKey("body"))
                {
                    if (css["body"].HasProperty("background-color"))
                    {
                        template.ParseVariables("BACKGROUND_COLOUR", HttpUtility.HtmlEncode(css["body"]["background-color"].Value));
                    }
                    if (css["body"].HasProperty("color"))
                    {
                        template.ParseVariables("FORE_COLOUR", HttpUtility.HtmlEncode(css["body"]["color"].Value));
                    }
                    if (css["body"].HasProperty("background-image"))
                    {
                        template.ParseVariables("BACKGROUND_IMAGE", HttpUtility.HtmlEncode(css["body"]["background-image"].Value));
                    }
                    if (css["body"].HasProperty("background-repeat"))
                    {
                        backgroundRepeat = css["body"]["background-repeat"].Value;
                    }
                    if (css["body"].HasProperty("background-position"))
                    {
                        backgroundPosition = css["body"]["background-position"].Value;
                    }
                    if (css["body"].HasProperty("background-attachment"))
                    {
                        if (css["body"]["background-attachment"].Value == "fixed")
                        {
                            backgroundAttachment = true;
                        }
                        else
                        {
                            backgroundAttachment = false;
                        }
                    }
                }
                else if (css.HasKey("html"))
                {
                    if (css["html"].HasProperty("background-color"))
                    {
                        template.ParseVariables("BACKGROUND_COLOUR", HttpUtility.HtmlEncode(css["html"]["background-color"].Value));
                    }
                    if (css["html"].HasProperty("color"))
                    {
                        template.ParseVariables("FORE_COLOUR", HttpUtility.HtmlEncode(css["html"]["color"].Value));
                    }
                    if (css["html"].HasProperty("background-image"))
                    {
                        template.ParseVariables("BACKGROUND_IMAGE", HttpUtility.HtmlEncode(css["html"]["background-image"].Value));
                    }
                    if (css["html"].HasProperty("background-repeat"))
                    {
                        backgroundRepeat = css["html"]["background-repeat"].Value;
                    }
                    if (css["html"].HasProperty("background-position"))
                    {
                        backgroundPosition = css["html"]["background-position"].Value;
                    }
                    if (css["html"].HasProperty("background-attachment"))
                    {
                        if (css["html"]["background-attachment"].Value == "fixed")
                        {
                            backgroundAttachment = true;
                        }
                        else
                        {
                            backgroundAttachment = false;
                        }
                    }
                }

                if (css.HasKey("a"))
                {
                    if (css["a"].HasProperty("color"))
                    {
                        template.ParseVariables("LINK_COLOUR", HttpUtility.HtmlEncode(css["a"]["color"].Value));
                    }
                }

                if (css.HasKey("#pane-profile div.pane"))
                {
                    if (css["#pane-profile div.pane"].HasProperty("border-color"))
                    {
                        template.ParseVariables("BOX_BORDER_COLOUR", HttpUtility.HtmlEncode(css["#pane-profile div.pane"]["border-color"].Value));
                    }

                    if (css["#pane-profile div.pane"].HasProperty("background-color"))
                    {
                        template.ParseVariables("BOX_BACKGROUND_COLOUR", HttpUtility.HtmlEncode(css["#pane-profile div.pane"]["background-color"].Value));
                    }

                    if (css["#pane-profile div.pane"].HasProperty("color"))
                    {
                        template.ParseVariables("BOX_FORE_COLOUR", HttpUtility.HtmlEncode(css["#pane-profile div.pane"]["color"].Value));
                    }
                }

                if (css.HasKey("#pane-profile div.pane h3"))
                {
                    if (css["#pane-profile div.pane h3"].HasProperty("background-color"))
                    {
                        template.ParseVariables("BOX_H_BACKGROUND_COLOUR", HttpUtility.HtmlEncode(css["#pane-profile div.pane h3"]["background-color"].Value));
                    }

                    if (css["#pane-profile div.pane h3"].HasProperty("color"))
                    {
                        template.ParseVariables("BOX_H_FORE_COLOUR", HttpUtility.HtmlEncode(css["#pane-profile div.pane h3"]["color"].Value));
                    }
                }

                List<SelectBoxItem> repeatBoxItems = new List<SelectBoxItem>();
                repeatBoxItems.Add(new SelectBoxItem("no-repeat", "None", "/images/bg-no-rpt.png"));
                repeatBoxItems.Add(new SelectBoxItem("repeat-x", "Horizontal", "/images/bg-rpt-x.png"));
                repeatBoxItems.Add(new SelectBoxItem("repeat-y", "Vertical", "/images/bg-rpt-y.png"));
                repeatBoxItems.Add(new SelectBoxItem("repeat", "Tile", "/images/bg-rpt.png"));

                template.ParseVariables("S_BACKGROUND_REPEAT", Functions.BuildRadioArray("background-repeat", 2, repeatBoxItems, backgroundRepeat));

                List<SelectBoxItem> positionBoxItems = new List<SelectBoxItem>();
                positionBoxItems.Add(new SelectBoxItem("left top", "Top Left", "/images/bg-t-l.png"));
                positionBoxItems.Add(new SelectBoxItem("center top", "Top Centre", "/images/bg-t-c.png"));
                positionBoxItems.Add(new SelectBoxItem("right top", "Top Right", "/images/bg-t-r.png"));
                positionBoxItems.Add(new SelectBoxItem("left center", "Middle Left", "/images/bg-m-l.png"));
                positionBoxItems.Add(new SelectBoxItem("center center", "Middle Centre", "/images/bg-m-c.png"));
                positionBoxItems.Add(new SelectBoxItem("right center", "Middle Right", "/images/bg-m-r.png"));
                positionBoxItems.Add(new SelectBoxItem("left bottom", "Bottom Left", "/images/bg-b-l.png"));
                positionBoxItems.Add(new SelectBoxItem("center bottom", "Bottom Centre", "/images/bg-b-c.png"));
                positionBoxItems.Add(new SelectBoxItem("right bottom", "Bottom Right", "/images/bg-b-r.png"));

                template.ParseVariables("S_BACKGROUND_POSITION", Functions.BuildRadioArray("background-position", 3, positionBoxItems, backgroundPosition));

                if (backgroundAttachment)
                {
                    template.ParseVariables("BACKGROUND_IMAGE_FIXED", "checked=\"checked\"");
                }
            }

            if (editor == StyleGenerator.Advanced)
            {
                template.ParseVariables("ADVANCED_EDITOR", "TRUE");

                css.Generator = StyleGenerator.Advanced;
                template.ParseVariables("STYLE", HttpUtility.HtmlEncode(css.ToString())); //  + "\n----\n" + loggedInMember.GetUserStyle())
            }
        }

        public void StyleSave()
        {
            string mode = Request["mode"];

            CascadingStyleSheet css = new CascadingStyleSheet();

            switch (mode.ToLower())
            {
                case "theme":
                    css.Generator = StyleGenerator.Theme;

                    int baseHue = Functions.FormInt("theme", -1);

                    if (baseHue == -1)
                    {
                        css.Generator = StyleGenerator.Theme;
                        css.Hue = -1;
                    }
                    else
                    {
                        css.Generator = StyleGenerator.Theme;
                        css.Hue = baseHue;

                        System.Drawing.Color one = Display.HlsToRgb(baseHue, 0.5F, 0.5F); // background colour
                        System.Drawing.Color two = Display.HlsToRgb(baseHue, 0.6F, 0.2F); // link colour
                        System.Drawing.Color three = Display.HlsToRgb(baseHue, 0.4F, 0.85F); // box title colour
                        System.Drawing.Color four = Display.HlsToRgb(baseHue - 11F - 180F, 0.7F, 0.2F); // box border
                        System.Drawing.Color five = Display.HlsToRgb(baseHue - 11F - 180F, 0.4F, 0.85F); // box background colour

                        string backgroundColour = string.Format("#{0:x2}{1:x2}{2:x2}", one.R, one.G, one.B);
                        string linkColour = string.Format("#{0:x2}{1:x2}{2:x2}", two.R, two.G, two.B);
                        string boxTitleColour = string.Format("#{0:x2}{1:x2}{2:x2}", three.R, three.G, three.B);
                        string boxBorderColour = string.Format("#{0:x2}{1:x2}{2:x2}", four.R, four.G, four.B);
                        string boxBackgroundColour = string.Format("#{0:x2}{1:x2}{2:x2}", five.R, five.G, five.B);

                        css.AddStyle("body");
                        css["body"].SetProperty("background-color", backgroundColour);
                        css["body"].SetProperty("color", "#000000");

                        css.AddStyle("a");
                        css["a"].SetProperty("color", linkColour);

                        css.AddStyle("#pane-profile div.pane");
                        css["#pane-profile div.pane"].SetProperty("background-color", boxBackgroundColour);
                        css["#pane-profile div.pane"].SetProperty("border-color", boxBorderColour);
                        css["#pane-profile div.pane"].SetProperty("color", "#000000");

                        css.AddStyle("#profile div.pane");
                        css["#profile div.pane"].SetProperty("background-color", boxBackgroundColour);
                        css["#profile div.pane"].SetProperty("border-color", boxBorderColour);
                        css["#profile div.pane"].SetProperty("color", "#000000");

                        css.AddStyle("#overview-profile");
                        css["#overview-profile"].SetProperty("background-color", boxBackgroundColour);
                        css["#overview-profile"].SetProperty("border-color", boxBorderColour);
                        css["#overview-profile"].SetProperty("color", "#000000");

                        css.AddStyle("#pane-profile div.pane h3");
                        css["#pane-profile div.pane h3"].SetProperty("background-color", boxBorderColour);
                        css["#pane-profile div.pane h3"].SetProperty("border-color", boxBorderColour);
                        css["#pane-profile div.pane h3"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#pane-profile div.pane h3 a");
                        css["#pane-profile div.pane h3 a"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#profile div.pane h3");
                        css["#profile div.pane h3"].SetProperty("background-color", boxBorderColour);
                        css["#profile div.pane h3"].SetProperty("border-color", boxBorderColour);
                        css["#profile div.pane h3"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#profile div.pane h3 a");
                        css["#profile div.pane h3 a"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#overview-profile div.info");
                        css["#overview-profile div.info"].SetProperty("background-color", boxBorderColour);
                        css["#overview-profile div.info"].SetProperty("border-color", boxBorderColour);
                        css["#overview-profile div.info"].SetProperty("color", boxTitleColour);

                        css.AddStyle("#overview-profile div.info a");
                        css["#overview-profile div.info a"].SetProperty("color", boxTitleColour);
                    }

                    break;
                case "standard":
                    css.Generator = StyleGenerator.Standard;
                    css.AddStyle("body");
                    css["body"].SetProperty("background-color", Request.Form["background-colour"]);
                    css["body"].SetProperty("color", Request.Form["fore-colour"]);
                    if (!string.IsNullOrEmpty(Request.Form["background-image"]))
                    {
                        css["body"].SetProperty("background-image", "url('" + Request.Form["background-image"] + "')");
                        css["body"].SetProperty("background-repeat", Request.Form["background-repeat"]);
                        css["body"].SetProperty("background-position", Request.Form["background-position"]);
                        if (Request.Form["background-image-fixed"] == "true")
                        {
                            css["body"].SetProperty("background-attachment", "fixed");
                        }
                        else
                        {
                            css["body"].SetProperty("background-attachment", "scroll");
                        }
                    }

                    css.AddStyle("a");
                    css["a"].SetProperty("color", Request.Form["link-colour"]);

                    css.AddStyle("#pane-profile div.pane");
                    css["#pane-profile div.pane"].SetProperty("background-color", Request.Form["box-background-colour"]);
                    css["#pane-profile div.pane"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#pane-profile div.pane"].SetProperty("color", Request.Form["box-fore-colour"]);

                    css.AddStyle("#profile div.pane");
                    css["#profile div.pane"].SetProperty("background-color", Request.Form["box-background-colour"]);
                    css["#profile div.pane"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#profile div.pane"].SetProperty("color", Request.Form["box-fore-colour"]);

                    css.AddStyle("#overview-profile");
                    css["#overview-profile"].SetProperty("background-color", Request.Form["box-background-colour"]);
                    css["#overview-profile"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#overview-profile"].SetProperty("color", Request.Form["box-fore-colour"]);

                    css.AddStyle("#pane-profile div.pane h3");
                    css["#pane-profile div.pane h3"].SetProperty("background-color", Request.Form["box-h3-background-colour"]);
                    css["#pane-profile div.pane h3"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#pane-profile div.pane h3"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#pane-profile div.pane h3 a");
                    css["#pane-profile div.pane h3 a"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#profile div.pane h3");
                    css["#profile div.pane h3"].SetProperty("background-color", Request.Form["box-h3-background-colour"]);
                    css["#profile div.pane h3"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#profile div.pane h3"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#profile div.pane h3 a");
                    css["#profile div.pane h3 a"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#overview-profile div.info");
                    css["#overview-profile div.info"].SetProperty("background-color", Request.Form["box-h3-background-colour"]);
                    css["#overview-profile div.info"].SetProperty("border-color", Request.Form["box-border-colour"]);
                    css["#overview-profile div.info"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    css.AddStyle("#overview-profile div.info a");
                    css["#overview-profile div.info a"].SetProperty("color", Request.Form["box-h3-fore-colour"]);

                    break;
                case "advanced":
                    css.Generator = StyleGenerator.Advanced;
                    css.Parse(Request.Form["css-style"]);
                    break;
            }

            if (db.Query(string.Format("SELECT user_id FROM user_style WHERE user_id = {0}",
                loggedInMember.UserId)).Rows.Count == 0)
            {
                db.UpdateQuery(string.Format("INSERT INTO user_style (user_id, style_css) VALUES ({0}, '{1}');",
                    loggedInMember.UserId, Mysql.Escape(css.ToString())));
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE user_style SET style_css = '{1}' WHERE user_id = {0};",
                    loggedInMember.UserId, Mysql.Escape(css.ToString())));
            }

            SetRedirectUri(AccountModule.BuildModuleUri("profile", "style"));
            Display.ShowMessage("Style Saved", "Your profile style has been saved in the database.<br /><a href=\"/account/?module=profile&sub=style\">Return</a>");
        }

        public void Permissions(string submodule)
        {
            subModules.Add("permissions", "Profile Permissions");
            if (submodule != "permissions") return;

            if (Request.Form["save"] != null)
            {
                PermissionsSave();
                return;
            }

            //loggedInMember.LoadProfileInfo();

            List<string> permissions = new List<string>();
            permissions.Add("Can Read");
            permissions.Add("Can Comment");

            template.ParseVariables("S_PROFILE_PERMS", Functions.BuildPermissionsBox(loggedInMember.Permissions, permissions));
            template.SetTemplate("account_permissions.html");
        }

        public void PermissionsSave()
        {
            ushort permission = Functions.GetPermission();

            db.UpdateQuery(string.Format("UPDATE user_profile SET profile_access = {1} WHERE user_id = {0};",
                loggedInMember.UserId, permission));

            SetRedirectUri(AccountModule.BuildModuleUri("profile", "permissions"));
            Display.ShowMessage("Permissions Saved", "Your profile permissions have been saved in the database.<br /><a href=\"/account/?module=profile&sub=permissions\">Return</a>");
        }

        private void MyName(string submodule)
        {
            subModules.Add("name", "My Name");
            if (submodule != "name") return;

            if (Request.Form["save"] != null)
            {
                MyNameSave();
                return;
            }

            loggedInMember.LoadProfileInfo();

            template.SetTemplate("account_name.html");
            template.ParseVariables("DISPLAY_NAME", HttpUtility.HtmlEncode(loggedInMember.DisplayName));
            template.ParseVariables("FIRST_NAME", HttpUtility.HtmlEncode(loggedInMember.FirstName));
            template.ParseVariables("MIDDLE_NAME", HttpUtility.HtmlEncode(loggedInMember.MiddleName));
            template.ParseVariables("LAST_NAME", HttpUtility.HtmlEncode(loggedInMember.LastName));
            template.ParseVariables("SUFFIX", HttpUtility.HtmlEncode(loggedInMember.Suffix));

            string selected = " selected=\"selected\"";
            switch (loggedInMember.Title.ToLower().TrimEnd(new char[] { '.' }))
            {
                default:
                    template.ParseVariables("TITLE_NONE", selected);
                    break;
                case "master":
                    template.ParseVariables("TITLE_MASTER", selected);
                    break;
                case "mr":
                    template.ParseVariables("TITLE_MR", selected);
                    break;
                case "miss":
                    template.ParseVariables("TITLE_MISS", selected);
                    break;
                case "ms":
                    template.ParseVariables("TITLE_MS", selected);
                    break;
                case "mrs":
                    template.ParseVariables("TITLE_MRS", selected);
                    break;
                case "fr":
                    template.ParseVariables("TITLE_FR", selected);
                    break;
                case "sr":
                    template.ParseVariables("TITLE_SR", selected);
                    break;
                case "prof":
                    template.ParseVariables("TITLE_PROF", selected);
                    break;
                case "lord":
                    template.ParseVariables("TITLE_LORD", selected);
                    break;
            }
        }

        private void MyNameSave()
        {
            db.UpdateQuery(string.Format("UPDATE user_info SET user_name_display = '{1}' WHERE user_id = {0};",
                loggedInMember.UserId, Mysql.Escape(Request.Form["display"])));

            db.UpdateQuery(string.Format("UPDATE user_profile SET profile_name_first = '{1}', profile_name_last = '{2}', profile_name_middle = '{3}', profile_name_suffix = '{4}', profile_name_title = '{5}' WHERE user_id = {0};",
                loggedInMember.UserId, Mysql.Escape(Request.Form["firstname"]), Mysql.Escape(Request.Form["lastname"]), Mysql.Escape(Request.Form["middlename"]), Mysql.Escape(Request.Form["suffix"]), Mysql.Escape(Request.Form["title"])));

            Display.ShowMessage("Name Saved", "Your name has been saved in the database.<br /><a href=\"/account/?module=profile&sub=name\">Return</a>");
        }

        private void DisplayPicture(string submodule)
        {
            subModules.Add("display-picture", "Display Picture");
            if (submodule != "display-picture") return;

            if (Request.Form["save"] != null)
            {
                DisplayPictureSave();
                return;
            }

            loggedInMember.LoadProfileInfo();

            template.SetTemplate("Profile", "account_display_picture");

            template.ParseVariables("S_DISPLAY_PICTURE", HttpUtility.HtmlEncode(Linker.AppendSid("/account", true)));

            if (!string.IsNullOrEmpty(loggedInMember.UserThumbnail))
            {
                template.ParseVariables("I_DISPLAY_PICTURE", HttpUtility.HtmlEncode(loggedInMember.UserThumbnail));
            }
        }

        private void DisplayPictureSave()
        {
            AuthoriseRequestSid();

            string meSlug = "display-pictures";

            UserGallery profileGallery;
            try
            {
                profileGallery = new UserGallery(core, loggedInMember, meSlug);
            }
            catch (GalleryNotFoundException)
            {
                UserGallery root = new UserGallery(core, loggedInMember);
                profileGallery = UserGallery.Create(core, root, "Display Pictures", ref meSlug, "All my uploaded display pictures", 0);
            }

            if (profileGallery != null)
            {
                string title = "";
                string description = "";
                string slug = "";

                try
                {
                    slug = Request.Files["photo-file"].FileName;
                }
                catch
                {
                    Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                    return;
                }

                try
                {
                    string saveFileName = GalleryItem.HashFileUpload(Request.Files["photo-file"].InputStream);
                    if (!File.Exists(TPage.GetStorageFilePath(saveFileName)))
                    {
                        TPage.EnsureStoragePathExists(saveFileName);
                        Request.Files["photo-file"].SaveAs(TPage.GetStorageFilePath(saveFileName));
                    }

                    GalleryItem galleryItem = UserGalleryItem.Create(core, loggedInMember, profileGallery, title, ref slug, Request.Files["photo-file"].FileName, saveFileName, Request.Files["photo-file"].ContentType, (ulong)Request.Files["photo-file"].ContentLength, description, 0x3331, 0, Classifications.Everyone);

                    db.UpdateQuery(string.Format("UPDATE user_info SET user_icon = {0} WHERE user_id = {1}",
                        galleryItem.Id, loggedInMember.UserId));

                    SetRedirectUri(BuildUri("display-picture"));
                    Display.ShowMessage("Display Picture set", "You have successfully uploaded a new display picture.");
                    return;
                }
                catch (GalleryItemTooLargeException)
                {
                    Display.ShowMessage("Photo too big", "The photo you have attempted to upload is too big, you can upload photos up to 1.2 MiB in size.");
                    return;
                }
                catch (GalleryQuotaExceededException)
                {
                    Display.ShowMessage("Not Enough Quota", "You do not have enough quota to upload this photo. Try resizing the image before uploading or deleting images you no-longer need. Smaller images use less quota.");
                    return;
                }
                catch (InvalidGalleryItemTypeException)
                {
                    Display.ShowMessage("Invalid image uploaded", "You have tried to upload a file type that is not a picture. You are allowed to upload PNG and JPEG images.");
                    return;
                }
                catch (InvalidGalleryFileNameException)
                {
                    Display.ShowMessage("Submission failed", "Submission failed, try uploading with a different file name.");
                    return;
                }
            }
            else
            {
                Display.ShowMessage("Invalid submission", "You have made an invalid form submission.");
                return;
            }
        }

        private void Contact(string submodule)
        {
            subModules.Add("contact", "My Contact Details");
            if (submodule != "contact") return;

            if (Request.Form["save"] != null)
            {
                ContactSave();
                return;
            }

            template.SetTemplate("Profile", "account_contact");
        }

        private void ContactSave()
        {
        }

        public void SaveStatus(string submodule)
        {
            if (submodule != "status") return;

            AuthoriseRequestSid();

            string message = Request.Form["message"];

            StatusFeed.SaveMessage(core, message);

            Ajax.SendRawText("Success", message);
        }
    }
}
