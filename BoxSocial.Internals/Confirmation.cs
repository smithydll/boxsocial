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
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    /// <summary>
    /// Visual Confirmation Codes
    /// </summary>
    [DataTable("confirm")]
    public class Confirmation : NumberedItem
    {
        [DataField("confirm_id", DataFieldKeys.Primary)]
        private long confirmId;
        [DataField("session_id", 32)]
        private string sessionId;
        [DataField("confirm_code", 8)]
        private string confirmCode;
        [DataField("confirm_type")]
        private byte confirmType;

        public long ConfirmId
        {
            get
            {
                return confirmId;
            }
        }

        public string SessionId
        {
            get
            {
                return sessionId;
            }
        }

        public string ConfirmationCode
        {
            get
            {
                return confirmCode;
            }
        }

        public byte ConfirmationType
        {
            get
            {
                return confirmType;
            }
        }

        public Confirmation(Core core, long confirmId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(Confirmation_ItemLoad);

            try
            {
                LoadItem(confirmId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidConfirmationException();
            }
        }

        void Confirmation_ItemLoad()
        {
        }

        public static Confirmation Create(Core core, string session, string code, byte type)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            InsertQuery iQuery = new InsertQuery(GetTable(typeof(Confirmation)));
            iQuery.AddField("session_id", session);
            iQuery.AddField("confirm_code", code);
            iQuery.AddField("confirm_type", type);

            long confirmId = core.Db.Query(iQuery);

            return new Confirmation(core, confirmId);
        }

        public static void ClearStale(Core core, string session, byte type)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            DeleteQuery dQuery = new DeleteQuery(GetTable(typeof(Confirmation)));
            dQuery.AddCondition("confirm_type", type);
            dQuery.AddCondition("session_id", session);

            core.Db.Query(dQuery);
        }

        public override long Id
        {
            get
            {
                return confirmId;
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

    public class InvalidConfirmationException : Exception
    {
    }
}
