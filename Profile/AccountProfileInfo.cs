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
using BoxSocial.Forms;
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

            SelectBox dobYearsSelectBox = new SelectBox("dob-year");

            for (int i = DateTime.Now.AddYears(-110).Year; i < DateTime.Now.AddYears(-13).Year; i++)
            {
                dobYearsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            if (LoggedInMember.DateOfBirth != null)
            {
                dobYearsSelectBox.SelectedKey = LoggedInMember.DateOfBirth.Year.ToString();
            }

            SelectBox dobMonthsSelectBox = new SelectBox("dob-month");

            for (int i = 1; i < 13; i++)
            {
                dobMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), Functions.IntToMonth(i)));
            }

            if (LoggedInMember.DateOfBirth != null)
            {
                dobMonthsSelectBox.SelectedKey = LoggedInMember.DateOfBirth.Month.ToString();
            }

            SelectBox dobDaysSelectBox = new SelectBox("dob-day");

            for (int i = 1; i < 32; i++)
            {
                dobDaysSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            if (LoggedInMember.DateOfBirth != null)
            {
                dobDaysSelectBox.SelectedKey = LoggedInMember.DateOfBirth.Day.ToString();
            }

            SelectBox countriesSelectBox = new SelectBox("country");

            SelectQuery query = new SelectQuery("countries");
            query.AddFields("*");
            query.AddSort(SortOrder.Ascending, "country_name");

            DataTable countriesTable = db.Query(query);

            countriesSelectBox.Add(new SelectBoxItem("", "Unspecified"));
            foreach (DataRow countryRow in countriesTable.Rows)
            {
                countriesSelectBox.Add(new SelectBoxItem((string)countryRow["country_iso"], (string)countryRow["country_name"]));
            }

			if (LoggedInMember.CountryIso != null)
			{
				countriesSelectBox.SelectedKey = LoggedInMember.CountryIso;
			}

            template.Parse("S_DOB_YEAR", dobYearsSelectBox);
            template.Parse("S_DOB_MONTH", dobMonthsSelectBox);
            template.Parse("S_DOB_DAY", dobDaysSelectBox);
            template.Parse("S_COUNTRY", countriesSelectBox);

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
