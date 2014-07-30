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

namespace BoxSocial.Applications.Blog
{
    [AccountSubModule("blog", "blogroll")]
    public class AccountBlogRoll : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("Blog", "EDIT_BLOG_ROLL");
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
        /// Initializes a new instance of the AccountBlogRoll class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountBlogRoll(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountBlogRoll_Load);
            this.Show += new EventHandler(AccountBlogRoll_Show);
        }

        void AccountBlogRoll_Load(object sender, EventArgs e)
        {
            AddModeHandler("new", new ModuleModeHandler(AccountBlogRoll_New));
            AddModeHandler("edit", new ModuleModeHandler(AccountBlogRoll_New));
            AddModeHandler("delete", new ModuleModeHandler(AccountBlogRoll_Delete));
            AddSaveHandler("delete", new EventHandler(AccountBlogRoll_DeleteSave));
        }

        void AccountBlogRoll_Show(object sender, EventArgs e)
        {
            SetTemplate("account_blog_roll");

            Blog myBlog;
            try
            {
                myBlog = new Blog(core, session.LoggedInMember);
            }
            catch (InvalidBlogException)
            {
                myBlog = Blog.Create(core);
            }

            List<BlogRollEntry> blogRollEntries = myBlog.GetBlogRoll();

            template.Parse("BLOG_ROLL_ENTRIES", blogRollEntries.Count.ToString());
            template.Parse("U_NEW_BLOG_ROLL", core.Hyperlink.BuildAccountSubModuleUri(ModuleKey, Key, "new"));

            foreach (BlogRollEntry bre in blogRollEntries)
            {
                VariableCollection breVariableCollection = template.CreateChild("blog_roll_list");

                if (!string.IsNullOrEmpty(bre.Title))
                {
                    breVariableCollection.Parse("TITLE", bre.Title);
                }
                else if (bre.User != null)
                {
                    breVariableCollection.Parse("TITLE", bre.User.DisplayName);
                }

                breVariableCollection.Parse("URI", bre.Uri);
                breVariableCollection.Parse("U_EDIT", core.Hyperlink.BuildAccountSubModuleUri(ModuleKey, Key, "edit", bre.Id));
                breVariableCollection.Parse("U_DELETE", core.Hyperlink.BuildAccountSubModuleUri(ModuleKey, Key, "delete", bre.Id));
            }
        }

        void AccountBlogRoll_New(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_blog_roll_new");

            BlogRollEntry bre = null;

            long id = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
            string title = core.Http.Form["title"];
            string uri = core.Http.Form["uri"];

            if (e.Mode == "new")
            {
            }
            else if (e.Mode == "edit")
            {
                if (id > 0)
                {
                    try
                    {
                        bre = new BlogRollEntry(core, id);

                        title = bre.Title;
                        uri = bre.EntryUri;

                        template.Parse("S_ID", id.ToString());
                        template.Parse("EDIT", "TRUE");
                    }
                    catch (InvalidBlogEntryException)
                    {
                        DisplayGenericError();
                        return;
                    }
                }
                else
                {
                    DisplayGenericError();
                    return;
                }
            }

            template.Parse("S_TITLE", title);
            template.Parse("S_URI", uri);

            SaveItemMode(new ItemModuleModeHandler(AccountBlogRoll_SaveNew), bre);
        }

        void AccountBlogRoll_SaveNew(object sender, ItemModuleModeEventArgs e)
        {
            AuthoriseRequestSid();
            string title = core.Http.Form["title"];
            string uri = core.Http.Form["uri"];

            if (string.IsNullOrEmpty(title))
            {
                SetError("Title cannot be empty");
                return;
            }

            if (string.IsNullOrEmpty(uri))
            {
                SetError("URI cannot be empty");
                return;
            }

            if (e.Mode == "new")
            {
                BlogRollEntry bre = BlogRollEntry.Create(core, title, uri);

                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Blog Roll Entry Created", "The blog roll entry has been created");
            }
            else if (e.Mode == "edit")
            {
                long id = core.Functions.FormLong("id", 0);
                BlogRollEntry bre = (BlogRollEntry)e.Item;

                if (bre == null)
                {
                    DisplayGenericError();
                    return;
                }

                bre.Title = title;
                bre.EntryUri = uri;
                bre.Update();

                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Blog Roll Entry Saved", "The blog roll entry has been saved");
            }
        }

        void AccountBlogRoll_Delete(object sender, EventArgs e)
        {
            long id = core.Functions.RequestLong("id", 0);
            BlogRollEntry bre = null;

            try
            {
                bre = new BlogRollEntry(core, id);
            }
            catch (InvalidBlogRollEntryException)
            {
                DisplayGenericError();
                return;
            }

            Dictionary<string, string> hiddenFieldList = new Dictionary<string, string>();
            hiddenFieldList.Add("module", ModuleKey);
            hiddenFieldList.Add("sub", Key);
            hiddenFieldList.Add("mode", "delete");
            hiddenFieldList.Add("id", bre.Id.ToString());

            core.Display.ShowConfirmBox(core.Hyperlink.AppendSid(Owner.AccountUriStub, true),
                "Confirm",
                "Do you really want to delete this blog roll entry?",
                hiddenFieldList);
        }

        void AccountBlogRoll_DeleteSave(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            if (core.Display.GetConfirmBoxResult() == ConfirmBoxResult.Yes)
            {
                long id = core.Functions.FormLong("id", 0);
                try
                {
                    BlogRollEntry bre = new BlogRollEntry(core, id);

                    bre.Delete();
                    SetRedirectUri(BuildUri());
                    core.Display.ShowMessage("Blog Roll Entry Deleted", "The blog roll entry has been deleted");
                }
                catch (InvalidBlogEntryException)
                {
                    DisplayGenericError();
                    return;
                }
                catch (UnauthorisedToDeleteItemException)
                {
                }
            }
            else
            {
                SetRedirectUri(BuildUri());
                core.Display.ShowMessage("Blog Roll Entry Not Deleted", "The blog roll entry has not been deleted");
            }
        }
    }
}
