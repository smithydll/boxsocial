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

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule("forum", "forum", true)]
    public class AccountForumManage : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Manage Forum";
            }
        }

        public override int Order
        {
            get
            {
                return 1;
            }
        }

        public AccountForumManage()
        {
            this.Load += new EventHandler(AccountForumManage_Load);
            this.Show += new EventHandler(AccountForumManage_Show);
        }

        void AccountForumManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("new", new ModuleModeHandler(AccountForumManage_New));
            AddModeHandler("edit", new ModuleModeHandler(AccountForumManage_New));
            AddSaveHandler("new", new EventHandler(AccountForumManage_New_Save));
            AddSaveHandler("edit", new EventHandler(AccountForumManage_Edit_Save));
        }

        void AccountForumManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_forum_manage");

            long forumId = Functions.RequestLong("id", 0);

            Forum thisForum;
            if (forumId == 0)
            {
                if (Owner is UserGroup)
                {
                    thisForum = new Forum(core, (UserGroup)Owner);
                }
                else
                {
                    thisForum = null;
                }
            }
            else
            {
                if (Owner is UserGroup)
                {
                    thisForum = new Forum(core, (UserGroup)Owner, forumId);
                }
                else
                {
                    thisForum = new Forum(core, forumId);
                }
            }

            if (thisForum != null)
            {
                List<Forum> forums = thisForum.GetForums();

                foreach (Forum forum in forums)
                {
                    VariableCollection forumVariableCollection = template.CreateChild("forum_list");

                    forumVariableCollection.Parse("TITLE", forum.Title);
                    forumVariableCollection.Parse("U_SUB_FORUMS", BuildUri("forum", forum.Id));
                }
            }

            if (forumId > 0)
            {
                template.Parse("U_CREATE_FORUM", BuildUri("forum", "new", forumId));
            }
            else
            {
                template.Parse("U_CREATE_FORUM", BuildUri("forum", "new"));
            }
        }

        void AccountForumManage_New(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_forum_edit");

            long id = Functions.RequestLong("id", 0);

            Dictionary<string, string> forumTypes = new Dictionary<string, string>();
            forumTypes.Add("FORUM", "Forum");
            forumTypes.Add("CAT", "Category");
            //forumTypes.Add("LINK", "Link");

            switch (e.Mode)
            {
                case "new":
                    template.Parse("S_ID", id.ToString());

                    Display.ParseSelectBox(template, "S_FORUM_TYPE", "type", forumTypes, "FORUM");
                    break;
                case "edit":
                    try
                    {
                        Forum forum = new Forum(core, id);

                        string type = "FORUM";

                        if (forum.IsCategory)
                        {
                            type = "CAT";
                        }

                        template.Parse("S_TITLE", forum.Title);
                        template.Parse("S_DESCRIPTION", forum.Description);
                        template.Parse("S_ID", forum.Id.ToString());
                        Display.ParseSelectBox(template, "S_FORUM_TYPE", "type", forumTypes, type);

                        template.Parse("EDIT", "TRUE");
                    }
                    catch (InvalidForumException)
                    {
                        DisplayGenericError();
                    }
                    break;
            }
        }

        void AccountForumManage_New_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long parentId = Functions.FormLong("id", 0);
            string title = Request.Form["title"];
            string description = Request.Form["description"];
            string type = Request.Form["type"];
            bool isCategory = (type == "CAT");

            Forum parent;

            if (parentId == 0)
            {
                if (Owner is UserGroup)
                {
                    parent = new Forum(core, (UserGroup)Owner);
                }
                else
                {
                    parent = null;
                }
            }
            else
            {
                if (Owner is UserGroup)
                {
                    parent = new Forum(core, (UserGroup)Owner, parentId);
                }
                else
                {
                    parent = new Forum(core, parentId);
                }
            }

            if (parent != null)
            {
                try
                {
                    Forum forum = Forum.Create(core, parent, title, description, 0x1111, isCategory);
                    if (parentId == 0)
                    {
                        SetRedirectUri(BuildUri("forum"));
                    }
                    else
                    {
                        SetRedirectUri(BuildUri("forum", forum.ParentId));
                    }
                    Display.ShowMessage("Forum Created", "A new forum has been created");
                }
                catch (UnauthorisedToCreateItemException)
                {
                    DisplayGenericError();
                }
                catch (InvalidForumException)
                {
                    DisplayGenericError();
                }
            }
        }

        void AccountForumManage_Edit_Save(object sender, EventArgs e)
        {
        }
    }
}
