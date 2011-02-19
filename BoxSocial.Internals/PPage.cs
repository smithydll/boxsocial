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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    /// <summary>
    /// Primitive Page, this used to be Profile Page, use UPage for User Page
    /// </summary>
    public abstract partial class PPage : TPage
    {
        protected string primitiveKey;
        protected Primitive primitive;

        public PPage()
            : base()
        {
        }

        public PPage(string templateFile)
            : base(templateFile)
        {
        }

        public Primitive Owner
        {
            get
            {
                return primitive;
            }
        }
    }

    public class ShowPPageEventArgs : ShowPageEventArgs
    {
        private long itemId;
        private string itemSlug;

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public string Slug
        {
            get
            {
                return itemSlug;
            }
        }

        public new PPage Page
        {
            get
            {
                return (PPage)page;
            }
        }

        public ShowPPageEventArgs(PPage page)
            : base(page)
        {
        }

        public ShowPPageEventArgs(PPage page, long itemId)
            : base(page)
        {
            this.itemId = itemId;
        }

        public ShowPPageEventArgs(PPage page, string slug)
            : base(page)
        {
            this.itemSlug = slug;
        }
    }
}
