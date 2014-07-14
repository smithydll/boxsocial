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
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;


namespace BoxSocial.Internals
{
    [DataTable("dns_records")]
    public class DnsRecord : Item
    {
        [DataField("dns_domain", DataFieldKeys.Unique, "dns_domain", 63)]
        private string domain;
        [DataField("dns_owner_id", DataFieldKeys.Unique, "dns_owner")]
        private long ownerId;
        [DataField("dns_owner_type", DataFieldKeys.Unique, "dns_owner", 15)]
        private long ownerType;
        [DataField("dns_owner_key", 31)]
        private string ownerKey;

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

        public static DnsRecord Create(Core core, Primitive owner, string domain)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item item = Item.Create(core, typeof(DnsRecord), true, new FieldValuePair("dns_domain", domain.ToLower()),
                new FieldValuePair("dns_owner_id", owner.Id),
                new FieldValuePair("dns_owner_type", owner.TypeId),
                new FieldValuePair("dns_owner_key", owner.Key));

            return new DnsRecord(core, owner);
        }

        public new long Update()
        {
            /* TODO: query permissions for this update */

            UpdateQuery uQuery = new UpdateQuery(typeof(DnsRecord));
            uQuery.AddField("dns_domain", domain);
            uQuery.AddCondition("dns_owner_id", ownerId);
            uQuery.AddCondition("dns_owner_type", ownerType);

            return core.Db.Query(uQuery);
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
