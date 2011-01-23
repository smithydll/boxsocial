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
    [AccountSubModule(AppPrimitives.Member, "music", "my-musicians")]
    public class AccountMyMusicians : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "My Musicians";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountMyMusicians()
        {
            this.Load += new EventHandler(AccountMyMusicians_Load);
            this.Show += new EventHandler(AccountMyMusicians_Show);
        }

        void AccountMyMusicians_Load(object sender, EventArgs e)
        {
            
        }

        void AccountMyMusicians_Show(object sender, EventArgs e)
        {
            SetTemplate("account_my_musicians");

            template.Parse("U_REGISTER_MUSICIAN", core.Uri.AppendSid("/music/register"));

            SelectQuery query = Musician.GetSelectQueryStub(MusicianLoadOptions.Common);
            query.AddJoin(JoinTypes.Inner, new DataField(typeof(Musician), "musician_id"), new DataField(typeof(MusicianMember), "musician_id"));
            query.AddCondition("user_id", LoggedInMember.Id);
            query.AddSort(SortOrder.Ascending, "musician_slug");

            DataTable musicianDataTable = db.Query(query);

            List<Musician> musicians = new List<Musician>();

            foreach (DataRow dr in musicianDataTable.Rows)
            {
                musicians.Add(new Musician(core, dr, MusicianLoadOptions.Common));
            }

            foreach (Musician musician in musicians)
            {
                VariableCollection musicianVariableCollection = template.CreateChild("musician_list");

                musicianVariableCollection.Parse("DISPLAY_NAME", musician.DisplayName);
                musicianVariableCollection.Parse("U_MUSICIAN", musician.Uri);
                musicianVariableCollection.Parse("FANS", core.Functions.LargeIntegerToString(musician.Fans));
            }
        }

    }
}
