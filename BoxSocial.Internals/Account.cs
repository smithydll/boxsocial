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
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using Lachlan.Web;

namespace BoxSocial.Internals
{
    public class Account
    {

        public delegate void RegisterModuleHandler(Core core, EventArgs e);
        public event RegisterModuleHandler RegisterModule;

        public Core core;

        public Account(Core core)
        {
            this.core = core;
            RegisterModule += new RegisterModuleHandler(OnRegisterModule);
        }

        private void OnRegisterModule(Core core, EventArgs e)
        {

        }

        public void RegisterAllModules()
        {
            this.RegisterModule(core, new EventArgs());
        }
    }
}
