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
        [DataFieldKey(DataFieldKeys.Index, "i_primitive_application_id")]
        [DataField("application_id", typeof(ApplicationEntry))]
        private long applicationId;
        [DataField("app_simple_permissions")]
        private bool simplePermissions;
        [DataField("app_email_notifications")]
        private bool emailNotifications;
        // oauth credentials
        [DataField("app_oauth_access_token", 127)]
        private string oauthAccessToken;
        [DataField("app_oauth_access_token_secret", 127)]
        private string oauthAccessTokenSecret;

        private Primitive owner; // primitive installed the application
        private Access access; // primitive application access rights

        public long AppId
        {
            get
            {
                return appId;
            }
        }

        public bool EmailNotifications
        {
            get
            {
                return emailNotifications;
            }
            set
            {
                SetProperty("emailNotifications", value);
            }
        }

        public string OAuthAccessToken
        {
            get
            {
                return oauthAccessToken;
            }
        }

        public string OAuthAccessTokenSecret
        {
            get
            {
                return oauthAccessTokenSecret;
            }
        }

        public ItemKey OwnerKey
        {
            get
            {
                return ownerKey;
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

        public bool IsSimplePermissions
        {
            get
            {
                return simplePermissions;
            }
            set
            {
                SetPropertyByRef(new { simplePermissions }, value);
            }
        }

        public PrimitiveApplicationInfo(Core core, Primitive owner, long applicationId)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(PrimitiveApplicationInfo_ItemLoad);

            SelectQuery query = new SelectQuery(PrimitiveApplicationInfo.GetTable(typeof(PrimitiveApplicationInfo)));
            query.AddFields(PrimitiveApplicationInfo.GetFieldsPrefixed(core, typeof(PrimitiveApplicationInfo)));
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

        public PrimitiveApplicationInfo(Core core, System.Data.Common.DbDataReader appRow)
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

        protected override void loadItemInfo(System.Data.Common.DbDataReader appRow)
        {
            loadValue(appRow, "item", out ownerKey);
            loadValue(appRow, "application_id", out applicationId);
            loadValue(appRow, "app_simple_permissions", out simplePermissions);
            loadValue(appRow, "app_email_notifications", out emailNotifications);
            loadValue(appRow, "app_oauth_access_token", out oauthAccessToken);
            loadValue(appRow, "app_oauth_access_token_secret", out oauthAccessTokenSecret);

            itemLoaded(appRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void PrimitiveApplicationInfo_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return appId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }

        public List<AccessControlPermission> AclPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsItemGroupMember(ItemKey viewer, ItemKey key)
        {
            return false;
        }
        
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return Owner;
            }
        }

        public ItemKey PermissiveParentKey
        {
            get
            {
                return ownerKey;
            }
        }

        public bool GetDefaultCan(string permission, ItemKey viewer)
        {
            return false;
        }

        public string DisplayTitle
        {
            get
            {
                return "Application (" + Owner.DisplayTitle + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidPrimitiveAppInfoException : Exception
    {
    }
}
