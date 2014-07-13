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

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule(AppPrimitives.Group, "forum", "settings")]
    public class AccountForumSettings : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Settings";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AccountForumSettings class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountForumSettings(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountForumSettings_Load);
            this.Show += new EventHandler(AccountForumSettings_Show);
        }

        void AccountForumSettings_Load(object sender, EventArgs e)
        {
        }

        void AccountForumSettings_Show(object sender, EventArgs e)
        {
            SetTemplate("account_forum_settings");
			
			Save(new EventHandler(AccountForumSettings_Save));

            ForumSettings settings;
            try
            {
                settings = new ForumSettings(core, Owner);
            }
            catch (InvalidForumSettingsException)
            {
                ForumSettings.Create(core, Owner);
                settings = new ForumSettings(core, Owner);
            }
            //ForumSettings settings = new ForumSettings(core, Owner);

            template.Parse("S_TOPICS_PER_PAGE", settings.TopicsPerPage.ToString());
            template.Parse("S_POSTS_PER_PAGE", settings.PostsPerPage.ToString());
        }

        void AccountForumSettings_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            ForumSettings settings = new ForumSettings(core, Owner);
            settings.TopicsPerPage = core.Functions.FormInt("topics-per-page", 10);
            settings.PostsPerPage = core.Functions.FormInt("posts-per-page", 10);

            settings.Update();
			
			this.SetInformation("Forum Settings Saved");
        }
    }
}
