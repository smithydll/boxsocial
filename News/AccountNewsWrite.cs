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

namespace BoxSocial.Applications.News
{
    [AccountSubModule(AppPrimitives.Group, "news", "write")]
    public class AccountNewsWrite : AccountSubModule
    {

        public override string Title
        {
            get
            {
                return "Write New News Article";
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
        /// Initializes a new instance of the AccountNewsWrite class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountNewsWrite(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountNewsWrite_Load);
            this.Show += new EventHandler(AccountNewsWrite_Show);
        }

        void AccountNewsWrite_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountNewsWrite_Delete));
        }

        void AccountNewsWrite_Show(object sender, EventArgs e)
        {
            SetTemplate("account_news_article_write");

            Save(new EventHandler(AccountNewsWrite_Save));
            if (core.Http.Form["publish"] != null)
            {
                AccountNewsWrite_Save(this, new EventArgs());
            }
        }

        void AccountNewsWrite_Save(object sender, EventArgs e)
        {
            string subject = core.Http.Form["title"];
            string body = core.Http.Form["post"];

            Article newArticle = Article.Create(core, Owner, subject, body);

            SetRedirectUri(BuildUri("manage"));
            core.Display.ShowMessage("Article Published", "The news article has been published.");
        }

        void AccountNewsWrite_Delete(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            SetRedirectUri(BuildUri("manage"));
        }
    }
}
