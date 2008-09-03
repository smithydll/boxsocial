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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [DataTable("navigation_tabs")]
    public class NagivationTab : NumberedItem
    {
        [DataField("tab_id", DataFieldKeys.Primary)]
        private long tabId;
        [DataField("tab_page_id")]
        private long pageId;
        [DataField("tab_item_id")]
        private long ownerId;
        [DataField("tab_item_type", 15)]
        private string ownerType;

        private Primitive owner;

        public long TabId
        {
            get
            {
                return tabId;
            }
        }

        public long PageId
        {
            get
            {
                return pageId;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id || ownerType != owner.Type)
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

        public NagivationTab(Core core, long tabId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(NagivationTab_ItemLoad);

            try
            {
                LoadItem(tabId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNavigationTabException();
            }
        }

        void NagivationTab_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return tabId;
            }
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
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidNavigationTabException : Exception
    {
    }
}
