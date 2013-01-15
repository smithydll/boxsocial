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
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using BoxSocial.IO;
using BoxSocial.Forms;
using BoxSocial.Internals;

namespace BoxSocial.Internals
{
    public abstract class AccountSubModule : ControlPanelSubModule
    {
        public abstract override string Title
        {
            get;
        }

        public abstract override int Order
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the AccountSubModule class. 
        /// </summary>
        /// <param name="core">The Core token.</param>
        public AccountSubModule(Core core)
            : base(core)
        {
        }
    }
}
