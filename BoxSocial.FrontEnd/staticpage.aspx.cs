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
using System.Configuration;
using System.Data;
using System.Web;
using BoxSocial;
using BoxSocial.Internals;
using BoxSocial.IO;
using BoxSocial.Groups;

namespace BoxSocial.FrontEnd
{
    public partial class staticpage : TPage
    {
        private PerformanceMeter meter = null;

        public staticpage()
            : base("1201.html")
        {
            meter = new PerformanceMeter();

            meter.Add("Begin Static Page");
            BeginStaticPage();
            meter.Add("End Begin Static Page");
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            meter.Add("Begin Invoke Applications");
            Core.InvokeApplication(AppPrimitives.None, this, true);

            meter.Add("Begin End Response");

            EndResponse();
        }
    }
}
