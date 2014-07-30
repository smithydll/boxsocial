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

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule(AppPrimitives.Group, "forum", "members")]
	public class AccountForumMemberManage : AccountSubModule
	{
		public override string Title
        {
            get
            {
                return "Manage Forum Members";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountForumMemberManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountForumMemberManage(Core core, Primitive owner)
            : base(core, owner)
		{
			this.Load += new EventHandler(AccountForumMemberManage_Load);
            this.Show += new EventHandler(AccountForumMemberManage_Show);
		}
		
		void AccountForumMemberManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit", new ModuleModeHandler(AccountForumMemberManage_Edit));
            AddSaveHandler("edit", new EventHandler(AccountForumMemberManage_Edit_Save));
		}
		
		void AccountForumMemberManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_forum_member_manage");

            Dictionary<long, ForumMember> members = ForumMember.GetMembers(core, Owner, core.Functions.GetFilter(), core.TopLevelPageNumber, 20);

            foreach (ForumMember member in members.Values)
            {
                VariableCollection membersVariableCollection = template.CreateChild("members");
                
                membersVariableCollection.Parse("DISPLAY_NAME", member.DisplayName);
				membersVariableCollection.Parse("POSTS", member.ForumPosts.ToString());
				membersVariableCollection.Parse("U_EDIT", BuildUri("members", "edit", member.Id));
            }
		}

        void AccountForumMemberManage_Edit(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_forum_member_edit");

            long id = core.Functions.RequestLong("id", 0);
            ForumMember member = null;

            /* Signature TextBox */
            TextBox signatureTextBox = new TextBox("signature");
            signatureTextBox.IsFormatted = true;
            //signatureTextBox.IsDisabled = true;
            signatureTextBox.Lines = 7;

            /* Ranks SelectBox */
            SelectBox ranksSelectBox = new SelectBox("ranks");

            try
            {
                member = new ForumMember(core, Owner, id, UserLoadOptions.All);
            }
            catch (InvalidForumMemberException)
            {
                core.Functions.Generate404();
            }
            catch (InvalidUserException)
            {
                core.Functions.Generate404();
            }

            ranksSelectBox.Add(new SelectBoxItem("0", "None"));

            Dictionary<long, ForumMemberRank> ranks = ForumMemberRank.GetRanks(core, Owner);

            foreach (ForumMemberRank rank in ranks.Values)
            {
                ranksSelectBox.Add(new SelectBoxItem(rank.Id.ToString(), rank.RankTitleText));
            }

            if (ranksSelectBox.ContainsKey(member.ForumRankId.ToString()))
            {
                ranksSelectBox.SelectedKey = member.ForumRankId.ToString();
            }

            signatureTextBox.Value = member.ForumSignature;

            /* Parse the form fields */
            template.Parse("S_USERNAME", member.UserName);
            template.Parse("S_RANK", ranksSelectBox);
            template.Parse("S_SIGNATURE", signatureTextBox);
			template.Parse("S_ID", id.ToString());
        }

        void AccountForumMemberManage_Edit_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = core.Functions.FormLong("id", 0);
            long rankId = core.Functions.FormLong("ranks", 0);
            ForumMember member = null;

            try
            {
                member = new ForumMember(core, Owner, id, UserLoadOptions.Common);
            }
            catch (InvalidForumMemberException)
            {
                core.Functions.Generate404();
            }
            catch (InvalidUserException)
            {
                core.Functions.Generate404();
            }

            member.ForumSignature = core.Http.Form["signature"];
            member.ForumRankId = rankId;

            member.Update(typeof(ForumMember));
			
			SetRedirectUri(BuildUri());
            core.Display.ShowMessage("Forum Profile Updated", "The user's forum profile has been saved in the database");
        }
	}
}
