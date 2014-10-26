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
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("oauth_tokens")]
    public class OAuthToken : NumberedItem
    {
        [DataField("oauth_token_id", DataFieldKeys.Primary)]
        private long oauthTokenId;
        [DataField("oauth_application_id")]
        private long oauthApplicationId;
        [DataField("oauth_token", 127)]
        private string oauthToken;
        [DataField("oauth_token_secret", 127)]
        private string oauthTokenSecret;
        [DataField("oauth_token_nonce", 32)]
        private string oauthTokenNone;
        [DataField("oauth_token_ip", 50)]
        private string oauthTokenIp;
        [DataField("oauth_token_created_ut")]
        private long oauthTokenCreated;
        [DataField("oauth_token_expires_ut")]
        private long oauthTokenExpires;
        [DataField("oauth_token_expired")]
        private bool oauthTokenExpired;

        public override long Id
        {
            get
            {
                return oauthTokenId;
            }
        }

        public string Token
        {
            get
            {
                return oauthToken;
            }
        }

        public string TokenSecret
        {
            get
            {
                return oauthTokenSecret;
            }
        }

        public bool TokenExpired
        {
            get
            {
                return oauthTokenExpired;
            }
        }

        public OAuthToken(Core core, long tokenId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(OAuthToken_ItemLoad);

            try
            {
                LoadItem(tokenId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidOAuthTokenException();
            }
        }

        public OAuthToken(Core core, DataRow tokenRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(OAuthToken_ItemLoad);

            try
            {
                loadItemInfo(tokenRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidOAuthTokenException();
            }
        }

        public static OAuthToken Create(Core core, OAuthApplication oae, string nonce)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            SelectQuery query = new SelectQuery(typeof(OAuthToken));
            query.AddCondition("oauth_token_nonce", nonce);

            System.Data.Common.DbDataReader queryReader = core.Db.ReaderQuery(query);

            if (queryReader.HasRows)
            {
                queryReader.Close();
                queryReader.Dispose();

                throw new NonceViolationException();
            }
            else
            {
                queryReader.Close();
                queryReader.Dispose();
            }

            string newToken = OAuth.GeneratePublic();
            string newSecret = OAuth.GenerateSecret();

            Item item = Item.Create(core, typeof(OAuthToken), new FieldValuePair("oauth_application_id", oae.Id),
                new FieldValuePair("oauth_token", newToken),
                new FieldValuePair("oauth_token_secret", newSecret),
                new FieldValuePair("oauth_token_nonce", nonce),
                new FieldValuePair("oauth_token_ip", core.Http.IpAddress),
                new FieldValuePair("oauth_token_created_ut", UnixTime.UnixTimeStamp()),
                new FieldValuePair("oauth_token_expires_ut", UnixTime.UnixTimeStamp() + 15 * 60), /* Expire after 15 minutes */
                new FieldValuePair("oauth_token_expired", false));

            OAuthToken newOAuthToken = (OAuthToken)item;

            return newOAuthToken;
        }

        public void UseToken()
        {
            SetPropertyByRef(new { oauthTokenExpired }, true);
            Update();
        }

        protected override void loadItemInfo(DataRow tokenRow)
        {
            loadValue(tokenRow, "oauth_token_id", out oauthTokenId);
            loadValue(tokenRow, "oauth_application_id", out oauthApplicationId);
            loadValue(tokenRow, "oauth_token", out oauthToken);
            loadValue(tokenRow, "oauth_token_secret", out oauthTokenSecret);
            loadValue(tokenRow, "oauth_token_nonce", out oauthTokenNone);
            loadValue(tokenRow, "oauth_token_created_ut", out oauthTokenCreated);
            loadValue(tokenRow, "oauth_token_expires_ut", out oauthTokenExpires);

            itemLoaded(tokenRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader tokenRow)
        {
            loadValue(tokenRow, "oauth_token_id", out oauthTokenId);
            loadValue(tokenRow, "oauth_application_id", out oauthApplicationId);
            loadValue(tokenRow, "oauth_token", out oauthToken);
            loadValue(tokenRow, "oauth_token_secret", out oauthTokenSecret);
            loadValue(tokenRow, "oauth_token_nonce", out oauthTokenNone);
            loadValue(tokenRow, "oauth_token_created_ut", out oauthTokenCreated);
            loadValue(tokenRow, "oauth_token_expires_ut", out oauthTokenExpires);

            itemLoaded(tokenRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void OAuthToken_ItemLoad()
        {
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidOAuthTokenException : Exception
    {
    }

    public class NonceViolationException : Exception
    {
    }
}
