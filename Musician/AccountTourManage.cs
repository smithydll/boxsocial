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
            this.AddModeHandler("add", AccountTourManage_Edit);
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
                songsVariableCollection.Parse("U_DELETE", BuildUri("tour", "delete", tour.Id));
            }

            template.Parse("U_ADD_TOUR", BuildUri("tour", "add"));
        }

        void AccountTourManage_Edit(object sender, ModuleModeEventArgs e)
        {

            SaveMode(AccountTourManage_EditSave);
        }

        void AccountTourManage_EditSave(object sender, ModuleModeEventArgs e)
        {

        }
    }
}
