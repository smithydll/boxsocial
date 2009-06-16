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

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule(AppPrimitives.Group, "forum", "ranks")]
    public class AccountForumRanks : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Ranks";
            }
        }

        public override int Order
        {
            get
            {
                return 3;
            }
        }

        public AccountForumRanks()
        {
            this.Load += new EventHandler(AccountForumRanks_Load);
            this.Show += new EventHandler(AccountForumRanks_Show);
        }

        void AccountForumRanks_Load(object sender, EventArgs e)
        {
            AddModeHandler("add", new ModuleModeHandler(AccountForumRanks_Add));
            AddSaveHandler("add", new EventHandler(AccountForumRanks_Add_Save));
            AddModeHandler("edit", new ModuleModeHandler(AccountForumRanks_Add));
            AddSaveHandler("edit", new EventHandler(AccountForumRanks_Add_Save));
        }

        void AccountForumRanks_Show(object sender, EventArgs e)
        {
            SetTemplate("account_forum_ranks");

            ForumSettings settings = new ForumSettings(core, Owner);
            List<ForumMemberRank> ranks = settings.GetRanks();

            foreach (ForumMemberRank rank in ranks)
            {
                VariableCollection ranksVariableCollection = template.CreateChild("rank_list");

                ranksVariableCollection.Parse("RANK", rank.RankTitleText);
                ranksVariableCollection.Parse("SPECIAL", (rank.RankSpecial) ? "True" : "False");
                ranksVariableCollection.Parse("MINIMUM_POSTS", rank.RankPosts.ToString());

                ranksVariableCollection.Parse("U_EDIT", BuildUri("ranks", "edit", rank.Id));
                ranksVariableCollection.Parse("U_DELETE", BuildUri("ranks", "delete", rank.Id));
            }
			
			template.Parse("U_NEW_RANK", BuildUri("ranks", "add"));
        }

        void AccountForumRanks_Add(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_forum_rank_edit");

            if (e.Mode == "edit")
            {
                template.Parse("EDIT", "TRUE");
                long id = Functions.RequestLong("id", 0);

                if (id == 0)
                {
                    core.Functions.Generate404();
                    return;
                }

                try
                {
                    ForumMemberRank fmr = new ForumMemberRank(core, id);

                    template.Parse("S_ID", fmr.RankId.ToString());
                    template.Parse("S_TITLE", fmr.RankTitleText);
                    template.Parse("S_MINIMUM_POSTS", fmr.RankPosts.ToString());
                    template.Parse("S_SPECIAL", (fmr.RankSpecial) ? "checked=\"checked\" " : "");

                    return;
                }
                catch (InvalidForumMemberRankException)
                {
                    core.Functions.Generate404();
                    return;
                }
            }

            template.Parse("S_TITLE", "");
            template.Parse("S_MINIMUM_POSTS", "0");
            template.Parse("S_SPECIAL", "");
        }

        void AccountForumRanks_Add_Save(object sender, EventArgs e)
        {
			AuthoriseRequestSid();
			
			string title = Request.Form["rank-title"];
			long rankId = Functions.FormLong("id", 0);
			int posts = Functions.FormInt("min-posts", 0);
			bool special = (Request.Form["special"] == "true");
			int colour = -1;
			
			if (rankId > 0)
			{
				// Edit
				ForumMemberRank theRank = new ForumMemberRank(core, rankId);
				theRank.RankTitleText = title;
				theRank.RankPosts = posts;
				theRank.RankSpecial = special;
				theRank.RankColourRaw = colour;
				
				theRank.Update();
				
				SetRedirectUri(BuildUri("ranks"));
                core.Display.ShowMessage("New Updated", "The rank has been updated.");
			}
			else
			{
				// New Rank
				ForumMemberRank newRank = ForumMemberRank.Create(core, Owner, title, posts, special, colour);
				SetRedirectUri(BuildUri("ranks"));
                core.Display.ShowMessage("New Rank Created", "The new rank has been created.");
			}
        }
    }
}
