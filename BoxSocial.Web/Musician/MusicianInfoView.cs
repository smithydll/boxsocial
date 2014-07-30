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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [TableView("musicians", "MUSIC")]
    public class MusicianInfoView : Item
    {
        [DataField("musician_id", DataFieldKeys.Primary)]
        private long musicianId;
        [DataField("musician_name", 63)]
        private string name;
        [DataField("musician_slug", DataFieldKeys.Unique, 63)]
        private string slug;
        [DataField("musician_name_first", DataFieldKeys.Index, 1)]
        protected string nameFirstCharacter;
        [DataField("musician_fans")]
        private long fans;
        [DataField("musician_genre")]
        private long genre;
        [DataField("musician_subgenre")]
        private long subgenre;
        [DataField("musician_home_page", MYSQL_TEXT)]
        private string homepage;

        public MusicianInfoView(Core core, long musicianId)
            : base(core)
        {
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }
}
