﻿/*
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

namespace BoxSocial.Groups
{
    [AccountSubModule(AppPrimitives.Group, "groups", "style", true)]
    public class AccountGroupStyle : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return "Group Style";
            }
        }

        public override int Order
        {
            get
            {
                return 2;
            }
        }

        public AccountGroupStyle()
        {
            this.Load += new EventHandler(AccountGroupStyle_Load);
            this.Show += new EventHandler(AccountGroupStyle_Show);
        }

        void AccountGroupStyle_Load(object sender, EventArgs e)
        {
        }

        void AccountGroupStyle_Show(object sender, EventArgs e)
        {
            SetTemplate("account_group_style");

            if (Owner.GetType() != typeof(UserGroup))
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = (UserGroup)Owner;

            CascadingStyleSheet css = new CascadingStyleSheet();
            css.Parse(thisGroup.Info.Style);

            template.Parse("STYLE", css.ToString());

            Save(new EventHandler(AccountGroupStyle_Save));
        }

        void AccountGroupStyle_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            if (Owner.GetType() != typeof(UserGroup))
            {
                DisplayGenericError();
                return;
            }

            UserGroup thisGroup = (UserGroup)Owner;

            CascadingStyleSheet css = new CascadingStyleSheet();
            css.Generator = StyleGenerator.Advanced;
            css.Parse(Request.Form["css-style"]);

            if (!thisGroup.IsGroupOperator(LoggedInMember))
            {
                Display.ShowMessage("Cannot Edit Group", "You must be an operator of the group to edit it.");
                return;
            }
            else
            {
                db.UpdateQuery(string.Format("UPDATE group_info SET group_style = '{1}' WHERE group_id = {0}",
                        thisGroup.GroupId, Mysql.Escape(css.ToString())));

                SetRedirectUri(thisGroup.Uri);
                Display.ShowMessage("Group Style Saved", "You have successfully changed the group style.");
            }
        }
    }
}