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
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [AccountSubModule(AppPrimitives.Musician, "music", "tour", true)]
    public class AccountTourManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Tours";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountTourManage()
        {
            this.Load += new EventHandler(AccountTourManage_Load);
            this.Show += new EventHandler(AccountTourManage_Show);
        }

        void AccountTourManage_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("new", AccountTourManage_Edit);
            this.AddModeHandler("edit", AccountTourManage_Edit);
        }

        void AccountTourManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_tour_manage");

            List<Tour> tours = ((Musician)Owner).GetTours();

            foreach (Tour tour in tours)
            {
                VariableCollection tourVariableCollection = template.CreateChild("tour_list");

                tourVariableCollection.Parse("ID", tour.Id.ToString());
                tourVariableCollection.Parse("TITLE", tour.Title);
                tourVariableCollection.Parse("GIGS", tour.Gigs.ToString());
                tourVariableCollection.Parse("U_EDIT", BuildUri("tour", "edit", tour.Id));
                tourVariableCollection.Parse("U_ADD_GIG", BuildUri("gig", "add", tour.Id));
                tourVariableCollection.Parse("U_DELETE", BuildUri("tour", "delete", tour.Id));
            }

            template.Parse("U_ADD_TOUR", BuildUri("tour", "add"));
        }

        void AccountTourManage_Edit(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_tour_edit");
            
            Tour tour = null;

            /* */
            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 127;

            /* */
            SelectBox yearSelectBox = new SelectBox("year");

            for (int i = 1990; i < DateTime.UtcNow.Year + 5; i++)
            {
                yearSelectBox.Add(new SelectBoxItem(i.ToString(), i.ToString()));
            }

            switch (e.Mode)
            {
                case "new":
                    break;
                case "edit":
                    long tourId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));

                    try
                    {
                        tour = new Tour(core, tourId);
                    }
                    catch (InvalidTourException)
                    {
                        return;
                    }

                    titleTextBox.Value = tour.Title;

                    if (yearSelectBox.ContainsKey(tour.StartYear.ToString()))
                    {
                        yearSelectBox.SelectedKey = tour.StartYear.ToString();
                    }

                    template.Parse("S_ID", "TRUE");
                    template.Parse("EDIT", tour.Id.ToString());

                    break;
            }

            if (core.Http.Form["title"] != null)
            {
                titleTextBox.Value = core.Http.Form["title"];
            }
            yearSelectBox.SelectedKey = core.Functions.FormShort("year", short.Parse(yearSelectBox.SelectedKey)).ToString();

            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_YEAR", yearSelectBox);

            SaveItemMode(AccountTourManage_EditSave, tour);
        }

        void AccountTourManage_EditSave(object sender, ItemModuleModeEventArgs e)
        {
            AuthoriseRequestSid();
            string title = core.Http.Form["title"];
            short year = core.Functions.FormShort("year", (short)DateTime.UtcNow.Year);

            switch (e.Mode)
            {
                case "new":
                    try
                    {
                        Tour.Create(core, (Musician)Owner, title, year);
                    }
                    catch (InvalidTourException)
                    {
                    }

                    break;
                case "edit":
                    Tour tour = (Tour)e.Item;

                    if (tour == null)
                    {
                        return;
                    }

                    tour.Title = title;
                    tour.StartYear = year;

                    tour.Update();
                    break;
            }
        }
    }
}
