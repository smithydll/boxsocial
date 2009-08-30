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
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("primitive_apps")]
    public class PrimitiveApplicationInfo : NumberedItem, IPermissibleItem
    {
        [DataField("app_id", DataFieldKeys.Primary)]
        private long appId;
        /*[DataField("item_id")]
        private long itemId;
        [DataField("item_type", 31)]
        private string itemType;*/
		[DataField("item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("application_id")]
        private long applicationId;

        private Primitive owner; // primitive installed the application
        private Access access; // primitive application access rights

        public long AppId
        {
            get
            {
                return appId;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.Type != owner.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerKey);
                    owner = core.UserProfiles[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public Access Access
        {
            get
            {
                if (access == null)
                {
                    access = new Access(core, this, Owner);
                }

                return access;
            }
        }

        public PrimitiveApplicationInfo(Core core, Primitive owner, long applicationId)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(PrimitiveApplicationInfo_ItemLoad);

            SelectQuery query = new SelectQuery(PrimitiveApplicationInfo.GetTable(typeof(PrimitiveApplicationInfo)));
            query.AddFields(PrimitiveApplicationInfo.GetFieldsPrefixed(typeof(PrimitiveApplicationInfo)));
            query.AddCondition("application_id", applicationId);
            query.AddCondition("item_id", owner.Id);
            query.AddCondition("item_type_id", owner.TypeId);

            DataTable appDataTable = db.Query(query);

            if (appDataTable.Rows.Count == 1)
            {
                DataRow appRow = appDataTable.Rows[0];
                try
                {
                    loadItemInfo(appRow);
                }
                catch (InvalidItemException)
                {
                    throw new InvalidPrimitiveAppInfoException();
                }
            }
            else
            {
                throw new InvalidPrimitiveAppInfoException();
            }
        }

        public PrimitiveApplicationInfo(Core core, DataRow appRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(PrimitiveApplicationInfo_ItemLoad);

            try
            {
                loadItemInfo(appRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidPrimitiveAppInfoException();
            }
        }

        private void PrimitiveApplicationInfo_ItemLoad()
        {
        }

        public override long Id
        {
            get { throw new NotImplementedException(); }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }

        #region IPermissibleItem Members

        public List<string> PermissibleActions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }

    public class InvalidPrimitiveAppInfoException : Exception
    {
    }
}
