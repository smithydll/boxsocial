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
using BoxSocial.Groups;

namespace BoxSocial.Applications.News
{
    [AccountSubModule(AppPrimitives.Group, "news", "icon")]
    public class AccountNewsIconManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Icons";
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
        /// Initializes a new instance of the AccountNewsIconManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountNewsIconManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountNewsIconManage_Load);
            this.Show += new EventHandler(AccountNewsIconManage_Show);
        }

        void AccountNewsIconManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("edit", AccountNewsIconManage_Edit);
            AddSaveHandler("edit", AccountNewsIconManage_Save);
            AddModeHandler("delete", AccountNewsIconManage_Delete);
        }

        void AccountNewsIconManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_news_icon_manage");

            News news = new News(core, Owner);
            List<NewsIcon> icons = news.GetIcons();

            foreach (NewsIcon icon in icons)
            {
                VariableCollection articlesVariableCollection = template.CreateChild("icon_list");


                articlesVariableCollection.Parse("TITLE", icon.Title);

                articlesVariableCollection.Parse("I_ICON", icon.Uri);

                articlesVariableCollection.Parse("U_EDIT", BuildUri("icon", "edit", icon.Id));
                articlesVariableCollection.Parse("U_DELETE", BuildUri("icon", "delete", icon.Id));

            }
        }

        void AccountNewsIconManage_Edit(object sender, EventArgs e)
        {
            long iconId = 0;
            bool edit = false;
            try
            {
                iconId = long.Parse(core.Http.Query["id"]);
            }
            catch
            {
                core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                return;
            }

            SetTemplate("account_news_icon_edit");

            News news = new News(core, Owner);
            NewsIcon icon = null;

            if (iconId > 0)
            {
                edit = true;
                try
                {
                    icon = new NewsIcon(core, iconId);
                }
                catch (InvalidNewsIconException)
                {
                    core.Display.ShowMessage("Invalid", "If you have stumbled onto this page by mistake, click back in your browser.");
                }


                template.Parse("ICON_TITLE", icon.Title);
            }
        }

        void AccountNewsIconManage_Save(object sender, EventArgs e)
        {
        }

        void AccountNewsIconManage_Delete(object sender, EventArgs e)
        {
        }
    }
}
