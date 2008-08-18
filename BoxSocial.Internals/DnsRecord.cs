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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;


namespace BoxSocial.Internals
{
    [DataTable("dns_records")]
    public class DnsRecord : Item
    {
        [DataField("dns_domain", DataFieldKeys.Unique)]
        private string domain;
        [DataField("dns_owner_id", DataFieldKeys.Unique, "dns_owner")]
        private long ownerId;
        [DataField("dns_owner_type", DataFieldKeys.Unique, "dns_owner", 15)]
        private string ownerType;

        private Primitive owner;

        public string Domain
        {
            get
            {
                return domain;
            }
            set
            {
                SetProperty("domain", value);
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id)
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerType, ownerId);
                    owner = core.UserProfiles[ownerType, ownerId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public DnsRecord(Core core, string domain)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(DnsRecord_ItemLoad);

            try
            {
                LoadItem("dns_domain", domain);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDnsRecordException();
            }
        }

        public DnsRecord(Core core, Primitive owner)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(DnsRecord_ItemLoad);

            try
            {
                LoadItem("dns_owner_id", "dns_owner_type", owner);
            }
            catch (InvalidItemException)
            {
                throw new InvalidDnsRecordException();
            }
        }

        private void DnsRecord_ItemLoad()
        {
        }

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        public override string Uri
        {
            get
            {
                return string.Format("http://{0}",
                    domain);
            }
        }
    }

    public class InvalidDnsRecordException : Exception
    {
    }
}
