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
using System.Text;
using System.Web;
using BoxSocial.Forms;

namespace BoxSocial.Internals
{
    public class YesNoList : RadioList
    {
        private Core core;

        public YesNoList(Core core, string name)
            : base(name)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            this.core = core;

            Add("yes", core.Prose.GetString("YES"));
            Add("no", core.Prose.GetString("NO"));
        }

        public static bool FormBool(Core core, string name)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            switch (core.Http.Form[name])
            {
                case "yes":
                    return true;
                case "no":
                default:
                    return false;
            }
        }
    }
}
