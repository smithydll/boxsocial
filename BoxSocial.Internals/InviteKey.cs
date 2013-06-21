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
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("invite_keys")]
    public class InviteKey : Item
    {
        [DataField("email_hash", 128)]
        private string emailHash;
        [DataField("email_key", 32)]
        private string emailKey;
        [DataField("invite_allow")]
        private bool allowInvite;
        [DataField("invite_time_ut")]
        private long inviteTimeRaw;

        public InviteKey(Core core, DataRow inviteRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(InviteKey_ItemLoad);

            loadItemInfo(inviteRow);
        }

        void InviteKey_ItemLoad()
        {

        }

        public static Dictionary<string, InviteKey> GetInvites(Core core, string emailKey)
        {
            Dictionary<string, InviteKey> keys = new Dictionary<string, InviteKey>(StringComparer.Ordinal);

            SelectQuery query = new SelectQuery(typeof(InviteKey));
            query.AddCondition("email_key", emailKey);

            DataTable inviteDataTable = core.Db.Query(query);

            foreach (DataRow row in inviteDataTable.Rows)
            {
                InviteKey newKey = new InviteKey(core, row);
                keys.Add(newKey.emailHash, newKey);
            }

            return keys;
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
