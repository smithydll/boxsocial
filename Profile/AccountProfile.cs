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

namespace BoxSocial
{
    public class AccountProfile : AccountModule
    {

        public AccountProfile(Account account)
            : base(account)
        {
            RegisterSubModule += new RegisterSubModuleHandler(Info);
            RegisterSubModule += new RegisterSubModuleHandler(MyName);
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

        public override string Key
        {
            get
            {
                return "profile";
            }
        }

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

            template.SetTemplate("account_lifestyle.html");

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

            DataTable religionsTable = db.SelectQuery("SELECT * FROM religions ORDER BY religion_title ASC");

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

            DataTable countriesTable = db.SelectQuery("SELECT * FROM countries ORDER BY country_name ASC");

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

            template.SetTemplate("account_style.html");

            template.ParseVariables("STYLE", HttpUtility.HtmlEncode(loggedInMember.GetUserStyle()));
        }

        public void StyleSave()
        {
            if (db.SelectQuery(string.Format("SELECT user_id FROM user_style WHERE user_id = {0}",
                loggedInMember.UserId)).Rows.Count == 0)
            {
                db.UpdateQuery(string.Format("INSERT INTO user_style (user_id, style_css) VALUES ({0}, '{1}');",
                    loggedInMember.UserId, Mysql.Escape(Request.Form["css-style"])));
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE user_style SET style_css = '{1}' WHERE user_id = {0};",
                    loggedInMember.UserId, Mysql.Escape(Request.Form["css-style"])));
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
