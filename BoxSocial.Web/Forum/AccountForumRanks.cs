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

        /// <summary>
        /// Initializes a new instance of the AccountForumRanks class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountForumRanks(Core core, Primitive owner)
            : base(core, owner)
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
            AddModeHandler("delete", new ModuleModeHandler(AccountForumRanks_Delete));
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

            /* Title TextBox */
            TextBox titleTextBox = new TextBox("rank-title");

            /* Minimum Posts (to attain rank) TextBox */
            TextBox minPostsTextBox = new TextBox("min-posts");

            /* Special Rank TextBox */
            CheckBox specialCheckBox = new CheckBox("special");

            if (e.Mode == "edit")
            {
                template.Parse("EDIT", "TRUE");
                long id = core.Functions.RequestLong("id", 0);

                if (id == 0)
                {
                    core.Functions.Generate404();
                    return;
                }

                try
                {
                    ForumMemberRank fmr = new ForumMemberRank(core, id);

                    titleTextBox.Value = fmr.RankTitleText;
                    minPostsTextBox.Value = fmr.RankPosts.ToString();
                    specialCheckBox.IsChecked = fmr.RankSpecial;

                    template.Parse("S_ID", fmr.RankId.ToString());
                }
                catch (InvalidForumMemberRankException)
                {
                    core.Functions.Generate404();
                    return;
                }
            }

            /* Parse the form fields */
            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_MINIMUM_POSTS", minPostsTextBox);
            template.Parse("S_SPECIAL", specialCheckBox);
        }

        void AccountForumRanks_Add_Save(object sender, EventArgs e)
        {
			AuthoriseRequestSid();
			
			string title = core.Http.Form["rank-title"];
			long rankId = core.Functions.FormLong("id", 0);
			int posts = core.Functions.FormInt("min-posts", 0);
			bool special = (core.Http.Form["special"] != null);
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

        void AccountForumRanks_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long rankId = core.Functions.RequestLong("id", 0);

            if (rankId > 0)
            {
                ForumMemberRank theRank = new ForumMemberRank(core, rankId);

                switch (core.Display.GetConfirmBoxResult())
                {
                    case ConfirmBoxResult.None:
                        Dictionary<string, string> hiddenFieldList = GetModeHiddenFieldList();
                        hiddenFieldList.Add("id", theRank.Id.ToString());

                        core.Display.ShowConfirmBox(HttpUtility.HtmlEncode(core.Hyperlink.AppendSid(Owner.AccountUriStub, true)),
                            "Delete the rank?",
                            "Do you really want to delete this rank?",
                            hiddenFieldList);

                        return;
                    case ConfirmBoxResult.Yes:
                        // Delete the rank
                        theRank.Delete();

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Forum rank deleted", "You have deleted this forum rank from the database.");
                        break;
                    case ConfirmBoxResult.No:
                        // don't do anything
                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Delete cancelled", "The forum rank has not been deleted from the database.");
                        break;
                }
            }
        }
    }
}
