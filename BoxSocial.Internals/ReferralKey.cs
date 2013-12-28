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
    [DataTable("referral_keys")]
    public class ReferralKey : Item
    {
        [DataField("referral_key", 32)]
        private string key;
        [DataField("referral_user_id")]
        private long referralUserId;
        [DataField("referral_time_ut")]
        private long referralTimeRaw;

        public long ReferralUserId
        {
            get
            {
                return referralUserId;
            }
        }

        public string Key
        {
            get
            {
                return key;
            }
        }

        public ReferralKey(Core core, DataRow referralRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ReferralKey_ItemLoad);

            loadItemInfo(referralRow);
        }

        void ReferralKey_ItemLoad()
        {

        }

        public static Dictionary<string, ReferralKey> GetReferrals(Core core, string key)
        {
            Dictionary<string, ReferralKey> keys = new Dictionary<string, ReferralKey>(StringComparer.Ordinal);

            SelectQuery query = new SelectQuery(typeof(InviteKey));
            query.AddCondition("referral_key", key);

            DataTable inviteDataTable = core.Db.Query(query);

            foreach (DataRow row in inviteDataTable.Rows)
            {
                ReferralKey newKey = new ReferralKey(core, row);
                keys.Add(newKey.key, newKey);
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
