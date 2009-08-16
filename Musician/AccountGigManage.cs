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

        public AccountGigManage()
        {
            this.Load += new EventHandler(AccountGigManage_Load);
            this.Show += new EventHandler(AccountGigManage_Show);
        }

        void AccountGigManage_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("add", AccountGigManage_Edit);
            this.AddModeHandler("edit", AccountGigManage_Edit);
        }

        void AccountGigManage_Show(object sender, EventArgs e)
        {
            List<Gig> gigs = null;
            long tourId = Functions.RequestLong("id", 0);

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
                gigVariableCollection.Parse("DATE", core.tz.DateTimeToString(gig.GetTime(core.tz)));
            }
        }

        void AccountGigManage_Edit(object sender, ModuleModeEventArgs e)
        {
        }

        void AccountGigManage_EditSave(object sender, ModuleModeEventArgs e)
        {

        }
    }
}
