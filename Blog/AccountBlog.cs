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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Blog
{
    /// <summary>
    /// The account module for blog management
    /// </summary>
    [AccountModule("blog")]
    public class AccountBlog : AccountModule
    {

        /// <summary>
        /// Initialises a new instance of the AccountBlog class, which is an
        /// account module for the Blog application.
        /// </summary>
        /// <param name="account">The container account being initialised into.</param>
        public AccountBlog(Account account)
            : base(account)
        {
        }

        /// <summary>
        /// Callback on registration of the module in the account panel.
        /// </summary>
        /// <param name="core">Core token</param>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected override void RegisterModule(Core core, EventArgs e)
        {
            
        }

        /// <summary>
        /// Display name of the module.
        /// </summary>
        public override string Name
        {
            get {
                return core.Prose.GetString("BLOG");
            }
        }

        /// <summary>
        /// The order the module is to appear along the tab display.
        /// </summary>
        public override int Order
        {
            get
            {
                return 4;
            }
        }
    }
}
