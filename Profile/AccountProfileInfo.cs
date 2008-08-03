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

namespace BoxSocial.Applications.Profile
{
    [AccountSubModule("profile", "info", true)]
    public class AccountProfileInfo : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "My Information";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountProfileInfo()
        {
            this.Load += new EventHandler(AccountProfileInfo_Load);
            this.Show += new EventHandler(AccountProfileInfo_Show);
        }

        void AccountProfileInfo_Load(object sender, EventArgs e)
        {
        }

        void AccountProfileInfo_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_profile.html");

            string selected = " checked=\"checked\"";
            switch (LoggedInMember.GenderRaw)
            {
                case "UNDEF":
                    template.Parse("S_GENDER_UNDEF", selected);
                    break;
                case "MALE":
                    template.Parse("S_GENDER_MALE", selected);
                    break;
                case "FEMALE":
                    template.Parse("S_GENDER_FEMALE", selected);
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

            SelectQuery query = new SelectQuery("countries");
            query.AddFields("*");
            query.AddSort(SortOrder.Ascending, "country_name");

            DataTable countriesTable = db.Query(query);

            countries.Add("", "Unspecified");
            foreach (DataRow countryRow in countriesTable.Rows)
            {
                countries.Add((string)countryRow["country_iso"], (string)countryRow["country_name"]);
            }

            Display.ParseSelectBox(template, "S_DOB_YEAR", "dob-year", dobYears, LoggedInMember.DateOfBirth.Year.ToString());
            Display.ParseSelectBox(template, "S_DOB_MONTH", "dob-month", dobMonths, LoggedInMember.DateOfBirth.Month.ToString());
            Display.ParseSelectBox(template, "S_DOB_DAY", "dob-day", dobDays, LoggedInMember.DateOfBirth.Day.ToString());
            Display.ParseSelectBox(template, "S_COUNTRY", "country", countries, LoggedInMember.CountryIso);
            template.Parse("S_AUTO_BIOGRAPHY", LoggedInMember.Autobiography);

            Save(new EventHandler(AccountProfileInfo_Save));
        }

        void AccountProfileInfo_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string dob = string.Format("{0:0000}-{1:00}-{2:00}",
                Request.Form["dob-year"], Request.Form["dob-month"], Request.Form["dob-day"]);

            LoggedInMember.Profile.DateOfBirth = DateTime.Parse(dob);
            LoggedInMember.Profile.CountryIso = Request.Form["country"];
            LoggedInMember.Profile.GenderRaw = Request.Form["gender"];
            LoggedInMember.Profile.Autobiography = Request.Form["auto-biography"];

            LoggedInMember.Profile.Update();

            SetRedirectUri(BuildUri());
            Display.ShowMessage("Information Saved", "Your information has been saved in the database.");
        }
    }
}
