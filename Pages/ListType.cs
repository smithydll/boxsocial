﻿/*
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
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [DataTable("list_types")]
    public class ListType : NumberedItem
    {
        [DataField("list_type_id", DataFieldKeys.Primary)]
        private long typeId;
        [DataField("list_type_title", 15)]
        private long title;

        public ListType(Core core, long typeId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListType_ItemLoad);

            try
            {
                LoadItem(typeId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidListTypeException();
            }
        }

        public ListType(Core core, DataRow typeRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ListType_ItemLoad);

            loadItemInfo(typeRow);
        }

        void ListType_ItemLoad()
        {
            
        }

        public override long Id
        {
            get
            {
                return typeId;
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

    public class InvalidListTypeException : Exception
    {
    }
}