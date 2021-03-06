﻿/*
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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;


namespace BoxSocial.Groups
{
    public class Default
    {
        public static void Show(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Groups", "groups_default");

            e.Template.Parse("U_CREATE_GROUP", e.Core.Hyperlink.AppendSid("/groups/register"));

            List<Category> categories = Category.GetAll(e.Core);

            e.Template.Parse("CATEGORIES", categories.Count.ToString());

            foreach (Category category in categories)
            {
                if (category.Groups > 0)
                {
                    VariableCollection categoriesVariableCollection = e.Template.CreateChild("category_list");

                    categoriesVariableCollection.Parse("TITLE", category.Title);
                    categoriesVariableCollection.Parse("GROUPS", category.Groups.ToString());
                    categoriesVariableCollection.Parse("U_GROUP_CATEGORY", UserGroup.BuildCategoryUri(e.Core, category));

                    List<UserGroup> groups = UserGroup.GetUserGroups(e.Core, category, 1, 3);

                    foreach (UserGroup group in groups)
                    {
                        VariableCollection groupsVariableCollection = categoriesVariableCollection.CreateChild("group_list");

                        groupsVariableCollection.Parse("GROUP_DISPLAY_NAME", group.DisplayName);
                        groupsVariableCollection.Parse("U_PROFILE", group.Uri);
                        groupsVariableCollection.Parse("ICON", group.Icon);
                        groupsVariableCollection.Parse("TILE", group.Tile);
                        groupsVariableCollection.Parse("MOBILE_COVER", group.MobileCoverPhoto);

                        groupsVariableCollection.Parse("ID", group.Id);
                        groupsVariableCollection.Parse("TYPE", group.TypeId);
                        e.Core.Display.ParseBbcode(groupsVariableCollection, "DESCRIPTION", group.GroupInfo.Description);
                        groupsVariableCollection.Parse("MEMBERS", e.Core.Functions.LargeIntegerToString(group.Members));

                        if (e.Core.Session.IsLoggedIn && !group.IsGroupMember(e.Core.LoggedInMemberItemKey))
                        {
                            groupsVariableCollection.Parse("U_JOIN", group.JoinUri);
                        }
                    }
                }
            }

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "groups", e.Core.Prose.GetString("Groups", "GROUPS") });

            e.Page.ParseCoreBreadCrumbs(breadCrumbParts);
        }

        public static void ShowCategory(object sender, ShowPageEventArgs e)
        {
            e.Template.SetTemplate("Groups", "groups_default");

            string categoryPath = e.Core.PagePathParts[1].Value;

            Category category = null;

            try
            {
                category = new Category(e.Core, categoryPath);
            }
            catch (InvalidCategoryException)
            {
                return;
            }

            e.Template.Parse("CATEGORY_TITLE", category.Title);
            e.Template.Parse("U_CREATE_GROUP_C", e.Core.Hyperlink.AppendSid("/groups/register?category=" + category.Id.ToString()));
            e.Template.Parse("U_CREATE_GROUP", e.Core.Hyperlink.AppendSid("/groups/register?category=" + category.Id.ToString()));

            List<UserGroup> groups = UserGroup.GetUserGroups(e.Core, category, e.Page.TopLevelPageNumber);

            VariableCollection categoriesVariableCollection = e.Template.CreateChild("category_list");

            e.Template.Parse("GROUPS", groups.Count.ToString());

            foreach (UserGroup group in groups)
            {
                VariableCollection groupsVariableCollection = categoriesVariableCollection.CreateChild("group_list");

                groupsVariableCollection.Parse("GROUP_DISPLAY_NAME", group.DisplayName);
                groupsVariableCollection.Parse("U_PROFILE", group.Uri);
                groupsVariableCollection.Parse("ICON", group.Icon);
                groupsVariableCollection.Parse("TILE", group.Tile);
                groupsVariableCollection.Parse("MOBILE_COVER", group.MobileCoverPhoto);

                groupsVariableCollection.Parse("ID", group.Id);
                groupsVariableCollection.Parse("TYPE", group.TypeId);
                e.Core.Display.ParseBbcode(groupsVariableCollection, "DESCRIPTION", group.GroupInfo.Description);
                groupsVariableCollection.Parse("MEMBERS", e.Core.Functions.LargeIntegerToString(group.Members));

                if (e.Core.Session.IsLoggedIn && !group.IsGroupMember(e.Core.LoggedInMemberItemKey))
                {
                    groupsVariableCollection.Parse("U_JOIN", group.JoinUri);
                }
            }

            e.Core.Display.ParsePagination(UserGroup.BuildCategoryUri(e.Core, category), UserGroup.GROUPS_PER_PAGE, category.Groups);

            List<string[]> breadCrumbParts = new List<string[]>();
            breadCrumbParts.Add(new string[] { "groups", e.Core.Prose.GetString("GROUPS") });
            breadCrumbParts.Add(new string[] { category.Path, category.Title });

            e.Page.ParseCoreBreadCrumbs(breadCrumbParts);
        }
    }
}
