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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("religions")]
    public class Religion : NumberedItem
    {
        [DataField("religion_id", DataFieldKeys.Primary)]
        private short religionId;
        [DataField("religion_title", 63)]
        private string title;

        public short ReligionId
        {
            get
            {
                return religionId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
        }

        public Religion(Core core, short religionId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(Religion_ItemLoad);

            this.religionId = religionId;

            /* We cache this as it's quite static */
            switch (religionId)
            {
                case 1:
                    title = "Roman Catholic";
                    break;
                case 2:
                    title = "Anglican";
                    break;
                case 3:
                    title = "Protestant";
                    break;
                case 4:
                    title = "Church of Jesus Christ and the Later Day Saints";
                    break;
                case 5:
                    title = "Seventh Day Adventist";
                    break;
                case 6:
                    title = "Baptist";
                    break;
                case 7:
                    title = "Lutheran";
                    break;
                case 8:
                    title = "Orthodox";
                    break;
                case 9:
                    title = "Christian - Other";
                    break;
                case 10:
                    title = "Buddhist";
                    break;
                case 11:
                    title = "Muslim";
                    break;
                case 12:
                    title = "Jewish";
                    break;
                case 13:
                    title = "Hindu";
                    break;
                case 15:
                    title = "Atheist";
                    break;
                case 16:
                    title = "Agnostic";
                    break;
                case 17:
                    title = "Taoist";
                    break;
                case 18:
                    title = "Other";
                    break;
                default:
                    try
                    {
                        LoadItem(religionId);
                    }
                    catch (InvalidItemException)
                    {
                        throw new InvalidReligionException();
                    }
                    break;
            }
        }

        void Religion_ItemLoad()
        {
        }

        public override long Id
        {
            get
            {
                return religionId;
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

    public class InvalidReligionException : Exception
    {
    }
}
