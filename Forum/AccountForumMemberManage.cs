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
		
		public AccountForumMemberManage()
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

            int page = Functions.RequestInt("p", 0);

            Dictionary<long, ForumMember> members = ForumMember.GetMembers(core, Owner, Functions.GetFilter(), page, 20);

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

            long id = Functions.RequestLong("id", 0);
            ForumMember member = null;

            try
            {
                member = new ForumMember(core, Owner, id, UserLoadOptions.All);
            }
            catch (InvalidForumMemberException)
            {
                Functions.Generate404();
            }
            catch (InvalidUserException)
            {
                Functions.Generate404();
            }

            SelectBox ranksSelectBox = new SelectBox("ranks");
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

            template.Parse("S_USERNAME", member.UserName);
            template.Parse("S_RANK", ranksSelectBox);
            template.Parse("S_SIGNATURE", member.ForumSignature);
			template.Parse("S_ID", id.ToString());
        }

        void AccountForumMemberManage_Edit_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long id = Functions.FormLong("id", 0);
            long rankId = Functions.FormLong("rank", 0);
            ForumMember member = null;

            try
            {
                member = new ForumMember(core, Owner, id, UserLoadOptions.Common);
            }
            catch (InvalidForumMemberException)
            {
                Functions.Generate404();
            }
            catch (InvalidUserException)
            {
                Functions.Generate404();
            }

            member.ForumSignature = Request.Form["signature"];
            member.ForumRankId = rankId;
            member.Update(typeof(ForumMember));
			
			SetRedirectUri(BuildUri());
			Display.ShowMessage("Forum Profile Updated", "The user's forum profile has been saved in the database");
        }
	}
}
