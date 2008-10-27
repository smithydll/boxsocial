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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_style", "USER")]
    public class UserStyle : NumberedItem
    {
        [DataField("user_id", DataFieldKeys.Unique)]
        private long userId;
        [DataField("style_css", MYSQL_TEXT)]
        private string css;

        private CascadingStyleSheet styleSheet;

        /// <summary>
        /// Gets the user Id
        /// </summary>
        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public string RawCss
        {
            get
            {
                return StyleSheet.ToString();
            }
            set
            {
                styleSheet = null;
                SetProperty("css", value);
            }
        }

        public CascadingStyleSheet StyleSheet
        {
            get
            {
                if (styleSheet == null)
                {
                    styleSheet = new CascadingStyleSheet();
                    styleSheet.Parse(css);
                }
                return styleSheet;
            }
        }

        internal UserStyle(Core core, long userId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserStyle_ItemLoad);
            OnUpdate += new EventHandler(UserStyle_OnUpdate);

            try
            {
                LoadItem("user_id", userId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserStyleException();
            }
        }

        internal UserStyle(Core core, DataRow memberRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserStyle_ItemLoad);
            OnUpdate += new EventHandler(UserStyle_OnUpdate);

            try
            {
                loadItemInfo(memberRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserStyleException();
            }
        }

        void UserStyle_ItemLoad()
        {
        }

        void UserStyle_OnUpdate(object sender, EventArgs e)
        {
            RawCss = StyleSheet.ToString();
        }

        public static UserStyle Create(Core core, User owner, string css)
        {
            if (owner.Id == core.LoggedInMemberId)
            {
                InsertQuery iQuery = new InsertQuery(Item.GetTable(typeof(UserStyle)));
                iQuery.AddField("user_id", owner.Id);
                iQuery.AddField("style_css", css);

                core.db.Query(iQuery);

                return new UserStyle(core, owner.Id);
            }
            else
            {
                throw new UnauthorisedToCreateItemException();
            }
        }

        public override long Id
        {
            get
            {
                return userId;
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

    public class InvalidUserStyleException : Exception
    {
    }
}
