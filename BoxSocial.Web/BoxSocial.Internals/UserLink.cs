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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("user_links")]
    public sealed class UserLink : NumberedItem, IPermissibleSubItem
    {
        [DataField("user_link_id", DataFieldKeys.Primary)]
        private long linkId;
        [DataField("user_link_user_id", typeof(User))]
        private long userId;
        [DataField("user_link_title", 31)]
        private string title;
        [DataField("user_link_uri", 255)]
        private string uri;
        [DataField("user_favicon", 127)]
        private string favicon;
        [DataField("link_time_ut")]
        private long linkTimeRaw;

        private User owner;

        public UserLink(Core core, long linkId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserLink_ItemLoad);

            try
            {
                LoadItem(linkId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserLinkException();
            }
        }

        public UserLink(Core core, DataRow linkRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserLink_ItemLoad);

            try
            {
                loadItemInfo(linkRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserLinkException();
            }
        }

        public UserLink(Core core, System.Data.Common.DbDataReader linkRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(UserLink_ItemLoad);

            try
            {
                loadItemInfo(linkRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidUserLinkException();
            }
        }

        protected override void loadItemInfo(DataRow linkRow)
        {
            loadValue(linkRow, "user_link_id", out linkId);
            loadValue(linkRow, "user_link_user_id", out userId);
            loadValue(linkRow, "user_link_title", out title);
            loadValue(linkRow, "user_link_uri", out uri);
            loadValue(linkRow, "user_favicon", out favicon);
            loadValue(linkRow, "link_time_ut", out linkTimeRaw);

            itemLoaded(linkRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader linkRow)
        {
            loadValue(linkRow, "user_link_id", out linkId);
            loadValue(linkRow, "user_link_user_id", out userId);
            loadValue(linkRow, "user_link_title", out title);
            loadValue(linkRow, "user_link_uri", out uri);
            loadValue(linkRow, "user_favicon", out favicon);
            loadValue(linkRow, "link_time_ut", out linkTimeRaw);

            itemLoaded(linkRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void UserLink_ItemLoad()
        {
        }

        public static UserLink Create(Core core, string uri, string title)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            UriBuilder ub = new UriBuilder(uri);
            string favicon = ub.Host.ToLower() + ".png";
            ub.Scheme = "http";
            ub.Port = 80;

            if (ub.Host.ToLower().EndsWith("tumblr.com", StringComparison.Ordinal))
            {
                favicon = "tumblr.com.png";
            }

            string localFaviconPath = Path.Combine(core.Http.MapPath("."), "images", "favicons", favicon);
            if (!File.Exists(localFaviconPath))
            {
                UriBuilder fb = new UriBuilder(uri);
                fb.Path = "favicon.ico";

                if (ub.Host.ToLower().EndsWith("tumblr.com", StringComparison.Ordinal))
                {
                    fb.Host = "tumblr.com";
                }

                WebDownload wc = new WebDownload(2000);

                try
                {
                    byte[] bytes = wc.DownloadData(fb.Uri);

                    MemoryStream ms = new MemoryStream(bytes);

                    Image img = Image.FromStream(ms);

                    if (img.Width > 16 || img.Height > 16)
                    {
                        Image icon = new Bitmap(16, 16);
                        Graphics g = Graphics.FromImage(icon);

                        g.DrawImage(img, new Rectangle(0, 0, 16, 16), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);

                        icon.Save(localFaviconPath, ImageFormat.Png);
                    }
                    else
                    {
                        img.Save(localFaviconPath, ImageFormat.Png);
                    }
                    ms.Close();
                }
                catch
                {
                    favicon = string.Empty;
                }
            }

            Item newItem = Item.Create(core, typeof(UserLink), new FieldValuePair("user_link_user_id", core.LoggedInMemberId),
                new FieldValuePair("user_link_title", title),
                new FieldValuePair("user_link_uri", ub.Uri.ToString()),
                new FieldValuePair("user_favicon", favicon),
                new FieldValuePair("link_time_ut", UnixTime.UnixTimeStamp()));

            return (UserLink)newItem;
        }

        public ItemKey OwnerKey
        {
            get
            {
                return new ItemKey(userId, ItemType.GetTypeId(core, typeof(User)));
            }
        }

        public User Owner
        {
            get
            {
                if (owner == null || userId != owner.Id)
                {
                    core.LoadUserProfile(userId);
                    owner = core.PrimitiveCache[userId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public override long Id
        {
            get
            {
                return linkId;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                SetPropertyByRef(new { title }, value);
            }
        }

        public string LinkAddress
        {
            get
            {
                return uri;
            }
            set
            {
                SetPropertyByRef(new { uri }, value);

                UriBuilder ub = new UriBuilder(uri);
                Favicon = ub.Host + ".ico";
            }
        }

        public override string Uri
        {
            get
            {
                return LinkAddress;
            }
        }

        public string Favicon
        {
            get
            {
                return favicon;
            }
            private set
            {
                SetPropertyByRef(new { favicon }, value);
            }
        }

        Primitive IPermissibleSubItem.Owner
        {
            get
            {
                return (Primitive)Owner;
            }
        }

        public IPermissibleItem PermissiveParent
        {
            get
            {
                return (IPermissibleItem)Owner;
            }
        }
    }

    public class InvalidUserLinkException : Exception
    {
    }
}
