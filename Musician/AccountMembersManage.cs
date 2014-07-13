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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [AccountSubModule(AppPrimitives.Musician, "music", "members", true)]
    public class AccountMembersManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Members";
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
        /// Initializes a new instance of the AccountMembersManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountMembersManage(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountMembersManage_Load);
            this.Show += new EventHandler(AccountMembersManage_Show);
        }

        void AccountMembersManage_Load(object sender, EventArgs e)
        {
            this.AddModeHandler("edit", new ModuleModeHandler(AccountMembersManage_Edit));
            this.AddModeHandler("leave", new ModuleModeHandler(AccountMembersManage_Leave));
        }

        void AccountMembersManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_members_manage");

            Musician musician = (Musician)Owner;
            List<MusicianMember> members = musician.GetMembers();

            foreach (MusicianMember member in members)
            {
                VariableCollection memberVariableCollection = template.CreateChild("member_list");

                memberVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
                memberVariableCollection.Parse("DATE_JOINED", core.Tz.DateTimeToString(member.GetMemberDate(core.Tz)));

                if (member.Id == LoggedInMember.Id)
                {
                    memberVariableCollection.Parse("U_EDIT", BuildUri("members", "edit", member.Id));
                    memberVariableCollection.Parse("U_LEAVE", BuildUri("members", "leave", member.Id));
                }
            }
        }

        void AccountMembersManage_Edit(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_member_profile");
        }

        void AccountMembersManage_Leave(object sender, ModuleModeEventArgs e)
        {
            MusicianMember member = new MusicianMember(core, (Musician)Owner, core.Functions.RequestLong("id", 0));

            if (member.Musician.Id != Owner.Id)
            {
                return;
            }

            if (member.Id == LoggedInMember.Id)
            {
            }
        }
    }
}
