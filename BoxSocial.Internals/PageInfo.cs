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
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
	
	[TableView("user_pages")]
	public class PageInfo : NumberedItem
	{
		[DataField("page_id", DataFieldKeys.Primary)]
        private long pageId;
        [DataField("user_id")]
        private long creatorId;
        [DataField("page_slug", 63)]
        private string slug;
        [DataField("page_title", 63)]
        private string title;
        [DataField("page_license")]
        private byte licenseId;
        [DataField("page_views")]
        private long views;
        [DataField("page_status", 15)]
        private string status;
        [DataField("page_ip", 50)]
        private string ipRaw;
        [DataField("page_ip_proxy", 50)]
        private string ipProxyRaw;
        [DataField("page_parent_path", 1023)]
        private string parentPath;
        [DataField("page_order")]
        private int order;
        [DataField("page_parent_id")]
        private long parentId;
        [DataField("page_list_only")]
        private bool listOnly;
        [DataField("page_application")]
        private long applicationId;
        [DataField("page_icon", 63)]
        private string icon;
        [DataField("page_date_ut")]
        private long createdRaw;
        [DataField("page_modified_ut")]
        private long modifiedRaw;
        [DataField("page_classification")]
        private byte classificationId;
		[DataField("page_level")]
        private int pageLevel;
        [DataField("page_hierarchy", MYSQL_TEXT)]
        private string hierarchy;
		[DataField("page_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
		
		private Primitive owner;
		
		public string FullPath
        {
            get
            {
                if (!string.IsNullOrEmpty(parentPath))
                {
                    return string.Format("{0}/{1}", parentPath, slug);
                }
                else
                {
                    return slug;
                }
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

		
		public PageInfo(Core core)
			: base(core)
		{
		}
		
		public override long Id
        {
            get
            {
                return pageId;
            }
        }

        public override string Uri
        {
            get
            {
                return core.Uri.AppendSid(string.Format("{0}{1}",
                    Owner.UriStub, FullPath));
            }
        }
	}
}
