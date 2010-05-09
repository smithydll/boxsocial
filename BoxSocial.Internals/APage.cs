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
using System.Configuration;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract partial class APage : PPage
    {
        protected string anAssemblyName;
        protected ApplicationEntry anApplication;

        public APage()
            : base()
        {
            page = 1;

            try
            {
                page = int.Parse(Request.QueryString["p"]);
            }
            catch
            {
            }

        }

        public APage(string templateFile)
            : base(templateFile)
        {
            page = 1;

            try
            {
                page = int.Parse(Request.QueryString["p"]);
            }
            catch
            {
            }
        }

        public ApplicationEntry AnApplication
        {
            get
            {
                return anApplication;
            }
        }

        protected void BeginProfile()
        {
            anAssemblyName = core.Http["an"];

            try
            {
                anApplication = new ApplicationEntry(core, null, anAssemblyName);
            }
            catch (InvalidApplicationException)
            {
                core.Functions.Generate404();
                return;
            }

            core.PagePath = core.PagePath.Substring(anApplication.AssemblyName.Length + 1 + 12);
            if (core.PagePath.Trim(new char[] { '/' }) == string.Empty)
            {
                core.PagePath = "/profile";
            }

            BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Application, core.PagePath, BoxSocial.Internals.Application.GetApplications(core, anApplication));

            PageTitle = anApplication.Title;



        }
    }

    public class ShowAPageEventArgs : ShowPPageEventArgs
    {
        public new APage Page
        {
            get
            {
                return (APage)page;
            }
        }

        public ShowAPageEventArgs(APage page, long itemId)
            : base(page, itemId)
        {
        }

        public ShowAPageEventArgs(APage page)
            : base(page)
        {
        }
    }
}
