﻿/*
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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web;
using BoxSocial.Forms;
using BoxSocial.Internals;
using BoxSocial.IO;
using TwoStepsAuthenticator;
using ZXing;
using ZXing.Common;

namespace BoxSocial.Internals
{
    [AccountSubModule("dashboard", "security")]
    public class AccountSecurity : AccountSubModule
    {
        public override string Title
        {
            get
            {
                return core.Prose.GetString("ACCOUNT_SECURITY");
            }
        }

        public override int Order
        {
            get
            {
                return 5;
            }
        }

        public AccountSecurity(Core core, Primitive owner)
            : base(core, owner)
        {
            this.Load += new EventHandler(AccountSecurity_Load);
            this.Show += new EventHandler(AccountSecurity_Show);
        }

        void AccountSecurity_Load(object sender, EventArgs e)
        {
            core.Session.ForceRecentAuthentication();
            AddModeHandler("enable", new ModuleModeHandler(AccountSecurity_Enable));
            AddSaveHandler("phone_number", new EventHandler(AccountSecurity_SavePhoneNumber));
            AddModeHandler("qr_code", new ModuleModeHandler(AccountSecurity_QrCode));
            AddModeHandler("disable", new ModuleModeHandler(AccountSecurity_Disable));
        }

        void AccountSecurity_Show(object sender, EventArgs e)
        {
            template.SetTemplate("account_security.html");

            Save(new EventHandler(AccountSecurity_Save));

            if (core.Http.Query["mode"] == "enrole_phone" && (!LoggedInMember.UserInfo.TwoFactorAuthVerified))
            {
                template.SetTemplate("account_security_twofactor.html");
                AuthoriseRequestSid();


            }
            else if (core.Http.Query["mode"] == "enrole_authenticator" && (!LoggedInMember.UserInfo.TwoFactorAuthVerified))
            {
                template.SetTemplate("account_security_twofactor.html");
                AuthoriseRequestSid();

                Authenticator authenticator = new Authenticator();
                string key = authenticator.GenerateKey();

                Dictionary<string, string> args = new Dictionary<string, string>();
                args.Add("mode", "qr_code");
                args.Add("secret", key);
                string qrCode = core.Hyperlink.AppendSid(BuildUri("security", args), true);

                BoxSocial.Forms.Image qrCodeImage = new Forms.Image("qr_code", qrCode);
                BoxSocial.Forms.TextBox verifyTextBox = new Forms.TextBox("verify");
                BoxSocial.Forms.HiddenField keyHiddenField = new Forms.HiddenField("key");
                keyHiddenField.Value = key;

                template.Parse("S_ENROLE_AUTHENTICATOR", "TRUE");
                template.Parse("I_QR_CODE", qrCodeImage);
                template.Parse("S_KEY", keyHiddenField);
                template.Parse("S_VERIFY", verifyTextBox);
                template.Parse("USERNAME", LoggedInMember.UserName);

                LoggedInMember.UserInfo.TwoFactorAuthKey = key;
                LoggedInMember.UserInfo.Update();
            }
            else if (LoggedInMember.UserInfo.TwoFactorAuthVerified)
            {
                template.Parse("S_ENABLED", "TRUE");
                template.Parse("U_DISABLE", core.Hyperlink.AppendSid(BuildUri("security", "disable"), true));
            }
            else
            {
                template.Parse("S_DISABLED", "TRUE");
                template.Parse("U_ENABLE", core.Hyperlink.AppendSid(BuildUri("security", "enable"), true));
            }

            // Show all active sessions
            SelectQuery query = SessionKey.GetSelectQueryStub(core, typeof(SessionKey));
            query.AddCondition(new DataField(typeof(SessionKey), "user_id"), LoggedInMember.Id);
            query.AddSort(SortOrder.Descending, "key_last_visit_ut");

            System.Data.Common.DbDataReader sessionsReader = db.ReaderQuery(query);

            while (sessionsReader.Read())
            {
                SessionKey sessionKey = new SessionKey(core, sessionsReader);

                VariableCollection sessionsVariableCollection = template.CreateChild("sessions_list");

                sessionsVariableCollection.Parse("IP", sessionKey.Ip);
                sessionsVariableCollection.Parse("TIME", core.Tz.DateTimeToString(sessionKey.GetVisit(core.Tz)));
                sessionsVariableCollection.Parse("BROWSER_STRING", sessionKey.BrowserString);
            }

            sessionsReader.Close();
            sessionsReader.Dispose();
        }

        void AccountSecurity_Save(object sender, EventArgs e)
        {
            AuthoriseRequestSid();

            bool fail = false;

            if (core.Http.Form["mode"] == "enable" && (!LoggedInMember.UserInfo.TwoFactorAuthVerified))
            {
                Authenticator authenticator = new Authenticator();
                if (authenticator.CheckCode(LoggedInMember.UserInfo.TwoFactorAuthKey, core.Http.Form["verify"]))
                {
                    LoggedInMember.UserInfo.TwoFactorAuthVerified = true;
                }
                else
                {
                    LoggedInMember.UserInfo.TwoFactorAuthKey = string.Empty;
                    fail = true;
                }
                LoggedInMember.UserInfo.Update();

                // Temporary, this should be done on an elevated session which is higher than two factor
                UpdateQuery uQuery = new UpdateQuery(typeof(Session));
                uQuery.AddField("session_signed_in", (byte)SessionSignInState.TwoFactorValidated);
                uQuery.AddCondition("session_id", core.Session.SessionId);

                core.Db.Query(uQuery);
            }

            if (LoggedInMember.UserInfo.TwoFactorAuthVerified)
            {
                core.Display.ShowMessage("Two Factor Authentication Enabled", "Two factor authentication has been enabled for this account.");
            }
            else if (fail)
            {
                core.Display.ShowMessage("Two Factor Authentication Failed", "Two factor authentication has not been enabled for this account. Check you entered the code correctly.");
            }
            else
            {
                core.Display.ShowMessage("Two Factor Authentication Disabled", "Two factor authentication has been disabled for this account.");
            }
            SetRedirectUri(BuildUri());
        }

        void AccountSecurity_SavePhoneNumber(object sender, EventArgs e)
        {
            AuthoriseRequestSid();
        }

            void AccountSecurity_Enable(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            template.SetTemplate("account_security_twofactor.html");

            SelectBox phoneNumberSelectBox = new SelectBox("two_factor_phone_id");
            TextBox phoneNumberTextBox = new TextBox("two_factor_phone_number");
            HiddenField modeHiddenField = new HiddenField("mode");

            modeHiddenField.Value = "phone_number";

            SelectQuery query = UserPhoneNumber.GetSelectQueryStub(core, typeof(UserPhoneNumber));
            query.AddCondition("phone_user_id", core.LoggedInMemberId);

            DataTable phoneNumberDataTable = core.Db.Query(query);

            foreach (DataRow row in phoneNumberDataTable.Rows)
            {
                UserPhoneNumber number = new UserPhoneNumber(core, row);
                phoneNumberSelectBox.Add(new SelectBoxItem(number.Id.ToString(), number.PhoneNumber));
            }

            template.Parse("S_SELECT_NUMBER", phoneNumberSelectBox);
            template.Parse("S_ADD_NUMBER", phoneNumberTextBox);
            template.Parse("S_MODE", modeHiddenField);
        }

        void AccountSecurity_Disable(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            LoggedInMember.UserInfo.TwoFactorAuthVerified = false;
            LoggedInMember.UserInfo.Update();

            SetRedirectUri(BuildUri("security"));
        }

        void AccountSecurity_QrCode(object sender, ModuleModeEventArgs e)
        {
            AuthoriseRequestSid();

            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = 200,
                    Width = 200
                }
            };

            Bitmap bitmap = barcodeWriter.Write("otpauth://totp/" + HttpUtility.UrlEncode("@" + LoggedInMember.UserName) + "?secret=" + LoggedInMember.UserInfo.TwoFactorAuthKey + "&issuer=" + HttpUtility.UrlEncode(core.Settings.SiteTitle));

            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);

            core.Http.SwitchContextType("image/png");
            core.Http.WriteStream(stream);

            if (db != null)
            {
                db.CloseConnection();
            }

            core.Prose.Close();

            core.Http.End();
        }
    }
}
