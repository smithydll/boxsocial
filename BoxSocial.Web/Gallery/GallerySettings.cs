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
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;
using BoxSocial.Networks;
using BoxSocial.Musician;

namespace BoxSocial.Applications.Gallery
{

    /// <summary>
    /// Gallery Settings
    /// </summary>
    [DataTable("user_gallery_settings")]
    [Permission("VIEW", "Can view the gallery", PermissionTypes.View)]
    [Permission("COMMENT", "Can comment on the gallery", PermissionTypes.Interact)]
    [Permission("CREATE_CHILD", "Can create child galleries", PermissionTypes.CreateAndEdit)]
    [Permission("VIEW_ITEMS", "Can view gallery photos", PermissionTypes.View)]
    [Permission("COMMENT_ITEMS", "Can comment on the photos", PermissionTypes.Interact)]
    [Permission("RATE_ITEMS", "Can rate the photos", PermissionTypes.Interact)]
    [Permission("CREATE_ITEMS", "Can upload photos to gallery", PermissionTypes.CreateAndEdit)]
    [Permission("EDIT_ITEMS", "Can edit photos", PermissionTypes.CreateAndEdit)]
    [Permission("DELETE_ITEMS", "Can delete photos", PermissionTypes.Delete)]
    public class GallerySettings : NumberedItem, IPermissibleItem
    {
        [DataField("gallery_settings_id", DataFieldKeys.Primary)]
        private long settingsId;
        [DataField("gallery_item", DataFieldKeys.Unique)]
        private ItemKey ownerKey;
        [DataField("gallery_comments")]
        private long comments;
        [DataField("gallery_items")]
        private long galleryItems;
        [DataField("gallery_bytes")]
        private long bytes;
        [DataField("gallery_allow_items_root")]
        private bool allowItemsAtRoot;
        [DataField("gallery_items_root")]
        private long galleryItemsAtRoot;
        [DataField("gallery_simple_permissions")]
        private bool simplePermissions;

        private Primitive owner;
        private Access access;

        public override long Id
        {
            get
            {
                return settingsId;
            }
        }

        public long Comments
        {
            get
            {
                return comments;
            }
        }

        public long GalleryItems
        {
            get
            {
                return galleryItems;
            }
        }

        public long GalleryItemsAtRoot
        {
            get
            {
                return galleryItemsAtRoot;
            }
        }

        public long Bytes
        {
            get
            {
                return bytes;
            }
        }

        public bool AllowItemsAtRoot
        {
            get
            {
                return allowItemsAtRoot;
            }
            set
            {
                SetProperty("allowItemsAtRoot", value);
            }
        }

        public GallerySettings(Core core, long settingsId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(GallerySettings_ItemLoad);

            try
            {
                LoadItem(settingsId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGallerySettingsException();
            }
        }

        public GallerySettings(Core core, Primitive owner)
            : base(core)
        {
            this.owner = owner;

            ItemLoad += new ItemLoadHandler(GallerySettings_ItemLoad);

            try
            {
                LoadItem("gallery_item_id", "gallery_item_type_id", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGallerySettingsException();
            }
        }

        public GallerySettings(Core core, DataRow settingsRow)
            : base(core)
        {

            ItemLoad += new ItemLoadHandler(GallerySettings_ItemLoad);

            try
            {
                loadItemInfo(settingsRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGallerySettingsException();
            }
        }

        public GallerySettings(Core core, System.Data.Common.DbDataReader settingsRow)
            : base(core)
        {

            ItemLoad += new ItemLoadHandler(GallerySettings_ItemLoad);

            try
            {
                loadItemInfo(settingsRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidGallerySettingsException();
            }
        }

        protected override void loadItemInfo(DataRow settingsRow)
        {
            loadValue(settingsRow, "gallery_settings_id", out settingsId);
            loadValue(settingsRow, "gallery_item", out ownerKey);
            loadValue(settingsRow, "gallery_comments", out comments);
            loadValue(settingsRow, "gallery_items", out galleryItems);
            loadValue(settingsRow, "gallery_bytes", out bytes);
            loadValue(settingsRow, "gallery_allow_items_root", out allowItemsAtRoot);
            loadValue(settingsRow, "gallery_items_root", out galleryItemsAtRoot);
            loadValue(settingsRow, "gallery_simple_permissions", out simplePermissions);

            itemLoaded(settingsRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader settingsRow)
        {
            loadValue(settingsRow, "gallery_settings_id", out settingsId);
            loadValue(settingsRow, "gallery_item", out ownerKey);
            loadValue(settingsRow, "gallery_comments", out comments);
            loadValue(settingsRow, "gallery_items", out galleryItems);
            loadValue(settingsRow, "gallery_bytes", out bytes);
            loadValue(settingsRow, "gallery_allow_items_root", out allowItemsAtRoot);
            loadValue(settingsRow, "gallery_items_root", out galleryItemsAtRoot);
            loadValue(settingsRow, "gallery_simple_permissions", out simplePermissions);

            itemLoaded(settingsRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        void GallerySettings_ItemLoad()
        {
        }

        public static GallerySettings Create(Core core, Primitive owner)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(GallerySettings)));
            iQuery.AddField("gallery_item_id", owner.Id);
            iQuery.AddField("gallery_item_type_id", owner.TypeId);
            iQuery.AddField("gallery_items", 0);
            iQuery.AddField("gallery_items_root", 0);
            iQuery.AddField("gallery_comments", 0);
            iQuery.AddField("gallery_bytes", 0);
            iQuery.AddField("gallery_allow_items_root", false);

            long settingsId = core.Db.Query(iQuery);

            GallerySettings settings = new GallerySettings(core, settingsId);

            if (owner is User)
            {
                Access.CreateAllGrantsForOwner(core, settings);
                settings.Access.CreateGrantForPrimitive(Friend.GetFriendsGroupKey(core), "VIEW", "VIEW_ITEMS", "COMMENT", "COMMENT_ITEMS", "RATE_ITEMS");
                settings.Access.CreateGrantForPrimitive(User.GetEveryoneGroupKey(core), "VIEW", "VIEW_ITEMS");
            }
            if (owner is UserGroup)
            {
                settings.Access.CreateAllGrantsForPrimitive(UserGroup.GetGroupOperatorsGroupKey(core));
                settings.Access.CreateGrantForPrimitive(UserGroup.GetGroupMembersGroupKey(core), "VIEW", "VIEW_ITEMS", "COMMENT", "COMMENT_ITEMS", "RATE_ITEMS", "CREATE_ITEMS");
                settings.Access.CreateGrantForPrimitive(User.GetEveryoneGroupKey(core), "VIEW", "VIEW_ITEMS");
            }
            if (owner is Musician.Musician)
            {
                settings.Access.CreateAllGrantsForPrimitive(Musician.Musician.GetMusicianMembersGroupKey(core));
                settings.Access.CreateGrantForPrimitive(Musician.Musician.GetMusicianFansGroupKey(core), "VIEW", "VIEW_ITEMS", "COMMENT", "COMMENT_ITEMS", "RATE_ITEMS", "CREATE_ITEMS");
                settings.Access.CreateGrantForPrimitive(User.GetEveryoneGroupKey(core), "VIEW", "VIEW_ITEMS");
            }
            if (owner is Network)
            {
                settings.Access.CreateGrantForPrimitive(Network.GetNetworkMembersGroupKey(core), "VIEW", "VIEW_ITEMS", "COMMENT", "COMMENT_ITEMS", "RATE_ITEMS", "CREATE_ITEMS");
                settings.Access.CreateGrantForPrimitive(User.GetEveryoneGroupKey(core), "VIEW", "VIEW_ITEMS");
            }
            if (owner is ApplicationEntry)
            {
                settings.Access.CreateAllGrantsForPrimitive(ApplicationEntry.GetApplicationDevelopersGroupKey(core));
                settings.Access.CreateGrantForPrimitive(User.GetRegisteredUsersGroupKey(core), "VIEW", "VIEW_ITEMS", "COMMENT", "COMMENT_ITEMS", "RATE_ITEMS");
                settings.Access.CreateGrantForPrimitive(User.GetEveryoneGroupKey(core), "VIEW", "VIEW_ITEMS");
            }

            return settings;
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
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
                return "Gallery Settings: " + Owner.DisplayName + " (" + Owner.Key + ")";
            }
        }

        public string ParentPermissionKey(Type parentType, string permission)
        {
            return permission;
        }
    }

    public class InvalidGallerySettingsException : Exception
    {
    }
}
