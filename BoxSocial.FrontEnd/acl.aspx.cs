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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;

namespace BoxSocial.FrontEnd
{
    public class acl : TPage
    {
        
        public acl()
            : base("acl.html")
        {
        }
        
        protected void Page_Load(object sender, EventArgs e)
        {
            // Mobile doesn't include jQuery UI by default, but is needed to userselectbox
            if (core.IsMobile)
            {
                VariableCollection javaScriptVariableCollection = template.CreateChild("javascript_list");
                javaScriptVariableCollection.Parse("URI", @"/scripts/jquery-ui-1.9.1.boxsocial.min.js");

                VariableCollection styleSheetVariableCollection = core.Template.CreateChild("style_sheet_list");
                styleSheetVariableCollection.Parse("URI", @"/styles/jquery-ui-1.9.1.boxsocial.min.css");
            }

            long itemId = core.Functions.FormLong("id", core.Functions.RequestLong("id", 0));
            long itemTypeId = core.Functions.FormLong("type", core.Functions.RequestLong("type", 0));
            
            if (itemId == 0 || itemTypeId == 0)
            {
                core.Functions.Generate404();
                return;
            }
            
            ItemKey itemKey = null;
            
            try
            {
                itemKey = new ItemKey(itemId, itemTypeId);
            }
            catch (InvalidItemTypeException)
            {
                core.Functions.Generate404();
                return;
            }

            NumberedItem ni = null;

            try
            {
                ni = NumberedItem.Reflect(core, itemKey);
            }
            catch (MissingMethodException)
            {
                core.Functions.Generate404();
                return;
            }

            if (!(ni is IPermissibleItem))
            {
                core.Functions.Generate404();
                return;
            }

            IPermissibleItem pi = (IPermissibleItem)ni;

            if (!pi.Access.Can("EDIT_PERMISSIONS"))
            {
                core.Functions.Generate403();
                return;
            }
            
            List<AccessControlPermission> permissions = AccessControlLists.GetPermissions(core, itemKey);
            
            AccessControlLists acl = new AccessControlLists(core, pi);

            if (Request.Form["delete"] != null)
            {
                acl_Delete(acl);
            }
            
            if (Request.Form["save"] != null)
            {
                acl_Save(acl);
            }

            template.Parse("ITEM_TITLE", pi.DisplayTitle);

            try
            {
                if (!string.IsNullOrEmpty(pi.Uri))
                {
                    template.Parse("U_ITEM", pi.Uri);
                }
            }
            catch (NotImplementedException)
            {
            }
            
            acl.ParseACL(template, pi.Owner, "S_PERMISSIONS");
            
            /*Template aclTemplate = new Template(core.Http.TemplatePath, "std.acl.html");
            
            template.ParseRaw("S_PERMISSIONS", aclTemplate.ToString());*/

            /*if (!pi.Access.Can("EDIT_PERMISSIONS"))
            {
                core.Functions.Generate403();
                return;
            }*/
            
            template.Parse("S_ITEM_ID", itemId.ToString());
            template.Parse("S_ITEM_TYPE_ID", itemTypeId.ToString());
            
            EndResponse();
        }

        private void acl_Save(AccessControlLists acl)
        {
            try
            {
                acl.SavePermissions();
            }
            catch
            {
                core.Functions.Generate403();
                return;
            }
        }

        private void acl_Delete(AccessControlLists acl)
        {
            string value = Request.Form["delete"];

            if (!string.IsNullOrEmpty(value))
            {
                string[] vals = value.Split(new char[] { ',' });

                if (vals.Length == 3)
                {
                    int permissionId = 0;
                    int primitiveTypeId = 0;
                    int primitiveId = 0;

                    int.TryParse(vals[0], out permissionId);
                    int.TryParse(vals[1], out primitiveTypeId);
                    int.TryParse(vals[2], out primitiveId);

                    if (permissionId != 0 && primitiveTypeId != 0 && primitiveId != 0)
                    {
                        try
                        {
                            acl.DeleteGrant(permissionId, primitiveTypeId, primitiveId);
                        }
                        catch
                        {
                            core.Functions.Generate403();
                            return;
                        }
                    }
                }
            }
        }
    }
}
