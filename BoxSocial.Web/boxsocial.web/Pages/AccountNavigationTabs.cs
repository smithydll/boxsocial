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

namespace BoxSocial.Applications.Pages
{
    [AccountSubModule(AppPrimitives.Group | AppPrimitives.Musician, "pages", "nav")]
    public class AccountNavigationTabs : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("NAVIGATION_TABS");
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
        /// Initializes a new instance of the AccountNavigationTabs class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountNavigationTabs(Core core, Primitive owner)
            : base(core, owner)
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

                tabVariableCollection.Parse("TITLE", tab.Page.Title);

                tabVariableCollection.Parse("U_MOVE_UP", BuildUri(Key, "move-up", tab.Id));
                tabVariableCollection.Parse("U_MOVE_DOWN", BuildUri(Key, "move-down", tab.Id));
                tabVariableCollection.Parse("U_DELETE", BuildUri(Key, "delete", tab.Id));
            }

            template.Parse("U_NEW_TAB", BuildUri(Key, "add"));
        }

        void AccountNavigationTabs_Delete(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long tabId = core.Functions.RequestLong("id", core.Functions.FormLong("id", 0));

            Dictionary<string, string> hiddenFields = new Dictionary<string, string>();
            hiddenFields.Add("key", ModuleKey);
            hiddenFields.Add("sub", Key);
            hiddenFields.Add("mode", "delete");

            if (core.Display.ShowConfirmBox(core.Hyperlink.AppendSid(Owner.AccountUriStub, true), "Delete?", "Are you sure you want to delete this tab?", hiddenFields) == ConfirmBoxResult.Yes)
            {
                try
                {
                    NagivationTab tab = new NagivationTab(core, tabId);

                    tab.Delete();

                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Tab Deleted", "You have successfully deleted the navigation tab.");
                }
                catch (InvalidNavigationTabException)
                {
                    DisplayGenericError();
                }
            }
            else
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Tab Not Deleted", "You have aborted deleting the navigation tab.");
            }
        }

        void AccountNavigationTabs_Move(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long tabId = core.Functions.RequestLong("id", 0);

            try
            {
                NagivationTab tab = new NagivationTab(core, tabId);

                switch (e.Mode)
                {
                    case "move-up":
                        tab.MoveUp();

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Tab Moved", "You have successfully moved the tab up one place.");
                        break;
                    case "move-down":
                        tab.MoveDown();

                        SetRedirectUri(BuildUri());
                        core.Display.ShowMessage("Tab Moved", "You have successfully moved the tab down one place.");
                        break;
                }
            }
            catch (InvalidNavigationTabException)
            {
                DisplayGenericError();
            }
        }

        void AccountNavigationTabs_Add(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_navigation_tab_add");

            Pages myPages = new Pages(core, Owner);
            List<Page> pagesList = myPages.GetPages(false, true);

            Dictionary<string, string> pages = new Dictionary<string, string>();
            List<string> disabledItems = new List<string>();

            foreach (Page page in pagesList)
            {
                pages.Add(page.Id.ToString(), page.FullPath);
            }

            ParseSelectBox("S_PAGE_LIST", "page-id", pages, "");
        }

        void AccountNavigationTabs_Add_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long pageId = core.Functions.FormLong("page-id", 0);

            try
            {
                Page thePage = new Page(core, Owner, pageId);

                NagivationTab tab = NagivationTab.Create(core, thePage);

                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Tab Created", "You have successfully created a navigation tab.");
            }
            catch (InvalidNavigationTabException)
            {
                DisplayGenericError();
            }
            catch (PageNotFoundException)
            {
                DisplayGenericError();
            }
        }
    }
}
