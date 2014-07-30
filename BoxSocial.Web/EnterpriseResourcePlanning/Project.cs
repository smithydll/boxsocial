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
    [DataTable("erp_projects")]
    public class Project : NumberedItem
    {
        [DataField("project_id", DataFieldKeys.Primary)]
        private int projectId;
        [DataField("project_key", DataFieldKeys.Unique, "u_project_key")]
        private string projectKey;
        [DataField("project_item", DataFieldKeys.Unique, "u_project_key")]
        private ItemKey ownerKey;
        [DataField("project_title", 63)]
        private string projectTitle;
        [DataField("project_start_date")]
        private long projectStartDate;

        private Primitive owner;

        public string Title
        {
            get
            {
                return projectTitle;
            }
            set
            {
                SetProperty("projectTitle", value);
            }
        }

        public string ProjectKey
        {
            get
            {
                return projectKey;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
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

        public Project(Core core, Primitive owner, string key)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Project_ItemLoad);

            try
            {
                LoadItem("project_item_id", "project_item_type_id", owner, new FieldValuePair("project_key", key));
            }
            catch (InvalidItemException)
            {
                throw new InvalidProjectException();
            }
        }

        public Project(Core core, DataRow projectDataRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Project_ItemLoad);

            try
            {
                loadItemInfo(projectDataRow);
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
                return core.Hyperlink.AppendSid(string.Format("{0}project/{1}",
                        Owner.UriStub, ProjectKey));
            }
        }

        public static void Show(object sender, ShowPPageEventArgs e)
        {
            e.SetTemplate("viewproject");

            Project project = null;

            try
            {
                project = new Project(e.Core, e.Page.Owner, e.Slug);
            }
            catch (InvalidProjectException)
            {
                e.Core.Functions.Generate404();
            }

            e.Template.Parse("PROJECT_TITLE", project.Title);
        }
    }

    public class InvalidProjectException : Exception
    {
    }
}
