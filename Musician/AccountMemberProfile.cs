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
    [AccountSubModule(AppPrimitives.Musician, "music", "my-profile")]
    public class AccountMemberProfile : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "My Profile";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountMemberProfile()
        {
            this.Load += new EventHandler(AccountMemberProfile_Load);
            this.Show += new EventHandler(AccountMemberProfile_Show);
        }

        void AccountMemberProfile_Load(object sender, EventArgs e)
        {
        }

        void AccountMemberProfile_Show(object sender, EventArgs e)
        {
            SetTemplate("account_member_profile");

            MusicianMember member = null;

            /* */
            TextBox stageNameTextBox = new TextBox("stage-name");
            stageNameTextBox.MaxLength = 63;

            /* */
            TextBox biographyTextBox = new TextBox("biography");
            biographyTextBox.IsFormatted = true;
            biographyTextBox.Lines = 7;

            try
            {
                member = new MusicianMember(core, (Musician)Owner, LoggedInMember);

                stageNameTextBox.Value = member.StageName;
                biographyTextBox.Value = member.Biography;
            }
            catch (InvalidMusicianMemberException)
            {
                return;
            }

            template.Parse("S_STAGENAME", stageNameTextBox);
            template.Parse("S_BIOGRAPHY", biographyTextBox);

            Save(new EventHandler(AccountMemberProfile_Save));
        }

        void AccountMemberProfile_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            MusicianMember member = new MusicianMember(core, (Musician)Owner, LoggedInMember);

            member.StageName = core.Http.Form["stage-name"];
            member.Biography = core.Http.Form["biography"];

            member.Update(typeof(MusicianMember));

            SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Profile Updated", "Your musician member profile has been updated.");
        }
    }
}
