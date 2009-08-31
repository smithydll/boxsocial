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
            int itemId = core.Functions.RequestInt("id", 0);
            int itemTypeId = core.Functions.RequestInt("type", 0);
            
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

            NumberedItem ni = NumberedItem.Reflect(core, itemKey);

            if (!(ni is IPermissibleItem))
            {
                core.Functions.Generate404();
                return;
            }

            IPermissibleItem pi = (IPermissibleItem)ni;
            
            List<AccessControlPermission> permissions = AccessControlLists.GetPermissions(core, itemKey);
            
            AccessControlLists acl = new AccessControlLists(core, pi);
            acl.ParseACL(template, loggedInMember, "S_PERMISSIONS");
            
            /*Template aclTemplate = new Template(core.Http.TemplatePath, "std.acl.html");
            
            template.ParseRaw("S_PERMISSIONS", aclTemplate.ToString());*/

            /*if (!pi.Access.Can("EDIT_PERMISSIONS"))
            {
                core.Functions.Generate403();
                return;
            }*/

            if (Request.Form["save"] != null)
            {
                acl_Save(acl);
            }
            
            EndResponse();
        }

        private void acl_Save(AccessControlLists acl)
        {
            try
            {
                //acl.Save();
            }
            catch
            {
                core.Functions.Generate403();
                return;
            }
        }
    }
}
