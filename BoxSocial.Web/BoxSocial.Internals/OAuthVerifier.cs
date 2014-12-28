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
    [DataTable("oauth_verifiers")]
    public class OAuthVerifier : NumberedItem
    {
        [DataField("oauth_verifier_id", DataFieldKeys.Primary)]
        private long oauthVerifierId;
        [DataField("oauth_verifier", DataFieldKeys.Unique, 127)]
        private string oauthVerifier;
        [DataField("oauth_verifier_user_id")]
        private long userId;
        [DataField("oauth_verifier_expired")]
        private bool oauthVerifierExpired;
        [DataField("oauth_token_id")]
        private long oauthTokenId;

        public override long Id
        {
            get
            {
                return oauthVerifierId;
            }
        }

        public long UserId
        {
            get
            {
                return userId;
            }
        }

        public long TokenId
        {
            get
            {
                return oauthTokenId;
            }
        }

        public string Verifier
        {
            get
            {
                return oauthVerifier;
            }
        }

        public bool Expired
        {
            get
            {
                return oauthVerifierExpired;
            }
        }

        public OAuthVerifier(Core core, long verifierId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(OAuthVerifier_ItemLoad);

            try
            {
                LoadItem(verifierId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidOAuthVerifierException();
            }
        }

        public OAuthVerifier(Core core, string verifier)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(OAuthVerifier_ItemLoad);

            try
            {
                LoadItem("oauth_verifier", verifier);
            }
            catch (InvalidItemException)
            {
                throw new InvalidOAuthVerifierException();
            }
        }

        public OAuthVerifier(Core core, DataRow verifierRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(OAuthVerifier_ItemLoad);

            try
            {
                loadItemInfo(verifierRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidOAuthTokenException();
            }
        }

        private void OAuthVerifier_ItemLoad()
        {
        }

        protected override void loadItemInfo(DataRow verifierRow)
        {
            loadValue(verifierRow, "oauth_verifier_id", out oauthVerifierId);
            loadValue(verifierRow, "oauth_verifier", out oauthVerifier);
            loadValue(verifierRow, "oauth_verifier_user_id", out userId);
            loadValue(verifierRow, "oauth_token_id", out oauthTokenId);
            loadValue(verifierRow, "oauth_verifier_expired", out oauthVerifierExpired);

            itemLoaded(verifierRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader verifierRow)
        {
            loadValue(verifierRow, "oauth_verifier_id", out oauthVerifierId);
            loadValue(verifierRow, "oauth_verifier", out oauthVerifier);
            loadValue(verifierRow, "oauth_verifier_user_id", out userId);
            loadValue(verifierRow, "oauth_token_id", out oauthTokenId);
            loadValue(verifierRow, "oauth_verifier_expired", out oauthVerifierExpired);

            itemLoaded(verifierRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        public static OAuthVerifier Create(Core core, OAuthToken token, User user)
        {
            string newVerifier = string.Empty;

            do
            {
                newVerifier = OAuth.GeneratePublic();
            } while (!CheckVerifierUnique(core, newVerifier));


            Item item = Item.Create(core, typeof(OAuthVerifier), new FieldValuePair("oauth_token_id", token.Id),
                new FieldValuePair("oauth_verifier", newVerifier),
                new FieldValuePair("oauth_verifier_user_id", user.Id),
                new FieldValuePair("oauth_verifier_expired", false));

            OAuthVerifier newOAuthVerifier = (OAuthVerifier)item;

            return newOAuthVerifier;
        }

        public void UseVerifier()
        {
            SetPropertyByRef(new { oauthVerifierExpired }, true);
            Update();
        }

        public static bool CheckVerifierUnique(Core core, string token)
        {
            SelectQuery query = new SelectQuery(typeof(OAuthVerifier));
            query.AddCondition("oauth_verifier", token);

            System.Data.Common.DbDataReader tokenReader = core.Db.ReaderQuery(query);

            if (tokenReader.HasRows)
            {
                tokenReader.Close();
                tokenReader.Dispose();
                return false;
            }
            else
            {
                tokenReader.Close();
                tokenReader.Dispose();
                return true;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidOAuthVerifierException : Exception
    {
    }
}
