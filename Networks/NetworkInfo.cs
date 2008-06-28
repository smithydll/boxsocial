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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Networks
{
    [DataTable("network_info")]
    public class NetworkInfo : Item
    {
        [DataField("network_id", DataFieldKeys.Primary)]
        private long networkId;
        [DataField("network_network", 24)]
        private string networkNetwork;
        [DataField("network_name_display", 45)]
        private string displayName;
        [DataField("network_abstract", MYSQL_TEXT)]
        private string description;
        [DataField("network_members")]
        private long members;
        [DataField("network_comments")]
        private long comments;
        [DataField("network_require_confirmation")]
        private bool requireConfirmation;
        [DataField("network_type", 15)]
        private string networkType;
        [DataField("network_gallery_items")]
        private long galleryItems;
        [DataField("network_bytes")]
        private long bytes;

        private string displayNameOwnership;

        public long NetworkId
        {
            get
            {
                return networkId;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
        }

        public string DisplayNameOwnership
        {
            get
            {
                if (displayNameOwnership == null)
                {
                    displayNameOwnership = (displayName != "") ? displayName : networkNetwork;

                    if (displayNameOwnership.EndsWith("s"))
                    {
                        displayNameOwnership = displayNameOwnership + "'";
                    }
                    else
                    {
                        displayNameOwnership = displayNameOwnership + "'s";
                    }
                }
                return displayNameOwnership;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public long Members
        {
            get
            {
                return members;
            }
        }

        public long Comments
        {
            get
            {
                return comments;
            }
        }

        public bool RequireConfirmation
        {
            get
            {
                return requireConfirmation;
            }
        }

        public NetworkTypes NetworkType
        {
            get
            {
                switch (networkType.ToUpper())
                {
                    case "UNIVERSITY":
                        return NetworkTypes.University;
                    case "SCHOOL":
                        return NetworkTypes.School;
                    case "WORKPLACE":
                        return NetworkTypes.Workplace;
                    case "COUNTRY":
                        return NetworkTypes.Country;
                    case "GLOBAL":
                        return NetworkTypes.Global;
                    default:
                        throw new InvalidNetworkTypeException();
                }
            }
            set
            {
                switch (value)
                {
                    case NetworkTypes.University:
                        SetProperty(networkType, "UNIVERSITY");
                        break;
                    case NetworkTypes.School:
                        SetProperty(networkType, "SCHOOL");
                        break;
                    case NetworkTypes.Workplace:
                        SetProperty(networkType, "WORKPLACE");
                        break;
                    case NetworkTypes.Country:
                        SetProperty(networkType, "COUNTRY");
                        break;
                    case NetworkTypes.Global:
                        SetProperty(networkType, "GLOBAL");
                        break;
                }
            }
        }

        public long GalleryItems
        {
            get
            {
                return galleryItems;
            }
        }

        internal NetworkInfo(Core core, long networkId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(NetworkInfo_ItemLoad);

            try
            {
                LoadItem("network_id", networkId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNetworkException();
            }
        }

        internal NetworkInfo(Core core, DataRow networkRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(NetworkInfo_ItemLoad);

            try
            {
                loadItemInfo(networkRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNetworkException();
            }
        }

        void NetworkInfo_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return networkId;
            }
        }

        public override string Namespace
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
