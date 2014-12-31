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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class PrimitiveKey : IComparable
    {
        private string key;
        private long typeId;
        int hashCode = 0;

        public string Key
        {
            get
            {
                return key;
            }
        }

        public long TypeId
        {
            get
            {
                return typeId;
            }
        }

        public PrimitiveKey(string key, long typeId)
        {
            this.typeId = typeId;
            this.key = key;
            hashCode = typeId.GetHashCode() ^ key.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            PrimitiveKey pk = obj as PrimitiveKey;
            if (pk == null) return -1;

            if (typeId != pk.typeId)
                return typeId.CompareTo(pk.typeId);
            return key.CompareTo(pk.Key);
        }

        public override bool Equals(object obj)
        {
            PrimitiveKey pk = obj as PrimitiveKey;
            if (pk == null) return false;

            if (typeId != pk.typeId)
                return false;
            if (key != pk.key) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

    }

    public sealed class PrimitiveId : ItemKey, IComparable
    {
        public PrimitiveId(long itemId, long typeId)
            : base(itemId, typeId)
        {
        }

        public int CompareTo(object obj)
        {
            PrimitiveId pk = obj as PrimitiveId;
            if (pk == null) return -1;

            if (TypeId != pk.TypeId)
                return TypeId.CompareTo(pk.TypeId);
            return Id.CompareTo(pk.Id);
        }

        public override bool Equals(object obj)
        {
            PrimitiveId pk = obj as PrimitiveId;
            if (pk == null) return false;

            if (TypeId != pk.TypeId)
                return false;
            if (Id != pk.Id)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return TypeId.GetHashCode() ^ Id.GetHashCode();
        }

        public override string ToString()
        {
            if (TypeId < 0)
            {
                return string.Format("{0}.{1}",
                    TypeId, Id);
                ;
            }
            else
            {
                return string.Format("NULL.{0}",
                    Id);
                ;
            }
        }
    }

    [JsonObject("primitive")]
    public abstract class Primitive : NumberedItem, IPermissibleItem
    {

        protected Primitive(Core core) : base(core)
        {
        }

        public override abstract long Id
        {
            get;
        }

        public abstract string Key
        {
            get;
        }

        public abstract string Type
        {
            get;
        }
		
		public long TypeId
        {
            get
            {
                return ItemKey.GetTypeId(core, this.GetType());
            }
        }

        public new ItemKey ItemKey
        {
            get
            {
                return new ItemKey(Id, TypeId);
            }
        }

        public abstract string AccountUriStub
        {
            get;
        }

        public abstract AppPrimitives AppPrimitive
        {
            get;
        }

        public abstract string Thumbnail
        {
            get;
        }

        public abstract string Icon
        {
            get;
        }

        public abstract string Tile
        {
            get;
        }

        public abstract string Square
        {
            get;
        }

        public abstract string CoverPhoto
        {
            get;
        }

        public abstract string MobileCoverPhoto
        {
            get;
        }

        public abstract string UriStub
        {
            get;
        }

        public abstract string UriStubAbsolute
        {
            get;
        }

        public override abstract string Uri
        {
            get;
        }

        public abstract string TitleName
        {
            get;
        }

        public abstract string TitleNameOwnership
        {
            get;
        }

        public abstract string DisplayName
        {
            get;
        }

        public abstract string DisplayNameOwnership
        {
            get;
        }

        public abstract bool CanModerateComments(User member);

        public abstract bool IsItemOwner(User member);

        public void ParseBreadCrumbs(List<string[]> parts)
        {
            ParseBreadCrumbs("BREADCRUMBS", parts);
        }

        public void ParseBreadCrumbs(string templateVar, List<string[]> parts)
        {
            ParseBreadCrumbs(core.Template, templateVar, parts);
        }

        public void ParseBreadCrumbs(Template template, string templateVar, List<string[]> parts)
        {
            template.ParseRaw(templateVar, GenerateBreadCrumbs(parts));
        }

        public abstract string GenerateBreadCrumbs(List<string[]> parts);

        public abstract Access Access
        {
            get;
        }

        public abstract bool IsSimplePermissions
        {
            get;
            set;
        }

        [JsonIgnore]
        public Primitive Owner
        {
            get
            {
                return this;
            }
        }

        public abstract List<AccessControlPermission> AclPermissions
        {
            get;
        }

        public abstract bool IsItemGroupMember(ItemKey viewer, ItemKey key);

        [JsonIgnore]
        public IPermissibleItem PermissiveParent
        {
            get
            {
                return this;
            }
        }

        [JsonIgnore]
        public ItemKey PermissiveParentKey
        {
            get
            {
                return ItemKey;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A dictionary of Item Type with language keys</returns>
        public abstract List<PrimitivePermissionGroup> GetPrimitivePermissionGroups();

        public abstract List<User> GetPermissionUsers();
        public abstract List<User> GetPermissionUsers(string namePart);

        public abstract bool GetIsMemberOfPrimitive(ItemKey viewer, ItemKey primitiveKey);

        public abstract bool CanEditPermissions();
        public abstract bool CanEditItem();
        public abstract bool CanDeleteItem();

        public abstract bool GetDefaultCan(string permission, ItemKey viewer);

        public abstract string DisplayTitle
        {
            get;
        }

        public abstract string ParentPermissionKey(Type parentType, string permission);

        public override bool Equals(object obj)
        {
            if (obj.GetType().IsSubclassOf(typeof(Primitive)) || obj.GetType() == typeof(Primitive))
            {
                Primitive p = (Primitive)obj;

                if (TypeId != p.TypeId)
                    return false;
                if (Id != p.Id)
                    return false;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PrimitivePermissionGroup
    {
        private long typeId;
        private long itemId;
        private string languageKey;
        private string displayName;
        private string tile;

        public ItemKey ItemKey
        {
            get
            {
                return new ItemKey(itemId, typeId);
            }
        }

        public long TypeId
        {
            get
            {
                return typeId;
            }
        }

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public string LanguageKey
        {
            get
            {
                return languageKey;
            }
        }
        
        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public string Tile
        {
            get
            {
                return tile;
            }
        }

        public PrimitivePermissionGroup(long typeId, long id, string displayName, string tile)
        {
            this.typeId = typeId;
            this.itemId = id;
            this.languageKey = null;
            this.displayName = displayName;
            this.tile = tile;
        }

        public PrimitivePermissionGroup(ItemKey item, string displayName, string tile)
            : this(item.TypeId, item.Id, displayName, tile)
        {
        }
        
        public PrimitivePermissionGroup(long typeId, long id, string languageKey, string displayName, string tile)
        {
            this.typeId = typeId;
            this.itemId = id;
            this.languageKey = languageKey;
            this.displayName = displayName;
            this.tile = tile;
        }

        public PrimitivePermissionGroup(ItemKey item, string languageKey, string displayName, string tile)
            : this(item.TypeId, item.Id, languageKey, displayName, tile)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != typeof(PrimitivePermissionGroup))
                return false;
            PrimitivePermissionGroup ppg = (PrimitivePermissionGroup)obj;

            if (TypeId != ppg.TypeId)
                return false;
            if (ItemId != ppg.ItemId)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return ItemKey.GetHashCode();
        }
    }
}
