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
    public class PrimitiveApplicationInfo : Item
    {
        [DataField("app_id", DataFieldKeys.Primary)]
        private long appId;
        [DataField("item_id")]
        private long itemId;
        [DataField("item_type", 31)]
        private string itemType;
        [DataField("application_id")]
        private long applicationId;
        [DataField("app_access")]
        private ushort permissions;

        private Primitive owner; // primitive installed the application
        private Access applicationAccess; // primitive application access rights

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
                if (owner == null)
                {
                    if (itemType == "USER")
                    {
                        owner = core.UserProfiles[itemId];
                    }
                }

                return owner;
            }
        }

        public Access ApplicationAccess
        {
            get
            {
                if (applicationAccess == null)
                {
                    applicationAccess = new Access(core, permissions, Owner);
                }

                return applicationAccess;
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
            query.AddCondition("item_type", owner.Type);

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
            if (itemType == "USER")
            {
                core.LoadUserProfile(itemId);
            }
        }

        public override long Id
        {
            get { throw new NotImplementedException(); }
        }

        public override string Namespace
        {
            get { throw new NotImplementedException(); }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidPrimitiveAppInfoException : Exception
    {
    }
}
