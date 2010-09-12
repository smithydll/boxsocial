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
using System.IO;
using System.Text;

namespace BoxSocial.IO
{
    public abstract class Storage
    {
        private Database db;

        public Storage(Database db)
        {
            this.db = db;
        }

        public abstract void CreateBin();

        public abstract void SaveFile(Stream file);

        public abstract void SaveFileWithReducedRedundancy(Stream file);

        public abstract void DeleteFile();

        public abstract void TouchFile();
    }
}
