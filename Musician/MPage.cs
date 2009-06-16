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
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Musician
{
    public abstract partial class MPage : TPage
    {

        protected string musicianSlug;
        protected Musician musician;

        public Musician Musician
        {
            get
            {
                return musician;
            }
        }

        public MPage()
            : base()
        {
            page = 1;
        }

        public MPage(string templateFile)
            : base(templateFile)
        {
            page = 1;
        }

        protected void BeginMusicianPage()
        {
            musicianSlug = HttpContext.Current.Request["mn"];

            try
            {
                musician = new Musician(core, musicianSlug);
            }
            catch (InvalidMusicianException)
            {
                core.Functions.Generate404();
                return;
            }

            // We do not have customised domains for musician
            /*if (string.IsNullOrEmpty(mus.Domain) || Linker.Domain == HttpContext.Current.Request.Url.Host.ToLower())
            {
                core.PagePath = core.PagePath.Substring(thisGroup.Slug.Length + 1 + 6);
            }*/
            if (core.PagePath.Trim(new char[] { '/' }) == "")
            {
                core.PagePath = Musician.Homepage;
            }
            if (core.PagePath.Trim(new char[] { '/' }) == "")
            {
                core.PagePath = "/profile";
            }

            if (loggedInMember != null)
            {
                if (loggedInMember.ShowCustomStyles)
                {
                    template.Parse("USER_STYLE_SHEET", string.Format("music/{0}.css", musician.Key));
                }
            }
            else
            {
                template.Parse("USER_STYLE_SHEET", string.Format("music/{0}.css", musician.Key));
            }

            if (!core.PagePath.StartsWith("/account"))
            {
                BoxSocial.Internals.Application.LoadApplications(core, AppPrimitives.Group, core.PagePath, BoxSocial.Internals.Application.GetApplications(core, musician));

                core.FootHooks += new Core.HookHandler(core_FootHooks);
                HookEventArgs e = new HookEventArgs(core, AppPrimitives.Group, musician);
                core.InvokeHeadHooks(e);
                core.InvokeFootHooks(e);
            }

            PageTitle = musician.DisplayName;
        }

        void core_FootHooks(HookEventArgs e)
        {
            if (e.PageType == AppPrimitives.Group)
            {
                Template template = new Template(Assembly.GetExecutingAssembly(), "group_footer");

                if (e.Owner.Type == "MUSIC")
                {
                    if (((Musician)e.Owner).IsMusicianMember(core.session.LoggedInMember))
                    {
                        template.Parse("U_MUSICIAN_ACCOUNT", core.Uri.AppendSid(e.Owner.AccountUriStub));
                    }
                }

                e.core.AddFootPanel(template);
            }
        }
    }
}
