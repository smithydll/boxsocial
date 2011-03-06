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
        public AccountMembersManage(Core core)
            : base (core)
        {
            this.Load += new EventHandler(AccountMembersManage_Load);
            this.Show += new EventHandler(AccountMembersManage_Show);
        }

        void AccountMembersManage_Load(object sender, EventArgs e)
        {
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
                    memberVariableCollection.Parse("U_LEAVE", "{TODO}");
                }
            }
        }
    }
}
