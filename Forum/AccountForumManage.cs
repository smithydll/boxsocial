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
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.Forum
{
    [AccountSubModule(AppPrimitives.Group, "forum", "forum", true)]
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

        /// <summary>
        /// Initializes a new instance of the AccountForumManage class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountForumManage(Core core)
            : base(core)
        {
            this.Load += new EventHandler(AccountForumManage_Load);
            this.Show += new EventHandler(AccountForumManage_Show);
        }

        void AccountForumManage_Load(object sender, EventArgs e)
        {
            AddModeHandler("new", new ModuleModeHandler(AccountForumManage_New));
            AddModeHandler("edit", new ModuleModeHandler(AccountForumManage_New));
            AddModeHandler("delete", new ModuleModeHandler(AccountForumManage_Delete));
            AddSaveHandler("new", new EventHandler(AccountForumManage_New_Save));
            AddSaveHandler("edit", new EventHandler(AccountForumManage_Edit_Save));
            AddSaveHandler("delete", new EventHandler(AccountForumManage_Delete_Save));
            AddModeHandler("move-up", new ModuleModeHandler(AccountForumManage_Move));
            AddModeHandler("move-down", new ModuleModeHandler(AccountForumManage_Move));
        }

        void AccountForumManage_Show(object sender, EventArgs e)
        {
            SetTemplate("account_forum_manage");

            long forumId = core.Functions.RequestLong("id", 0);

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
                thisForum = new Forum(core, forumId);
            }

            if (thisForum != null)
            {
                template.Parse("FORUM_TITLE", thisForum.Title);
                template.Parse("U_FORUM", thisForum.Uri);

                if (thisForum.Id == 0)
                {
                    ForumSettings settings;
                    try
                    {
                        settings = new ForumSettings(core, thisForum.Owner);
                    }
                    catch (InvalidForumSettingsException)
                    {
                        ForumSettings.Create(core, thisForum.Owner);
                        settings = new ForumSettings(core, thisForum.Owner);
                    }
                    //ForumSettings settings = new ForumSettings(core, thisForum.Owner);
                    template.Parse("U_PERMISSIONS", core.Uri.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", settings.Id, ItemType.GetTypeId(typeof(ForumSettings))), true));
                }
                else
                {
                    template.Parse("U_PERMISSIONS", core.Uri.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", thisForum.Id, ItemType.GetTypeId(typeof(Forum))), true));
                }

                List<Forum> forums = thisForum.GetForums();

                foreach (Forum forum in forums)
                {
                    VariableCollection forumVariableCollection = template.CreateChild("forum_list");

                    forumVariableCollection.Parse("TITLE", forum.Title);
                    forumVariableCollection.Parse("U_SUB_FORUMS", BuildUri("forum", forum.Id));
                    forumVariableCollection.Parse("U_VIEW", forum.Uri);
                    forumVariableCollection.Parse("U_EDIT", BuildUri("forum", "edit", forum.Id));
                    forumVariableCollection.Parse("U_MOVE_UP", BuildUri("forum", "move-up", forum.Id));
                    forumVariableCollection.Parse("U_MOVE_DOWN", BuildUri("forum", "move-down", forum.Id));
                    forumVariableCollection.Parse("U_EDIT_PERMISSION", core.Uri.AppendAbsoluteSid(string.Format("/api/acl?id={0}&type={1}", forum.Id, ItemType.GetTypeId(typeof(Forum))), true));
                    forumVariableCollection.Parse("U_DELETE", BuildUri("forum", "delete", forum.Id));
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

            long id = core.Functions.RequestLong("id", 0);

            /* Forum Types SelectBox */
            SelectBox forumTypesSelectBox = new SelectBox("type");
            Dictionary<string, string> forumTypes = new Dictionary<string, string>();
            forumTypesSelectBox.Add(new SelectBoxItem("FORUM", "Forum"));
            forumTypesSelectBox.Add(new SelectBoxItem("CAT", "Category"));
            //forumTypes.Add("LINK", "Link");

            /* Forum Types SelectBox */
            SelectBox forumParentSelectBox = new SelectBox("parent");

            /* Title TextBox */
            TextBox titleTextBox = new TextBox("title");
            titleTextBox.MaxLength = 127;

            /* Description TextBox */
            TextBox descriptionTextBox = new TextBox("description");
            descriptionTextBox.IsFormatted = true;
            descriptionTextBox.Lines = 6;

            /* Rules TextBox */
            TextBox rulesTextBox = new TextBox("rules");
            rulesTextBox.IsFormatted = true;
            rulesTextBox.Lines = 6;

            ForumSettings settings = new ForumSettings(core, Owner);
            List<Forum> forums = settings.GetForums();

            forumParentSelectBox.Add(new SelectBoxItem("0", ""));
            foreach (Forum forum in forums)
            {
                string levelString = string.Empty;

                for (int i = 0; i < forum.Level; i++)
                {
                    levelString += "--";
                }

                SelectBoxItem item = new SelectBoxItem(forum.Id.ToString(), levelString + " " + forum.Title);

                if (forum.Id == id && e.Mode == "edit")
                {
                    item.Selectable = false;
                }

                forumParentSelectBox.Add(item);
            }

            switch (e.Mode)
            {
                case "new":
                    forumTypesSelectBox.SelectedKey = "FORUM";

                    template.Parse("S_ID", id.ToString());
                    forumParentSelectBox.SelectedKey = id.ToString();
				
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

                        titleTextBox.Value = forum.Title;
                        forumParentSelectBox.SelectedKey = forum.ParentId.ToString();
                        descriptionTextBox.Value = forum.Description;
                        rulesTextBox.Value = forum.Rules;

                        template.Parse("S_ID", forum.Id.ToString());

                        List<string> disabledItems = new List<string>();
                        forumTypesSelectBox["FORUM"].Selectable = false;
                        forumTypesSelectBox["CAT"].Selectable = false;
                        //forumTypesSelectBox["LINK"].Selectable = false;

                        forumTypesSelectBox.SelectedKey = type;

                        template.Parse("EDIT", "TRUE");
                    }
                    catch (InvalidForumException)
                    {
                        DisplayGenericError();
                    }
                    break;
            }

            /* Parse the form fields */
            template.Parse("S_TITLE", titleTextBox);
            template.Parse("S_DESCRIPTION", descriptionTextBox);
            template.Parse("S_RULES", rulesTextBox);
            template.Parse("S_FORUM_TYPE", forumTypesSelectBox);
            template.Parse("S_FORUM_PARENT", forumParentSelectBox);
        }

        void AccountForumManage_New_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            long parentId = core.Functions.FormLong("parent", 0);
            string title = core.Http.Form["title"];
            string description = core.Http.Form["description"];
            string rules = core.Http.Form["rules"];
            string type = core.Http.Form["type"];
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
                parent = new Forum(core, parentId);
            }

            if (parent != null)
            {
                try
                {
                    Forum forum = Forum.Create(core, parent, title, description, rules, 0x1111, isCategory);
                    if (parentId == 0)
                    {
                        SetRedirectUri(BuildUri("forum"));
                    }
                    else
                    {
                        SetRedirectUri(BuildUri("forum", forum.ParentId));
                    }
                    core.Display.ShowMessage("Forum Created", "A new forum has been created");
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
            AuthoriseRequestSid();

            long parentId = core.Functions.FormLong("parent", 0);
            long forumId = core.Functions.FormLong("id", 0);
            string title = core.Http.Form["title"];
            string description = core.Http.Form["description"];
            string rules = core.Http.Form["rules"];
            string type = core.Http.Form["type"];

            Forum forum;

            try
            {
                forum = new Forum(core, forumId);
            }
            catch (InvalidForumException)
            {
                DisplayGenericError();
                return;
            }

            /*if (parentId != forum.ParentId)
            {
                db.BeginTransaction();
                int order = 0;
                int level = 0;

                Forum parent = null;

                if (parentId > 0)
                {
                    parent = new Forum(core, parentId);
                }
                else
                {
                    parent = new Forum(core, Owner);
                }

                if (!parent.Owner.Equals(Owner))
                {
                    DisplayError("Cannot move forum to another owner");
                    return;
                }

                forum.ParentId = parentId;

                level = parent.Level + 1;

                forum.Level = level;

                SelectQuery query = new SelectQuery(typeof(Forum));
                query.AddFields("forum_order");
                query.AddCondition("forum_order", ConditionEquality.GreaterThan, parent.Order);
                query.AddCondition("forum_item_id", parent.Owner.Id);
                query.AddCondition("forum_item_type_id", parent.Owner.TypeId);
                query.AddCondition("forum_parent_id", parent.Id);
                query.AddSort(SortOrder.Descending, "forum_order");
                query.LimitCount = 1;

                DataTable orderTable = core.Db.Query(query);

                if (orderTable.Rows.Count == 1)
                {
                    int tableorder = (int)orderTable.Rows[0]["forum_order"];
                    if (forum.Order < tableorder)
                    {
                        order = tableorder;
                    }
                    else
                    {
                        order = tableorder + 1;
                    }
                }
                else
                {
                    if (forum.Order < parent.Order)
                    {
                        order = parent.Order;
                    }
                    else
                    {
                        order = parent.Order + 1;
                    }
                }

                if (order > forum.Order)
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(Forum));
                    uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Subtraction, 1));
                    uQuery.AddCondition("forum_order", ConditionEquality.GreaterThan, forum.Order);
                    uQuery.AddCondition("forum_order", ConditionEquality.LessThanEqual, order);
                    uQuery.AddCondition("forum_item_id", parent.Owner.Id);
                    uQuery.AddCondition("forum_item_type_id", parent.Owner.TypeId);

                    core.Db.Query(uQuery);
                }

                if (order < forum.Order)
                {
                    UpdateQuery uQuery = new UpdateQuery(typeof(Forum));
                    uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Addition, 1));
                    uQuery.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, order);
                    uQuery.AddCondition("forum_order", ConditionEquality.LessThan, forum.Order);
                    uQuery.AddCondition("forum_item_id", parent.Owner.Id);
                    uQuery.AddCondition("forum_item_type_id", parent.Owner.TypeId);

                    core.Db.Query(uQuery);
                }

                /*
                // decrement all items below old order in the order
                UpdateQuery uQuery = new UpdateQuery(typeof(Forum));
                uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Subtraction, 1));
                uQuery.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, forum.Order);
                uQuery.AddCondition("forum_item_id", parent.Owner.Id);
                uQuery.AddCondition("forum_item_type_id", parent.Owner.TypeId);

                core.Db.Query(uQuery);

                // increment all items below in the order
                uQuery = new UpdateQuery(typeof(Forum));
                uQuery.AddField("forum_order", new QueryOperation("forum_order", QueryOperations.Addition, 1));
                uQuery.AddCondition("forum_order", ConditionEquality.GreaterThanEqual, order);
                uQuery.AddCondition("forum_item_id", parent.Owner.Id);
                uQuery.AddCondition("forum_item_type_id", parent.Owner.TypeId);

                core.Db.Query(uQuery);/

                forum.Order = order;
            }*/

            forum.Title = title;
            forum.Description = description;
            forum.Rules = rules;
			//forum.Permissions = Functions.GetPermission();

            try
            {
                forum.Update();
            }
            catch (UnauthorisedToUpdateItemException)
            {
                DisplayGenericError();
                return;
            }

            if (forum.ParentId == 0)
            {
                SetRedirectUri(BuildUri("forum"));
            }
            else
            {
                SetRedirectUri(BuildUri("forum", forum.ParentId));
            }
            core.Display.ShowMessage("Forum updated", "You have updated the forum");
        }

        void AccountForumManage_Move(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            long forumId = core.Functions.RequestLong("id", 0);

            Forum forum;

            try
            {
                forum = new Forum(core, forumId);
            }
            catch (InvalidForumException)
            {
                DisplayGenericError();
                return;
            }

            switch (e.Mode)
            {
                case "move-up":
                    forum.MoveUp();
                    core.Display.ShowMessage("Forum moved up", "You have moved the forum up in the list.");
                    break;
                case "move-down":
                    forum.MoveDown();
                    core.Display.ShowMessage("Forum moved down", "You have moved the forum down in the list.");
                    break;
            }
        }

        void AccountForumManage_Delete(object sender, ModuleModeEventArgs e)
        {
            SetTemplate("account_forum_delete");

            long id = core.Functions.RequestLong("id", 0);

            template.Parse("S_ID", id);
        }

        void AccountForumManage_Delete_Save(object sender, EventArgs e)
        {

            long forumId = core.Functions.FormLong("id", 0);

            Forum forum;

            try
            {
                forum = new Forum(core, forumId);
            }
            catch (InvalidForumException)
            {
                DisplayGenericError();
                return;
            }

            forum.Delete();

            core.Display.ShowMessage("Forum deleted", "The forum has been deleted.");
        }
    }
}
