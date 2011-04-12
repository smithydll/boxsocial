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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.Applications.EnterpriseResourcePlanning
{
    [DataTable("erp_project")]
    [Permission("VIEW_DOCUMENTS", "Can view documents", PermissionTypes.View)]
    [Permission("VIEW_SUPPLIERS", "Can view suppliers", PermissionTypes.View)]
    [Permission("VIEW_PURCHASES", "Can view purchases", PermissionTypes.View)]
    [Permission("CREATE_DOCUMENTS", "Can create documents", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_SUPPLIERS", "Can create suppliers", PermissionTypes.CreateAndEdit)]
    [Permission("CREATE_PURCHASES", "Can create purchases", PermissionTypes.CreateAndEdit)]
    public class Project : NumberedItem, IPermissibleItem
    {
        [DataField("project_id", DataFieldKeys.Primary)]
        private int projectId;
        [DataField("project_key", DataFieldKeys.Unique, "project_key")]
        private string projectKey;
        [DataField("project_item", DataFieldKeys.Unique, "project_key")]
        private ItemKey ownerKey;
        [DataField("project_title", 63)]
        private string projectTitle;
        [DataField("project_start_date")]
        private long projectStartDate;

        private Primitive owner;
        private Access access;

        public string ProjectKey
        {
            get
            {
                return projectKey;
            }
        }
        
        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this);
                }
                return access;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.Type != owner.Type)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public Project(Core core, long projectId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(Project_ItemLoad);

            try
            {
                LoadItem(projectId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidProjectException();
            }
        }

        void Project_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return projectId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}project/{1}",
                        Owner.UriStub, ProjectKey));
            }
        }
    }

    public class InvalidProjectException : Exception
    {
    }
}
