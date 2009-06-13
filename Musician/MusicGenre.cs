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
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    [DataTable("genres")]
    public class MusicGenre
    {
        [DataField("genre_id", DataFieldKeys.Primary)]
        private long genreId;
        [DataField("genre_is_sub")]
        private bool isSubGenre;
        [DataField("genre_name", 31)]
        private string name;
        [DataField("genre_musicians")]
        private long musicians;
        [DataField("genre_recordings")]
        private long recordings;
    }
}
