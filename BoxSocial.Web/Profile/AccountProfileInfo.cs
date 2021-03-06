﻿/*
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

        /// <summary>
        /// Initializes a new instance of the AccountProfileInfo class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountProfileInfo(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountProfileInfo_Load);
            this.Show += new EventHandler(AccountProfileInfo_Show);
        }

        void AccountProfileInfo_Load(object sender, EventArgs e)
        {
        }

        void AccountProfileInfo_Show(object sender, EventArgs e)
        {
            SetTemplate("account_profile");

            RadioList genderRadioList = new RadioList("gender");
            genderRadioList.Add(new RadioListItem(genderRadioList.Name, ((byte)Gender.Undefined).ToString(), core.Prose.GetString("NONE_SPECIFIED")));
            genderRadioList.Add(new RadioListItem(genderRadioList.Name, ((byte)Gender.Male).ToString(), core.Prose.GetString("MALE")));
            genderRadioList.Add(new RadioListItem(genderRadioList.Name, ((byte)Gender.Female).ToString(), core.Prose.GetString("FEMALE")));
            genderRadioList.Add(new RadioListItem(genderRadioList.Name, ((byte)Gender.Intersex).ToString(), core.Prose.GetString("INTERSEX")));
            genderRadioList.SelectedKey = ((byte)LoggedInMember.Profile.GenderRaw).ToString();
            genderRadioList.Layout = Layout.Horizontal;

            TextBox heightTextBox = new TextBox("height");
            heightTextBox.MaxLength = 3;
            heightTextBox.Width = new StyleLength(3F, LengthUnits.Em);
            heightTextBox.Value = LoggedInMember.Profile.Height.ToString();

            SelectBox dobYearsSelectBox = new SelectBox("dob-year");

            for (int i = DateTime.Now.AddYears(-110).Year; i < DateTime.Now.AddYears(-13).Year; i++)
            {
                dobYearsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            if (LoggedInMember.Profile.DateOfBirth != null)
            {
                dobYearsSelectBox.SelectedKey = LoggedInMember.Profile.DateOfBirth.Year.ToString();
            }

            SelectBox dobMonthsSelectBox = new SelectBox("dob-month");

            for (int i = 1; i < 13; i++)
            {
                dobMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), core.Functions.IntToMonth(i)));
            }

            if (LoggedInMember.Profile.DateOfBirth != null)
            {
                dobMonthsSelectBox.SelectedKey = LoggedInMember.Profile.DateOfBirth.Month.ToString();
            }

            SelectBox dobDaysSelectBox = new SelectBox("dob-day");

            for (int i = 1; i < 32; i++)
            {
                dobDaysSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            if (LoggedInMember.Profile.DateOfBirth != null)
            {
                dobDaysSelectBox.SelectedKey = LoggedInMember.Profile.DateOfBirth.Day.ToString();
            }

            SelectBox countriesSelectBox = new SelectBox("country");

            SelectQuery query = new SelectQuery("countries");
            query.AddFields("*");
            query.AddSort(SortOrder.Ascending, "country_name");

            System.Data.Common.DbDataReader countriesReader = db.ReaderQuery(query);

            countriesSelectBox.Add(new SelectBoxItem("", "Unspecified"));

            while (countriesReader.Read())
            {
                countriesSelectBox.Add(new SelectBoxItem((string)countriesReader["country_iso"], (string)countriesReader["country_name"]));
            }

            countriesReader.Close();
            countriesReader.Dispose();

			if (LoggedInMember.Profile.CountryIso != null)
			{
				countriesSelectBox.SelectedKey = LoggedInMember.Profile.CountryIso;
			}

            template.Parse("S_GENDER", genderRadioList);

            template.Parse("S_DOB_YEAR", dobYearsSelectBox);
            template.Parse("S_DOB_MONTH", dobMonthsSelectBox);
            template.Parse("S_DOB_DAY", dobDaysSelectBox);
            template.Parse("S_COUNTRY", countriesSelectBox);
            template.Parse("S_HEIGHT", heightTextBox);

            template.Parse("S_AUTO_BIOGRAPHY", LoggedInMember.Profile.Autobiography);

            Save(new EventHandler(AccountProfileInfo_Save));
        }

        void AccountProfileInfo_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            string dob = string.Format("{0:0000}-{1:00}-{2:00}",
                core.Http.Form["dob-year"], core.Http.Form["dob-month"], core.Http.Form["dob-day"]);

            LoggedInMember.Profile.DateOfBirth = DateTime.Parse(dob);
            LoggedInMember.Profile.CountryIso = core.Http.Form["country"];
            LoggedInMember.Profile.GenderRaw = (Gender)byte.Parse(core.Http.Form["gender"]);
            LoggedInMember.Profile.Autobiography = core.Http.Form["auto-biography"];
            LoggedInMember.Profile.Height = core.Functions.FormByte("height", 0);

            LoggedInMember.Profile.Update();

            SetInformation("Your information has been saved in the database.");
            //SetRedirectUri(BuildUri());
            //core.Display.ShowMessage("Information Saved", "Your information has been saved in the database.");
        }
    }
}
