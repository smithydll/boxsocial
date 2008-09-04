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

namespace BoxSocial.Applications.Pages
{
    [AccountSubModule(AppPrimitives.Group, "pages", "nav")]
    public class AccountNavigationTabs : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Navigation Tabs";
            }
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        public AccountNavigationTabs()
        {
            this.Load += new EventHandler(AccountNavigationTabs_Load);
            this.Show += new EventHandler(AccountNavigationTabs_Show);
        }

        void AccountNavigationTabs_Load(object sender, EventArgs e)
        {
            AddModeHandler("delete", new ModuleModeHandler(AccountNavigationTabs_Delete));
            AddModeHandler("add", new ModuleModeHandler(AccountNavigationTabs_Add));
            AddSaveHandler("add", new EventHandler(AccountNavigationTabs_Add_Save));
            AddModeHandler("move-up", new ModuleModeHandler(AccountNavigationTabs_Move));
            AddModeHandler("move-down", new ModuleModeHandler(AccountNavigationTabs_Move));
        }

        void AccountNavigationTabs_Show(object sender, EventArgs e)
        {
            SetTemplate("account_navigation_tabs");

            List<NagivationTab> tabs = NagivationTab.GetTabs(core, Owner);

            foreach (NagivationTab tab in tabs)
            {
                VariableCollection tabVariableCollection = template.CreateChild("tab_list");

                tabVariableCollection.Parse("U_MOVE_UP", BuildUri(Key, "move-up", tab.Id));
                tabVariableCollection.Parse("U_MOVE_DOWN", BuildUri(Key, "move-down", tab.Id));
                tabVariableCollection.Parse("U_DELETE", BuildUri(Key, "delete", tab.Id));
            }
        }

        void AccountNavigationTabs_Delete(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long tabId = Functions.RequestLong("id", 0);
        }

        void AccountNavigationTabs_Move(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long tabId = Functions.RequestLong("id", 0);
        }

        void AccountNavigationTabs_Add(object sender, ModuleModeEventArgs e)
        {
        }

        void AccountNavigationTabs_Add_Save(object sender, EventArgs e)
        {
        }
    }
}
