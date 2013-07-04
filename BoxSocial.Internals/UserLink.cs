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
    [DataTable("user_links")]
    public sealed class UserLink : NumberedItem
    {
        [DataField("user_link_id", DataFieldKeys.Primary)]
        private long linkId;
        [DataField("user_link_user_id", typeof(User))]
        private long userId;
        [DataField("user_link_title", 31)]
        private string title;
        [DataField("user_link_uri", 255)]
        private string uri;
        [DataField("link_time_ut")]
        private long linkTimeRaw;

        public UserLink(Core core, DataRow linkRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserLink_ItemLoad);

            try
            {
                loadItemInfo(linkRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserLinkException();
            }
        }

        private void UserLink_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return linkId;
            }
        }

        public override string Uri
        {
            get
            {
                return uri;
            }
        }
    }

    public class InvalidUserLinkException : Exception
    {
    }
}
