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
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [AccountSubModule(AppPrimitives.Musician, "music", "gig", true)]
    public class AccountGigManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Gigs";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountGigManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountGigManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountGigManage_Load);
            this.Show += new EventHandler(AccountGigManage_Show);
        }

        void AccountGigManage_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("add", AccountGigManage_Edit);
            this.AddModeHandler("edit", AccountGigManage_Edit);
            this.AddModeHandler("delete", AccountGigManage_Delete);
        }

        void AccountGigManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_gigs_manage");

            List<Gig> gigs = null;
            long tourId = core.Functions.RequestLong("id", 0);

            if (tourId > 0)
            {
                Tour tour = new Tour(core, tourId);

                gigs = tour.GetGigs();
            }
            else
            {
                gigs = ((Musician)Owner).GetGigs();
            }

            foreach (Gig gig in gigs)
            {
                VariableCollection gigVariableCollection = template.CreateChild("gig_list");

                gigVariableCollection.Parse("CITY", gig.City);
                gigVariableCollection.Parse("VENUE", gig.Venue);
                gigVariableCollection.Parse("DATE", core.Tz.DateTimeToString(gig.GetTime(core.Tz)));
            }

            template.Parse("U_ADD_GIG", BuildUri("gig", "add"));
        }

        void AccountGigManage_Edit(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_gig_edit");

            /* */
            TextBox cityTextBox = new TextBox("city");
            cityTextBox.MaxLength = 31;
            /* */
            TextBox venueTextBox = new TextBox("venue");
            venueTextBox.MaxLength = 63;
            /* */
            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 31;
            /* */
            TextBox abstractTextBox = new TextBox("abstract");
            abstractTextBox.IsFormatted = true;
            abstractTextBox.Lines = 5;
            /* */
            CheckBox allAgesCheckBox = new CheckBox("all-ages");
            allAgesCheckBox.Caption = core.Prose.GetString("Musician", "IS_ALL_AGES");
            /* */
            SelectBox tourSelectBox = new SelectBox("tour");
            tourSelectBox.Add(new SelectBoxItem("0", "No Tour"));

            SelectBox dateYearsSelectBox = new SelectBox("date-year");
            SelectBox dateMonthsSelectBox = new SelectBox("date-month");
            SelectBox dateDaysSelectBox = new SelectBox("date-day");

            /* */
            DateTimePicker dateDateTimePicker = new DateTimePicker(core, "date");
            dateDateTimePicker.ShowTime = true;
            dateDateTimePicker.ShowSeconds = false;

            /* */
            SelectBox timezoneSelectBox = UnixTime.BuildTimeZoneSelectBox("timezone");

            for (int i = DateTime.Now.AddYears(-110).Year; i < DateTime.Now.AddYears(-13).Year; i++)
            {
                dateYearsSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            for (int i = 1; i < 13; i++)
            {
                dateMonthsSelectBox.Add(new SelectBoxItem(i.ToString(), core.Functions.IntToMonth(i)));
            }

            for (int i = 1; i < 32; i++)
            {
                dateDaysSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            List<Tour> tours = Tour.GetAll(core, (Musician)Owner);

            foreach (Tour tour in tours)
            {
                tourSelectBox.Add(new SelectBoxItem(tour.Id.ToString(), tour.Title));
            }

            switch (e.Mode)
            {
                case "add":
                    long tourId = core.Functions.RequestLong("id", 0);

                    if (tourSelectBox.ContainsKey(tourId.ToString()))
                    {
                        tourSelectBox.SelectedKey = tourId.ToString();
                    }

                    dateDateTimePicker.Value = core.Tz.Now;
                    timezoneSelectBox.SelectedKey = core.Tz.TimeZoneCode.ToString();

                    break;
                case "edit":
                    long gigId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
                    Gig gig = null;

                    try
                    {
                        gig = new Gig(core, gigId);

                        cityTextBox.Value = gig.City;
                        venueTextBox.Value = gig.Venue;
                        titleTextBox.Value = gig.Title;
                        abstractTextBox.Value = gig.Abstract;
                        allAgesCheckBox.IsChecked = gig.AllAges;
                        dateYearsSelectBox.SelectedKey = gig.GetTime(gig.TimeZone).Year.ToString();
                        dateMonthsSelectBox.SelectedKey = gig.GetTime(gig.TimeZone).Month.ToString();
                        dateDaysSelectBox.SelectedKey = gig.GetTime(gig.TimeZone).Day.ToString();

                        dateDateTimePicker.Value = gig.GetTime(gig.TimeZone);
                        timezoneSelectBox.SelectedKey = gig.TimeZone.TimeZoneCode.ToString();

                        if (tourSelectBox.ContainsKey(gig.TourId.ToString()))
                        {
                            tourSelectBox.SelectedKey = gig.TourId.ToString();
                        }
                    }
                    catch (InvalidGigException)
                    {
                        return;
                    }
                    break;
            }

            template.Parse("S_CITY", cityTextBox);
            template.Parse("S_VENUE", venueTextBox);
            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_ABSTRACT", abstractTextBox);
            template.Parse("S_ALLAGES", allAgesCheckBox);
            template.Parse("S_TOURS", tourSelectBox);
            template.Parse("S_DATE", dateDateTimePicker);
            template.Parse("S_TIMEZONE", timezoneSelectBox);

            SaveMode(AccountGigManage_EditSave);
        }

        void AccountGigManage_EditSave(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            string city = Functions.TrimStringToWord(core.Http.Form["city"], 31);
            string venue = Functions.TrimStringToWord(core.Http.Form["venue"], 63);
            string gigAbstract = core.Http.Form["abstract"];
            long tourId = core.Functions.FormLong("tour", 0);
            bool allAges = true;
            ushort timezone = core.Functions.FormUShort("timezone", 1);
            long time = DateTimePicker.FormDate(core, "date", timezone);

            Tour tour = null;
            Gig gig = null;

            if (tourId > 0)
            {
                try
                {
                    tour = new Tour(core, tourId);

                    if (tour.Musician.Id != Owner.Id)
                    {
                        tour = null;
                        tourId = 0;
                        // TODO: throw exception
                        return;
                    }
                }
                catch (InvalidTourException)
                {
                    tour = null;
                    tourId = 0;
                    // TODO: throw exception
                    return;
                }
            }

            switch (e.Mode)
            {
                case "add":

                    // TODO;
                    gig = Gig.Create(core, (Musician)Owner, tour, time, timezone, city, venue, gigAbstract, allAges);

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Gig created", "Your gig has been created");
                    break;
                case "edit":
                    long gigId = core.Functions.FormLong("id", 0);

                    try
                    {
                        gig = new Gig(core, gigId);
                    }
                    catch (InvalidGigException)
                    {
                        // TODO: throw exception
                        return;
                    }

                    if (gig.Musician.Id != Owner.Id)
                    {
                        // TODO: throw exception
                        return;
                    }

                    gig.City = city;
                    gig.Venue = venue;
                    gig.Abstract = gigAbstract;
                    gig.AllAges = allAges;
                    gig.TourId = tourId;

                    gig.Update();

                    SetInformation("Gig information updated");
                    break;
            }
        }

        void AccountGigManage_Delete(object sender, ModuleModeEventArgs e)
        {
        }

        void AccountGigManage_DeleteSave(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();
        }
    }
}
